
namespace GDELT_Lab.Models;

/// <summary>
/// Represents an individual GDELT Mentions record, containing information
/// about a source document mentioning a particular event.
/// </summary>
public class MentionRecord
{
    // Database-generated identifier for each mention record.
    // This allows multiple mentions with otherwise similar field values.
    public long Id { get; set; }

    // Identifies the event associated with this mention.
    public string GlobalEventId { get; set; } = string.Empty;

    // Dates associated with the event and the document mention.
    public string? EventTimeDate { get; set; }
    public string? MentionTimeDate { get; set; }

    // Classification and identification of the mention's source.
    public string? MentionType { get; set; }
    public string? MentionSourceName { get; set; }
    public string? MentionIdentifier { get; set; }

    // Sentence in the source document containing the mention.
    public string? SentenceId { get; set; }

    // Character offsets identifying the positions of the actors
    // and action within the source document.
    public string? Actor1CharOffset { get; set; }
    public string? Actor2CharOffset { get; set; }
    public string? ActionCharOffset { get; set; }

    // Indicates whether the event information appears in the raw text.
    public string? InRawText { get; set; }

    // Confidence score associated with the mention.
    public string? Confidence { get; set; }

    // Length and tone of the source document.
    public string? MentionDocLen { get; set; }
    public string? MentionDocTone { get; set; }

    // Additional source fields retained for completeness.
    public string? ReservedField14 { get; set; }
    public string? ReservedField15 { get; set; }
}
