
namespace GDELT_Lab.Models;

/// <summary>
/// Tracks the ingestion status and processing results for an individual
/// GDELT dataset file.
/// </summary>
public class IngestionFile
{
    // Database-generated identifier for the manifest entry.
    public int Id { get; set; }

    // Original filename and dataset category, such as GKG, Events or Mentions.
    public string FileName { get; set; } = string.Empty;
    public string DatasetType { get; set; } = string.Empty;

    // Full path to the source file used during ingestion.
    public string SourcePath { get; set; } = string.Empty;

    // Current processing state, e.g. Pending, Processing, Completed or Failed.
    public string Status { get; set; } = "Pending";

    // Optional checksum for identifying or validating the source file.
    public string? Checksum { get; set; }

    // Number of source records examined, successfully imported and skipped.
    public long RecordsScanned { get; set; }
    public long RecordsImported { get; set; }
    public long RecordsSkipped { get; set; }

    // UTC timestamps marking the start and completion of processing.
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    // Elapsed processing time for this file, measured in seconds.
    public double? ProcessingDurationSeconds { get; set; }

    // Error details recorded when ingestion fails.
    public string? ErrorMessage { get; set; }
}
