
namespace NewsFeed.Api.Models;

/// <summary>
/// A selected GDELT GKG article enriched with publisher metadata
/// for the news-feed prototype.
/// Original GDELT values are retained for intelligence display
/// and future processing.
/// </summary>
public class NewsArticle
{
    // Internal identifier for the prototype article.
    public long Id { get; set; }

    // Original GDELT GKG identity and source information.
    public string GkgRecordId { get; set; } = string.Empty;
    public DateTime RecordTimestampUtc { get; set; }
    public string? SourceCollectionIdentifier { get; set; }
    public string? SourceCommonName { get; set; }
    public string? DocumentIdentifier { get; set; }

    // Publisher metadata extracted from the article webpage.
    public string? Headline { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    // Preserve GDELT's image separately from the selected display image.
    public string? GdeltSharingImage { get; set; }

    // Record which metadata source supplied each value.
    public string? HeadlineSource { get; set; }
    public string? DescriptionSource { get; set; }
    public string? ImageSource { get; set; }

    // Track enrichment attempts without discarding the article on failure.
    public string MetadataExtractionStatus { get; set; } = "Pending";
    public DateTime? MetadataLastAttemptUtc { get; set; }
    public string? MetadataError { get; set; }

    // Original GDELT GKG intelligence fields.
    public string? Counts { get; set; }
    public string? V2Counts { get; set; }
    public string? Themes { get; set; }
    public string? V2Themes { get; set; }
    public string? Locations { get; set; }
    public string? V2Locations { get; set; }
    public string? Persons { get; set; }
    public string? V2Persons { get; set; }
    public string? Organizations { get; set; }
    public string? V2Organizations { get; set; }
    public string? V2Tone { get; set; }
    public string? Dates { get; set; }
    public string? Gcam { get; set; }

    // Additional original GDELT information.
    public string? RelatedImages { get; set; }
    public string? SocialImageEmbeds { get; set; }
    public string? SocialVideoEmbeds { get; set; }
    public string? Quotations { get; set; }
    public string? AllNames { get; set; }
    public string? Amounts { get; set; }
    public string? TranslationInfo { get; set; }
    public string? Extras { get; set; }
}
