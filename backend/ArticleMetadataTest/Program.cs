
/*
 * Project 300 - Article Metadata Tester
 *
 * The PowerShell script below tests metadata extraction against multiple
 * publishers. It runs this console program once for each article URL.
 * Keep it commented out because it is a PowerShell script, not C# code.
 */

/*
$urls = @(
   # First three: BBC, The Guardian, and a second RTÉ article
   "https://www.bbc.co.uk/news/articles/c680zyz2m5lro"
   "https://www.theguardian.com/music/2026/oct/12/george-fenton-obituary"
   "https://www.rte.ie/news/regional/2026/1012/1595044-galway-culture-company/"

   # Top 10 publishers in your GDELT snapshot
   "https://941thebeat.iheart.com/content/2026-10-12-blueface-proposes-to-chrisean-rock-at-arizona-nightclub/"
   "https://economictimes.indiatimes.com/news/international/us/diddy-rushed-out-of-prison-after-head-injury-as-rapper-receives-emergency-medical-treatment/articleshow/134900669.cms"
   "https://www.aol.co.uk/articles/labours-troubles-bill-put-elderly-183000000.html"
   "https://www.boredpanda.com/hilarious-signs-ruined-misused-quotation-marks/?cexp_id=162868&_f=homepage_featured&cexp_var=0"
   "https://www.manilatimes.net/2026/10/13/tmt-newswire/globenewswire/fixgo-announces-nationwide-shipping-of-auto-parts-from-california-warehouse/2444062"
   "https://www.aol.com/articles/crowdstrike-124-2026-fortinet-142-190017000.html"
   "https://www.ghanamma.com/2026/10/12/ohemaa-woyeje-finally-breaks-silence-on-why-angel-fm-tv-accra-went-off-air/"
   "https://www.thehindu.com/news/cities/Delhi/delhi-cm-launches-portal-for-durga-e-auto-scheme-for-women-transgender-persons/article71575898.ece"
   "http://www.bignewsnetwork.com/news/279369248/leaning-into-tight-ends-in-look-at-lions-snaps"
   "https://aninews.in/news/national/general-news/216-households-in-valmiki-camp-to-get-tap-water-connections-delhi-mayor-pravesh-wahi-inaugurates-8216har-ghar-nal-yojana821720261012224334/"
)

foreach ($url in $urls) {
   Write-Host "`n============================================" -ForegroundColor Cyan
   Write-Host "Testing: $url" -ForegroundColor Yellow
   Write-Host "============================================" -ForegroundColor Cyan

   dotnet run -- "$url"
}
*/

using System.Globalization;
using System.Linq;
using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;

// Use the URL supplied as the first command-line argument.
// If no URL is supplied, test the default RTÉ article instead.
var articleUrl = args.Length > 0
    ? args[0]
    : "https://www.rte.ie/news/business/2026/1012/1595068-openai-jobs-ireland/";

// An optional second argument supplies the image URL from GDELT.
// This allows us to test the image fallback when a publisher page is blocked.
var gdeltImage = args.Length > 1 ? args[1] : null;

using var httpClient = new HttpClient
{
    // Stop waiting if a publisher takes too long to respond.
    Timeout = TimeSpan.FromSeconds(20)
};

// Identify this test program in requests sent to publishers.
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
    "Project300MetadataTest/1.0"
);

Console.WriteLine($"URL: {articleUrl}");

try
{
    using var response = await httpClient.GetAsync(articleUrl);

    Console.WriteLine(
        $"HTTP status: {(int)response.StatusCode} {response.StatusCode}"
    );

    // If the publisher rejects the request or returns another unsuccessful
    // status, use the available fallbacks instead of processing the page.
    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine();
        Console.WriteLine("The page could not be retrieved.");
        PrintFallbacks(articleUrl, gdeltImage);
        return;
    }

    var html = await response.Content.ReadAsStringAsync();

    // AngleSharp parses the downloaded HTML so we can search its elements
    // and metadata without manually processing the HTML text.
    using var context = BrowsingContext.New(Configuration.Default);

    using var document = await context.OpenAsync(
        request => request.Content(html)
    );

    /*
     * Metadata fallback order:
     * 1. Open Graph metadata
     * 2. JSON-LD structured data
     * 3. HTML page title (headline only)
     * 4. Headline generated from the URL
     *
     * Each field is handled independently. For example, a missing image
     * does not prevent us from retrieving a headline or description.
     */

    // 1. Open Graph metadata is commonly provided for sharing articles.
    var headline = GetMeta(document, "property", "og:title");
    var description = GetMeta(document, "property", "og:description");
    var image = GetMeta(document, "property", "og:image");

    // Keep track of where each value came from so we can verify the fallback
    // behaviour in the console output.
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
                GetJsonValue(article.Value, "image")
            );

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

    PrintResult(
        document.Title,
        headline,
        headlineSource,
        description,
        descriptionSource,
        image,
        imageSource,
        html.Length
    );
}
catch (HttpRequestException ex)
{
    // This can happen if the publisher blocks the request or a network error
    // prevents the page from being downloaded.
    Console.WriteLine($"HTTP request failed: {ex.Message}");
    PrintFallbacks(articleUrl, gdeltImage);
}
catch (TaskCanceledException)
{
    // HttpClient uses this exception when the request times out.
    Console.WriteLine("The request timed out.");
    PrintFallbacks(articleUrl, gdeltImage);
}
catch (Exception ex)
{
    // Report unexpected errors and display whatever fallbacks are available.
    Console.WriteLine($"Unexpected error: {ex.Message}");
    PrintFallbacks(articleUrl, gdeltImage);
}

