
using System.Diagnostics;
using System.Globalization;
using GDELT_Lab.Data;
using GDELT_Lab.Models;
using Microsoft.EntityFrameworkCore;

namespace GDELT_Lab.Services;

/// <summary>
/// Imports GDELT Global Knowledge Graph (GKG) dataset files into the local database.
/// </summary>
public class GkgIngestionService
{
    // GKG source records contain 27 tab-separated fields.
    private const int ExpectedFieldCount = 27;

    // Process records in batches to limit EF Core change-tracking overhead.
    private const int BatchSize = 500;

    private readonly GdeltDbContext _db;

    public GkgIngestionService(GdeltDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Imports a single GKG file and records its processing status and metrics.
    /// </summary>
    /// <param name="filePath">Path to the source dataset file.</param>
    /// <param name="cancellationToken">Token used to cancel ingestion.</param>
    /// <returns>The manifest entry containing the file's ingestion results.</returns>
    public async Task<IngestionFile> ImportFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        // Resolve the source path and confirm that the file exists.
        filePath = Path.GetFullPath(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("GKG source file not found.", filePath);

        var fileName = Path.GetFileName(filePath);
        var stopwatch = Stopwatch.StartNew();

        // Look for an existing manifest entry for this GKG file.
        var manifest = await _db.IngestionFiles
            .SingleOrDefaultAsync(
                file => file.FileName == fileName &&
                        file.DatasetType == "GKG",
                cancellationToken);

        // Skip files that have already completed successfully.
        if (manifest?.Status == "Completed")
            return manifest;

        // Create a manifest entry for a file being processed for the first time.
        if (manifest == null)
        {
            manifest = new IngestionFile
            {
                FileName = fileName,
                DatasetType = "GKG",
                SourcePath = filePath,
                Status = "Pending"
            };

            _db.IngestionFiles.Add(manifest);
        }

        // Initialise the manifest for this ingestion attempt.
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

        var batch = new List<GkgRecord>(BatchSize);

        try
        {
            // Keep database changes for this file within one transaction.
            await using var transaction =
                await _db.Database.BeginTransactionAsync(cancellationToken);

            // Stream the source file instead of loading it entirely into memory.
            using var reader = new StreamReader(filePath);

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                manifest.RecordsScanned++;

                // Count blank lines as skipped records.
                if (string.IsNullOrWhiteSpace(line))
                {
                    manifest.RecordsSkipped++;
                    continue;
                }

                // Split the source line into its tab-separated fields.
                var fields = line.Split('\t');

                // Validate the field count, record identifier and timestamp.
                // GKG timestamps are expected in yyyyMMddHHmmss format.
                if (fields.Length != ExpectedFieldCount ||
                    string.IsNullOrWhiteSpace(fields[0]) ||
                    !DateTime.TryParseExact(
                        fields[1],
                        "yyyyMMddHHmmss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal |
                        DateTimeStyles.AdjustToUniversal,
                        out var timestamp))
                {
                    manifest.RecordsSkipped++;
                    continue;
                }

                // Convert the validated source fields into a GkgRecord entity.
                batch.Add(MapRecord(fields, timestamp));

                // Persist a batch whenever it reaches the configured size.
                if (batch.Count >= BatchSize)
                    await SaveBatchAsync(batch, manifest, cancellationToken);
            }

            // Persist any remaining records in the final partial batch.
            if (batch.Count > 0)
                await SaveBatchAsync(batch, manifest, cancellationToken);

            // Record successful completion and the elapsed processing time.
            manifest.Status = "Completed";
            manifest.CompletedAtUtc = DateTime.UtcNow;
            stopwatch.Stop();
            manifest.ProcessingDurationSeconds = stopwatch.Elapsed.TotalSeconds;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return manifest;
        }
        catch (Exception ex)
        {
            // Record the exception and mark this attempt as failed.
            manifest.Status = "Failed";
            manifest.ErrorMessage = ex.Message.Length > 4000
                ? ex.Message[..4000]
                : ex.Message;
            manifest.CompletedAtUtc = DateTime.UtcNow;

            stopwatch.Stop();
            manifest.ProcessingDurationSeconds = stopwatch.Elapsed.TotalSeconds;

            // Detach any GKG entities still tracked by EF Core before
            // attempting to save the manifest's failure information.
            foreach (var entry in _db.ChangeTracker.Entries<GkgRecord>().ToList())
                entry.State = EntityState.Detached;

            // Attempt to save failure information even if cancellation occurred.
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Saves a batch of GKG records and updates the manifest's import count.
    /// </summary>
    private async Task SaveBatchAsync(
        List<GkgRecord> batch,
        IngestionFile manifest,
        CancellationToken cancellationToken)
    {
        // Add the current batch to the context and persist it.
        _db.GkgRecords.AddRange(batch);
        await _db.SaveChangesAsync(cancellationToken);

        // Update the manifest only after the batch save succeeds.
        manifest.RecordsImported += batch.Count;

        // Detach saved entities to keep EF Core's change tracker manageable.
        foreach (var record in batch)
            _db.Entry(record).State = EntityState.Detached;

        batch.Clear();
    }

    /// <summary>
    /// Maps the 27 GKG source fields to the corresponding database entity.
    /// </summary>
    private static GkgRecord MapRecord(string[] fields, DateTime timestamp)
    {
        return new GkgRecord
        {
            // Record identifier and parsed UTC timestamp.
            GkgRecordId = fields[0],
            RecordTimestampUtc = timestamp,

            // Source and article identification.
            SourceCollectionIdentifier = fields[2],
            SourceCommonName = EmptyToNull(fields[3]),
            DocumentIdentifier = EmptyToNull(fields[4]),

            // Counts and extracted themes.
            Counts = EmptyToNull(fields[5]),
            V2Counts = EmptyToNull(fields[6]),
            Themes = EmptyToNull(fields[7]),
            V2Themes = EmptyToNull(fields[8]),

            // Geographic references.
            Locations = EmptyToNull(fields[9]),
            V2Locations = EmptyToNull(fields[10]),

            // People and organisations identified in the document.
            Persons = EmptyToNull(fields[11]),
            V2Persons = EmptyToNull(fields[12]),
            Organizations = EmptyToNull(fields[13]),
            V2Organizations = EmptyToNull(fields[14]),

            // Tone, dates and Global Content Analysis Measures.
            V2Tone = EmptyToNull(fields[15]),
            Dates = EmptyToNull(fields[16]),
            Gcam = EmptyToNull(fields[17]),

            // Images and embedded media.
            SharingImage = EmptyToNull(fields[18]),
            RelatedImages = EmptyToNull(fields[19]),
            SocialImageEmbeds = EmptyToNull(fields[20]),
            SocialVideoEmbeds = EmptyToNull(fields[21]),

            // Extracted quotations, names and monetary amounts.
            Quotations = EmptyToNull(fields[22]),
            AllNames = EmptyToNull(fields[23]),
            Amounts = EmptyToNull(fields[24]),

            // Translation metadata and additional source information.
            TranslationInfo = EmptyToNull(fields[25]),
            Extras = EmptyToNull(fields[26])
        };
    }

    /// <summary>
    /// Converts empty or whitespace-only source values to null.
    /// </summary>
    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
