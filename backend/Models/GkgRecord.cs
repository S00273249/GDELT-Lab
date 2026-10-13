namespace GDELT_Lab.Models;

/// <summary>
/// Represents a record from the GDELT Global Knowledge Graph (GKG) dataset.
/// Encoded source fields are preserved as strings, while the record
/// timestamp is parsed into a UTC DateTime for time-based queries.
/// </summary>
public class GkgRecord
{
    // Unique identifier for the GKG record.
    public string GkgRecordId { get; set; } = string.Empty;

    // Record timestamp parsed from the source DATE field.
    public DateTime RecordTimestampUtc { get; set; }

    // Source collection and article identification.
    public string? SourceCollectionIdentifier { get; set; }
    public string? SourceCommonName { get; set; }
    public string? DocumentIdentifier { get; set; }

    // Counts and themes identified in the document.
    public string? Counts { get; set; }
    public string? V2Counts { get; set; }
    public string? Themes { get; set; }
    public string? V2Themes { get; set; }

    // Geographic locations and named entities.
    public string? Locations { get; set; }
    public string? V2Locations { get; set; }
    public string? Persons { get; set; }
    public string? V2Persons { get; set; }
    public string? Organizations { get; set; }
    public string? V2Organizations { get; set; }

    // Tone, date references and Global Content Analysis Measures.
    public string? V2Tone { get; set; }
    public string? Dates { get; set; }
    public string? Gcam { get; set; }

    // Images and media associated with the document.
    public string? SharingImage { get; set; }
    public string? RelatedImages { get; set; }
    public string? SocialImageEmbeds { get; set; }
    public string? SocialVideoEmbeds { get; set; }

    // Extracted quotations, names, amounts, translation data and metadata.
    public string? Quotations { get; set; }
    public string? AllNames { get; set; }
    public string? Amounts { get; set; }
    public string? TranslationInfo { get; set; }
    public string? Extras { get; set; }
}