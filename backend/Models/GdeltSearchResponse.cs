
using System.Text.Json.Serialization;

namespace GDELT_Lab.Models;

/// <summary>
/// Represents the response returned by the GDELT DOC API search endpoint.
/// </summary>
public class GdeltSearchResponse
{
    // Collection of articles returned by the search request.
    [JsonPropertyName("articles")]
    public List<GdeltArticle> Articles { get; set; } = [];
}

/// <summary>
/// Represents an individual article returned by the GDELT DOC API.
/// </summary>
public class GdeltArticle
{
    // URL of the article.
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    // Alternative URL intended for mobile devices, when available.
    [JsonPropertyName("url_mobile")]
    public string? UrlMobile { get; set; }

    // Article headline.
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    // Date and time when GDELT recorded seeing the article.
    [JsonPropertyName("seendate")]
    public string? SeenDate { get; set; }

    // URL of an associated social image, when available.
    [JsonPropertyName("socialimage")]
    public string? SocialImage { get; set; }

    // Domain from which the article originated.
    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    // Language in which the article is published.
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    // Country associated with the article's source.
    [JsonPropertyName("sourcecountry")]
    public string? SourceCountry { get; set; }
}
