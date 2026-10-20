using System.Globalization;
using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;

namespace ArticleMetadata;

/// <summary>
/// Retrieves publisher pages and extracts their article metadata.
/// This class contains no API or database dependencies so it can be reused
/// by both ArticleMetadataTest and the news-feed sample-data preparation step.
/// </summary>
public sealed class ArticleMetadataExtractor : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// Creates an extractor with its own HttpClient, configured with a timeout
    /// and a user-agent that identifies this Project 300 test/prototype.
    /// </summary>
    public ArticleMetadataExtractor()
        : this(CreateHttpClient(), ownsHttpClient: true)
    {
    }

    /// <summary>
    /// Creates an extractor using a caller-provided HttpClient.
    /// This overload is useful when a longer-running process wants to manage
    /// HttpClient lifetime and configuration itself.
    /// </summary>
    public ArticleMetadataExtractor(HttpClient httpClient)
        : this(httpClient, ownsHttpClient: false)
    {
    }

    private ArticleMetadataExtractor(HttpClient httpClient, bool ownsHttpClient)
    {
        _httpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            // Stop waiting if a publisher takes too long to respond.
            Timeout = TimeSpan.FromSeconds(20)
        };

        // Identify this test/prototype in requests sent to publishers.
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Project300MetadataTest/1.0");

        return client;
    }

    /// <summary>
    /// Extracts article metadata from a publisher URL.
    /// Fields are handled independently: a missing image does not prevent
    /// extraction of the headline or description.
    /// </summary>
    /// <param name="articleUrl">The publisher's article URL.</param>
    /// <param name="gdeltImage">Optional GDELT SharingImage fallback.</param>
    /// <param name="cancellationToken">Allows the caller to cancel the request.</param>
    public async Task<ArticleMetadataResult> ExtractAsync(
        string articleUrl,
        string? gdeltImage = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ArticleMetadataResult
        {
            ArticleUrl = articleUrl
        };

        // Validate the URL before making a network request. Only HTTP and
        // HTTPS URLs are accepted as article pages.
        if (!Uri.TryCreate(articleUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            result.Status = "InvalidUrl";
            result.ErrorMessage = "The article URL is not a valid HTTP or HTTPS URL.";
            ApplyFallbacks(result, articleUrl, gdeltImage);
            return result;
        }

        try
        {
            using var response = await _httpClient.GetAsync(uri, cancellationToken);
            result.HttpStatusCode = (int)response.StatusCode;

            // If the publisher rejects the request or returns another
            // unsuccessful status, use available fallbacks instead of parsing
            // a page that was not successfully retrieved.
            if (!response.IsSuccessStatusCode)
            {
                result.Status = "HttpError";
                result.ErrorMessage =
                    $"The publisher returned HTTP {(int)response.StatusCode} ({response.StatusCode}).";

                ApplyFallbacks(result, articleUrl, gdeltImage);
                return result;
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            result.HtmlLength = html.Length;

            // AngleSharp parses the downloaded HTML so we can search its
            // elements and metadata without manually processing HTML text.
            using var context = BrowsingContext.New(Configuration.Default);
            using var document = await context.OpenAsync(
                request => request.Content(html), cancellationToken);

            result.PageRetrieved = true;
            result.Status = "Success";
            result.PageTitle = document.Title;

            /*
             * Metadata fallback order:
             * 1. Open Graph metadata
             * 2. JSON-LD structured data
             * 3. HTML page title (headline only)
             * 4. Headline generated from the URL
             * 5. GDELT SharingImage (image only)
             *
             * Each field is handled independently. For example, a missing
             * image does not prevent us from retrieving a headline or description.
             */

            // 1. Open Graph metadata is commonly provided for sharing articles.
            var headline = GetMeta(document, "property", "og:title");
            var description = GetMeta(document, "property", "og:description");
            var image = GetMeta(document, "property", "og:image");

            // Keep track of where each value came from so the fallback
            // behaviour can be verified in the console and recorded later.
            var headlineSource = HasValue(headline) ? "Open Graph" : null;
            var descriptionSource = HasValue(description) ? "Open Graph" : null;
            var imageSource = HasValue(image) ? "Open Graph" : null;

            // 2. Look for NewsArticle structured data in the page's JSON-LD scripts.
            // JSON-LD is a format publishers use to describe information about a page.
            string? jsonHeadline = null;
            string? jsonDescription = null;
            string? jsonImage = null;

            foreach (var script in document.QuerySelectorAll(
                "script[type='application/ld+json']"))
            {
                try
                {
                    using var json = JsonDocument.Parse(script.TextContent);
                    var article = FindNewsArticle(json.RootElement);

                    // This script might describe something other than an article.
                    // Continue to the next script if no NewsArticle was found.
                    if (article is null)
                    {
                        continue;
                    }

                    jsonHeadline = GetJsonValue(article.Value, "headline");
                    jsonDescription = GetJsonValue(article.Value, "description");

                    // Publishers may store an image as either a thumbnail or image
                    // property, so try both.
                    jsonImage = FirstNonEmpty(
                        GetJsonValue(article.Value, "thumbnailUrl"),
                        GetJsonValue(article.Value, "image"));

                    break;
                }
                catch (JsonException)
                {
                    // A script may contain invalid JSON. Ignore it and try the next
                    // script rather than stopping metadata extraction completely.
                }
            }

            // Use JSON-LD only when the corresponding Open Graph value is missing.
            if (!HasValue(headline) && HasValue(jsonHeadline))
            {
                headline = jsonHeadline;
                headlineSource = "JSON-LD";
            }

            if (!HasValue(description) && HasValue(jsonDescription))
            {
                description = jsonDescription;
                descriptionSource = "JSON-LD";
            }

            if (!HasValue(image) && HasValue(jsonImage))
            {
                image = jsonImage;
                imageSource = "JSON-LD";
            }

            // 3. The HTML page title can provide another headline fallback.
            // CleanPageTitle removes common publisher names appended to page titles.
            if (!HasValue(headline) && HasValue(document.Title))
            {
                headline = CleanPageTitle(document.Title!);
                headlineSource = HasValue(headline) ? "HTML page title" : null;
            }

            // 4. If the page contains no usable headline, generate an approximate
            // one from the URL. This may not exactly match the publisher's headline.
            if (!HasValue(headline))
            {
                headline = CreateHeadlineFromUrl(articleUrl);
                headlineSource = HasValue(headline) ? "URL slug (generated)" : null;
            }

            // 5. Use GDELT's SharingImage if the publisher page provides no image.
            // This also works when an image URL is supplied for a blocked article.
            if (!HasValue(image) && HasValue(gdeltImage))
            {
                image = gdeltImage;
                imageSource = "GDELT SharingImage";
            }

            result.Headline = headline;
            result.HeadlineSource = headlineSource;
            result.Description = description;
            result.DescriptionSource = descriptionSource;
            result.ImageUrl = image;
            result.ImageSource = imageSource;

            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient uses cancellation internally when its timeout expires.
            // Record that separately from a cancellation requested by our caller.
            result.Status = "Timeout";
            result.ErrorMessage = "The publisher request timed out.";
            ApplyFallbacks(result, articleUrl, gdeltImage);
            return result;
        }
        catch (HttpRequestException ex)
        {
            // This can happen if the publisher blocks the request or a network
            // error prevents the page from being downloaded.
            result.Status = "HttpError";
            result.ErrorMessage = ex.Message;
            ApplyFallbacks(result, articleUrl, gdeltImage);
            return result;
        }
        catch (OperationCanceledException)
        {
            // Respect cancellation requested by the calling process. Do not
            // disguise it as a publisher error or continue with more requests.
            throw;
        }
        catch (Exception ex)
        {
            // Report unexpected errors while preserving any available fallbacks.
            result.Status = "Error";
            result.ErrorMessage = ex.Message;
            ApplyFallbacks(result, articleUrl, gdeltImage);
            return result;
        }
    }

    /// <summary>
    /// Supplies only fallbacks that do not require the publisher HTML.
    /// No description is invented because there is no trustworthy source for it.
    /// </summary>
    private static void ApplyFallbacks(
        ArticleMetadataResult result,
        string articleUrl,
        string? gdeltImage)
    {
        if (!HasValue(result.Headline))
        {
            result.Headline = CreateHeadlineFromUrl(articleUrl);
            result.HeadlineSource = HasValue(result.Headline)
                ? "URL slug (generated)"
                : null;
        }

        if (!HasValue(result.ImageUrl) && HasValue(gdeltImage))
        {
            result.ImageUrl = gdeltImage;
            result.ImageSource = "GDELT SharingImage";
        }
    }

    /// <summary>
    /// Finds a metadata tag in the HTML and returns its content.
    /// For example, this retrieves the content of the og:title property.
    /// </summary>
    private static string? GetMeta(IDocument document, string attribute, string value)
    {
        return document
            .QuerySelector($"meta[{attribute}='{value}']")
            ?.GetAttribute("content")
            ?.Trim();
    }

    /// <summary>Returns the first non-empty value in the supplied list.</summary>
    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(HasValue);
    }

    /// <summary>Checks whether a string contains something other than whitespace.</summary>
    private static bool HasValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Removes common publisher suffixes from an HTML page title.
    /// For example, "Example headline | Publisher Name" becomes "Example headline".
    /// </summary>
    private static string CleanPageTitle(string title)
    {
        var cleaned = title.Trim();

        // Publishers use different separators before their names.
        string[] separators = [" | ", " - ", " – ", " — ", " :: "];

        foreach (var separator in separators)
        {
            // Search from the end so we remove the final publisher suffix.
            var index = cleaned.LastIndexOf(separator, StringComparison.Ordinal);

            if (index > 0)
            {
                cleaned = cleaned[..index].Trim();
                break;
            }
        }

        return cleaned;
    }

    /// <summary>
    /// Creates an approximate headline from the article URL when no headline
    /// can be extracted from the publisher's page.
    /// </summary>
    private static string? CreateHeadlineFromUrl(string articleUrl)
    {
        // Check that the supplied text is a valid URL.
        if (!Uri.TryCreate(articleUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        // Split the URL path into sections using "/" as the separator.
        // The final section usually contains the article's URL slug.
        // Example: "my-news-article" in "example.com/my-news-article".
        var segments = uri.AbsolutePath.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            return null;
        }

        // Get the last section of the URL and decode any encoded characters.
        // For example, "%20" represents a space in a URL.
        var slug = Uri.UnescapeDataString(segments[^1]);

        // Remove a file extension such as ".html", if one exists.
        var extension = Path.GetExtension(slug);

        if (!string.IsNullOrEmpty(extension))
        {
            slug = slug[..^extension.Length];
        }

        // Some publishers encode curly quotation marks as the numbers
        // 8216 and 8217 in their article URLs. Replace these with spaces.
        slug = slug.Replace("8216", " ").Replace("8217", " ");

        // Some URLs end with a long number representing a timestamp or ID.
        // Count how many digits appear at the end of the slug.
        var digitCount = 0;

        while (digitCount < slug.Length &&
               char.IsDigit(slug[slug.Length - 1 - digitCount]))
        {
            digitCount++;
        }

        // Only remove the number if it contains at least 12 digits.
        // This avoids removing ordinary numbers that might be part of a title.
        if (digitCount >= 12)
        {
            slug = slug[..^digitCount];
        }

        // Remove leftover hyphens, underscores and spaces from the ends.
        slug = slug.Trim('-', '_', ' ');

        // If the slug does not look like a descriptive title, do not use it.
        // For example, a short article ID is not a useful headline.
        if (!slug.Contains('-') || slug.Length < 8)
        {
            return null;
        }

        // Replace hyphens and underscores with spaces.
        slug = slug.Replace('-', ' ').Replace('_', ' ');

        // Split the slug into individual words, ignoring extra spaces.
        var words = slug.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return null;
        }

        // Capitalise the beginning of each word to make the result look
        // more like a headline. This may not match the publisher's style.
        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        return textInfo.ToTitleCase(string.Join(' ', words));
    }

    /// <summary>
    /// Searches JSON-LD data for an object describing a NewsArticle.
    /// JSON-LD can contain nested objects and arrays, so this method searches
    /// inside them recursively.
    /// </summary>
    private static JsonElement? FindNewsArticle(JsonElement element)
    {
        // JSON data can contain an array, which is a list of values.
        // Search each item until we find a NewsArticle.
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var result = FindNewsArticle(item);

                if (result is not null)
                {
                    return result;
                }
            }
        }
        // A JSON object contains named properties, similar to key-value pairs.
        else if (element.ValueKind == JsonValueKind.Object)
        {
            // Check whether this object has an "@type" property.
            // Publishers use this property to identify what the object describes.
            if (element.TryGetProperty("@type", out var type))
            {
                // Some publishers store "@type" as a single string.
                if (type.ValueKind == JsonValueKind.String &&
                    type.GetString() == "NewsArticle")
                {
                    return element;
                }

                // Other publishers store "@type" as an array of strings.
                // Check each string in the array for "NewsArticle".
                if (type.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in type.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String &&
                            item.GetString() == "NewsArticle")
                        {
                            return element;
                        }
                    }
                }
            }

            // The NewsArticle might be inside another property of this object.
            // Search each property by calling this method again.
            foreach (var property in element.EnumerateObject())
            {
                var result = FindNewsArticle(property.Value);

                if (result is not null)
                {
                    return result;
                }
            }
        }

        // Return null when this part of the JSON contains no NewsArticle.
        return null;
    }

    /// <summary>
    /// Gets a named value from a JSON object. The value might be a string,
    /// an array, or another object (as often happens with image properties).
    /// </summary>
    private static string? GetJsonValue(JsonElement element, string propertyName)
    {
        // A named property can only be retrieved from a JSON object.
        // TryGetProperty also checks whether the requested property exists.
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        // Case 1: The value is already a string.
        // Headlines and descriptions are commonly stored this way.
        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        // Case 2: The value is an array (a list of values).
        // Image information can be stored as a list, so check each item.
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                // If an item is a string, return it.
                if (item.ValueKind == JsonValueKind.String)
                {
                    return item.GetString();
                }

                // An item might instead be an object containing a URL.
                if (item.ValueKind == JsonValueKind.Object)
                {
                    var url = GetJsonValue(item, "url");

                    if (HasValue(url))
                    {
                        return url;
                    }

                    // Some publishers use "contentUrl" instead of "url".
                    var contentUrl = GetJsonValue(item, "contentUrl");

                    if (HasValue(contentUrl))
                    {
                        return contentUrl;
                    }
                }
            }
        }

        // Case 3: The value is an object, such as an image object.
        // Look inside it for a "url" or "contentUrl" property.
        if (value.ValueKind == JsonValueKind.Object)
        {
            var url = GetJsonValue(value, "url");

            if (HasValue(url))
            {
                return url;
            }

            return GetJsonValue(value, "contentUrl");
        }

        // The property was missing or used a format we do not support.
        return null;
    }

    public void Dispose()
    {
        // Dispose the HttpClient only when this class created it.
        // A caller-provided HttpClient remains owned by the caller.
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
