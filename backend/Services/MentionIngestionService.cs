
using System.Diagnostics;
using GDELT_Lab.Data;
using GDELT_Lab.Models;
using Microsoft.EntityFrameworkCore;

namespace GDELT_Lab.Services;

/// <summary>
/// Imports GDELT Mentions dataset files into the local database.
/// </summary>
public class MentionIngestionService
{
    // GDELT Mentions records contain 16 tab-separated fields.
    private const int ExpectedFieldCount = 16;

    // Process records in batches to limit EF Core change-tracking overhead.
    private const int BatchSize = 500;

    private readonly GdeltDbContext _db;

    public MentionIngestionService(GdeltDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Imports a single Mentions file and records its processing results.
    /// </summary>
    /// <param name="filePath">Path to the source dataset file.</param>
    /// <param name="cancellationToken">Token used to cancel ingestion.</param>
    /// <returns>The manifest entry containing the file's ingestion status and metrics.</returns>
    public async Task<IngestionFile> ImportFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        // Resolve the source path and verify that the file exists.
        filePath = Path.GetFullPath(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                "Mentions source file not found.", filePath);

        var fileName = Path.GetFileName(filePath);
        var stopwatch = Stopwatch.StartNew();

        // Find an existing manifest entry for this Mentions file.
        var manifest = await _db.IngestionFiles
            .SingleOrDefaultAsync(
                file => file.FileName == fileName &&
                        file.DatasetType == "Mentions",
                cancellationToken);

        // Avoid importing a file that has already completed successfully.
        if (manifest?.Status == "Completed")
            return manifest;

        // Create a manifest entry when the file is being processed for the first time.
        if (manifest == null)
        {
            manifest = new IngestionFile
            {
                FileName = fileName,
                DatasetType = "Mentions",
                SourcePath = filePath,
                Status = "Pending"
            };

            _db.IngestionFiles.Add(manifest);
        }

        // Reset the manifest metrics and status for this processing attempt.
        manifest.SourcePath = filePath;
        manifest.Status = "Processing";
        manifest.ErrorMessage = null;
        manifest.StartedAtUtc = DateTime.UtcNow;
        manifest.CompletedAtUtc = null;
        manifest.RecordsScanned = 0;
        manifest.RecordsImported = 0;
        manifest.RecordsSkipped = 0;

        // Persist the initial processing state.
        await _db.SaveChangesAsync(cancellationToken);

        var batch = new List<MentionRecord>(BatchSize);

        try
        {
            // Group all record inserts for this file within a database transaction.
            await using var transaction =
                await _db.Database.BeginTransactionAsync(cancellationToken);

            // Stream the file line by line instead of loading it entirely into memory.
            using var reader = new StreamReader(filePath);

            string? line;

            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                manifest.RecordsScanned++;

                // Split each source record into its tab-separated fields.
                var fields = line.Split('\t');

                // Skip records with an unexpected field count or missing event ID.
                if (fields.Length != ExpectedFieldCount ||
                    string.IsNullOrWhiteSpace(fields[0]))
                {
                    manifest.RecordsSkipped++;
                    continue;
                }

                // Convert the source fields into a MentionRecord entity.
                batch.Add(MapRecord(fields));

                // Save the batch once it reaches the configured size.
                if (batch.Count >= BatchSize)
                    await SaveBatchAsync(batch, manifest, cancellationToken);
            }

            // Save any remaining records in the final partial batch.
            if (batch.Count > 0)
                await SaveBatchAsync(batch, manifest, cancellationToken);

            // Record successful completion and elapsed processing time.
            manifest.Status = "Completed";
            manifest.CompletedAtUtc = DateTime.UtcNow;

            stopwatch.Stop();
            manifest.ProcessingDurationSeconds =
                stopwatch.Elapsed.TotalSeconds;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return manifest;
        }
        catch (Exception ex)
        {
            // Capture the failure details and processing duration.
            manifest.Status = "Failed";
            manifest.ErrorMessage = ex.Message.Length > 4000
                ? ex.Message[..4000]
                : ex.Message;
            manifest.CompletedAtUtc = DateTime.UtcNow;

            stopwatch.Stop();
            manifest.ProcessingDurationSeconds =
                stopwatch.Elapsed.TotalSeconds;

            // Detach any mention records still tracked by EF Core before
            // attempting to save the manifest's failure information.
            foreach (var entry in _db.ChangeTracker
                         .Entries<MentionRecord>().ToList())
            {
                entry.State = EntityState.Detached;
            }

            // Attempt to persist the failure status even if cancellation occurred.
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Saves a batch of mention records and updates the imported-record count.
    /// </summary>
    private async Task SaveBatchAsync(
        List<MentionRecord> batch,
        IngestionFile manifest,
        CancellationToken cancellationToken)
    {
        // Insert the current batch into the database.
        _db.MentionRecords.AddRange(batch);
        await _db.SaveChangesAsync(cancellationToken);

        // Update the manifest after the batch has been saved successfully.
        manifest.RecordsImported += batch.Count;

        // Detach saved records to keep EF Core's change tracker manageable.
        foreach (var record in batch)
            _db.Entry(record).State = EntityState.Detached;

        batch.Clear();
    }

    /// <summary>
    /// Maps the 16 tab-separated Mentions fields to a MentionRecord entity.
    /// </summary>
    private static MentionRecord MapRecord(string[] f)
    {
        return new MentionRecord
        {
            // Event identifier and associated dates.
            GlobalEventId = f[0],
            EventTimeDate = f[1],
            MentionTimeDate = f[2],

            // Mention classification and source identification.
            MentionType = f[3],
            MentionSourceName = EmptyToNull(f[4]),
            MentionIdentifier = EmptyToNull(f[5]),

            // Sentence containing the mention.
            SentenceId = f[6],

            // Character offsets for the actors and action in the source text.
            Actor1CharOffset = f[7],
            Actor2CharOffset = f[8],
            ActionCharOffset = f[9],

            // Source-text presence and mention confidence.
            InRawText = f[10],
            Confidence = f[11],

            // Length and tone of the source document.
            MentionDocLen = f[12],
            MentionDocTone = f[13],

            // Additional fields retained from the original source record.
            ReservedField14 = EmptyToNull(f[14]),
            ReservedField15 = EmptyToNull(f[15])
        };
    }

    /// <summary>
    /// Converts empty or whitespace-only source values to null.
    /// </summary>
    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