// Prints the extracted values and their sources for testing and debugging.
static void PrintResult(
    string? pageTitle,
    string? headline,
    string? headlineSource,
    string? description,
    string? descriptionSource,
    string? image,
    string? imageSource,
    int htmlLength)
{
    Console.WriteLine();
    Console.WriteLine($"Page title: {Show(pageTitle)}");
    Console.WriteLine($"Headline: {Show(headline)}");
    Console.WriteLine($"Headline source: {Show(headlineSource)}");
    Console.WriteLine($"Description: {Show(description)}");
    Console.WriteLine($"Description source: {Show(descriptionSource)}");
    Console.WriteLine($"Image: {Show(image)}");
    Console.WriteLine($"Image source: {Show(imageSource)}");
    Console.WriteLine($"HTML downloaded: {htmlLength:N0} characters");
}

// Prints fallback values when the publisher page cannot be retrieved.
// No description is invented because there is no trustworthy source for it.
static void PrintFallbacks(string articleUrl, string? gdeltImage)
{
    var headline = CreateHeadlineFromUrl(articleUrl);

    Console.WriteLine();
    Console.WriteLine("Page title: (unavailable)");
    Console.WriteLine($"Headline: {Show(headline)}");
    Console.WriteLine(
        $"Headline source: {(HasValue(headline) ? "URL slug (generated)" : "(not found)")}"
    );
    Console.WriteLine("Description: (not found)");
    Console.WriteLine("Description source: (not found)");

    Console.WriteLine($"Image: {Show(gdeltImage)}");
    Console.WriteLine(
        $"Image source: {(HasValue(gdeltImage) ? "GDELT SharingImage" : "(not found)")}"
    );
    Console.WriteLine("HTML downloaded: 0 characters");
}

// Finds a metadata tag in the HTML and returns its content.
// For example, this can retrieve the content of the og:title property.
static string? GetMeta(
    IDocument document,
    string attribute,
    string value)
{
    return document
        .QuerySelector($"meta[{attribute}='{value}']")
        ?.GetAttribute("content")
        ?.Trim();
}

// Returns the first non-empty value in the supplied list.
// This is useful when a publisher supports more than one metadata field.
static string? FirstNonEmpty(params string?[] values)
{
    return values.FirstOrDefault(HasValue);
}

// Checks whether a string contains something other than null, empty text,
// or whitespace.
static bool HasValue(string? value)
{
    return !string.IsNullOrWhiteSpace(value);
}

// Replaces missing values with readable text in the console output.
static string Show(string? value)
{
    return HasValue(value) ? value! : "(not found)";
}

// Removes common publisher suffixes from an HTML page title.
// For example: "Example headline | Publisher Name" becomes "Example headline".
static string CleanPageTitle(string title)
{
    var cleaned = title.Trim();

    // Publishers use different separators before their names.
    string[] separators = [" | ", " - ", " – ", " — ", " :: "];

    foreach (var separator in separators)
    {
        // Search from the end so we remove the final publisher suffix.
        var index = cleaned.LastIndexOf(
            separator,
            StringComparison.Ordinal
        );

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
static string? CreateHeadlineFromUrl(string articleUrl)
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
        StringSplitOptions.RemoveEmptyEntries
    );

    if (segments.Length == 0)
    {
        return null;
    }

    // Get the last section of the URL and decode any encoded characters.
    // For example, "%20" represents a space in a URL.
    var slug = Uri.UnescapeDataString(
        segments[segments.Length - 1]
    );

    // Remove a file extension such as ".html", if one exists.
    var extension = Path.GetExtension(slug);

    if (!string.IsNullOrEmpty(extension))
    {
        slug = slug.Substring(0, slug.Length - extension.Length);
    }

    // Some publishers encode curly quotation marks as the numbers
    // 8216 and 8217 in their article URLs. Replace these with spaces.
    slug = slug.Replace("8216", " ");
    slug = slug.Replace("8217", " ");

    // Some URLs end with a long number representing a timestamp or ID.
    // Count how many digits appear at the end of the slug.
    int digitCount = 0;

    while (digitCount < slug.Length &&
           char.IsDigit(slug[slug.Length - 1 - digitCount]))
    {
        digitCount++;
    }

    // Only remove the number if it contains at least 12 digits.
    // This avoids removing ordinary numbers that might be part of a title.
    if (digitCount >= 12)
    {
        slug = slug.Substring(0, slug.Length - digitCount);
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
    slug = slug.Replace('-', ' ');
    slug = slug.Replace('_', ' ');

    // Split the slug into individual words, ignoring extra spaces.
    var words = slug.Split(
        ' ',
        StringSplitOptions.RemoveEmptyEntries
    );

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
/// JSON-LD can contain nested objects and arrays, so this method
/// calls itself to search inside them.
/// </summary>
static JsonElement? FindNewsArticle(JsonElement element)
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
/// Gets a named value from a JSON object.
/// The value might be a string, an array, or another object.
/// </summary>
static string? GetJsonValue(JsonElement element, string propertyName)
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
