
using System.Diagnostics;
using GDELT_Lab.Data;
using GDELT_Lab.Models;
using Microsoft.EntityFrameworkCore;

namespace GDELT_Lab.Services;

/// <summary>
/// Imports GDELT Events dataset files into the local database.
/// </summary>
public class EventIngestionService
{
    // GDELT Events records contain 61 tab-separated fields.
    private const int ExpectedFieldCount = 61;

    // Save records in batches to limit the amount of tracked data in memory.
    private const int BatchSize = 500;

    private readonly GdeltDbContext _db;

    public EventIngestionService(GdeltDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Imports a single Events dataset file and records its processing results.
    /// </summary>
    /// <param name="filePath">Path to the source dataset file.</param>
    /// <param name="cancellationToken">Token used to cancel ingestion.</param>
    /// <returns>The manifest entry containing the file's ingestion status and metrics.</returns>
    public async Task<IngestionFile> ImportFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        // Resolve the source path and confirm the file exists.
        filePath = Path.GetFullPath(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                "Events source file not found.", filePath);

        var fileName = Path.GetFileName(filePath);
        var stopwatch = Stopwatch.StartNew();

        // Find an existing manifest entry for this dataset file.
        var manifest = await _db.IngestionFiles
            .SingleOrDefaultAsync(
                file => file.FileName == fileName &&
                        file.DatasetType == "Events",
                cancellationToken);

        // Avoid importing a file that has already completed successfully.
        if (manifest?.Status == "Completed")
            return manifest;

        // Create a manifest entry if this file has not been processed before.
        if (manifest == null)
        {
            manifest = new IngestionFile
            {
                FileName = fileName,
                DatasetType = "Events",
                SourcePath = filePath,
                Status = "Pending"
            };

            _db.IngestionFiles.Add(manifest);
        }

        // Reset the manifest for this processing attempt.
        manifest.SourcePath = filePath;
        manifest.Status = "Processing";
        manifest.ErrorMessage = null;
        manifest.StartedAtUtc = DateTime.UtcNow;
        manifest.CompletedAtUtc = null;
        manifest.RecordsScanned = 0;
        manifest.RecordsImported = 0;
        manifest.RecordsSkipped = 0;

        // Persist the initial status before importing records.
        await _db.SaveChangesAsync(cancellationToken);

        var batch = new List<EventRecord>(BatchSize);

        try
        {
            // Keep each file's record inserts within one database transaction.
            await using var transaction =
                await _db.Database.BeginTransactionAsync(cancellationToken);

            using var reader = new StreamReader(filePath);

            string? line;

            // Read the file one line at a time rather than loading it entirely into memory.
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                manifest.RecordsScanned++;

                // Split the tab-separated source record into its fields.
                var fields = line.Split('\t');

                // Skip malformed records and records without an event ID.
                if (fields.Length != ExpectedFieldCount ||
                    string.IsNullOrWhiteSpace(fields[0]))
                {
                    manifest.RecordsSkipped++;
                    continue;
                }

                // Map the source fields to the EventRecord entity.
                batch.Add(MapRecord(fields));

                // Persist each full batch before continuing through the file.
                if (batch.Count >= BatchSize)
                    await SaveBatchAsync(batch, manifest, cancellationToken);
            }

            // Save any remaining records that did not fill a complete batch.
            if (batch.Count > 0)
                await SaveBatchAsync(batch, manifest, cancellationToken);

            // Record successful completion and the total processing duration.
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
            // Record the failure details and processing duration.
            manifest.Status = "Failed";
            manifest.ErrorMessage = ex.Message.Length > 4000
                ? ex.Message[..4000]
                : ex.Message;
            manifest.CompletedAtUtc = DateTime.UtcNow;

            stopwatch.Stop();
            manifest.ProcessingDurationSeconds =
                stopwatch.Elapsed.TotalSeconds;

            // Detach any event records still tracked by EF Core before
            // attempting to save the manifest's failure information.
            foreach (var entry in _db.ChangeTracker
                         .Entries<EventRecord>().ToList())
            {
                entry.State = EntityState.Detached;
            }

            // Attempt to record the failure even if the original operation
            // was cancelled.
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Inserts a batch of event records and updates the imported-record count.
    /// </summary>
    private async Task SaveBatchAsync(
        List<EventRecord> batch,
        IngestionFile manifest,
        CancellationToken cancellationToken)
    {
        // Add and persist the current batch.
        _db.EventRecords.AddRange(batch);
        await _db.SaveChangesAsync(cancellationToken);

        // Update the manifest after the batch has been saved successfully.
        manifest.RecordsImported += batch.Count;

        // Detach inserted records to keep EF Core's change tracker small.
        foreach (var record in batch)
            _db.Entry(record).State = EntityState.Detached;

        batch.Clear();
    }

    /// <summary>
    /// Maps the 61 tab-separated GDELT Events fields to an EventRecord.
    /// </summary>
    private static EventRecord MapRecord(string[] f)
    {
        return new EventRecord
        {
            // Event identifier and date components.
            GlobalEventId = f[0],
            Day = f[1],
            MonthYear = f[2],
            Year = f[3],
            FractionDate = f[4],

            // Actor 1 identification and classification.
            Actor1Code = f[5],
            Actor1Name = f[6],
            Actor1CountryCode = f[7],
            Actor1KnownGroupCode = f[8],
            Actor1EthnicCode = f[9],
            Actor1Religion1Code = f[10],
            Actor1Religion2Code = f[11],
            Actor1Type1Code = f[12],
            Actor1Type2Code = f[13],
            Actor1Type3Code = f[14],

            // Actor 2 identification and classification.
            Actor2Code = f[15],
            Actor2Name = f[16],
            Actor2CountryCode = f[17],
            Actor2KnownGroupCode = f[18],
            Actor2EthnicCode = f[19],
            Actor2Religion1Code = f[20],
            Actor2Religion2Code = f[21],
            Actor2Type1Code = f[22],
            Actor2Type2Code = f[23],
            Actor2Type3Code = f[24],

            // Event classification, significance and reporting statistics.
            IsRootEvent = f[25],
            EventCode = f[26],
            EventBaseCode = f[27],
            EventRootCode = f[28],
            QuadClass = f[29],
            GoldsteinScale = f[30],
            NumMentions = f[31],
            NumSources = f[32],
            NumArticles = f[33],
            AvgTone = f[34],

            // Geographic information associated with Actor 1.
            Actor1GeoType = f[35],
            Actor1GeoFullName = f[36],
            Actor1GeoCountryCode = f[37],
            Actor1GeoAdm1Code = f[38],
            Actor1GeoAdm2Code = f[39],
            Actor1GeoLat = f[40],
            Actor1GeoLong = f[41],
            Actor1GeoFeatureId = f[42],

            // Geographic information associated with Actor 2.
            Actor2GeoType = f[43],
            Actor2GeoFullName = f[44],
            Actor2GeoCountryCode = f[45],
            Actor2GeoAdm1Code = f[46],
            Actor2GeoAdm2Code = f[47],
            Actor2GeoLat = f[48],
            Actor2GeoLong = f[49],
            Actor2GeoFeatureId = f[50],

            // Geographic information associated with the action.
            ActionGeoType = f[51],
            ActionGeoFullName = f[52],
            ActionGeoCountryCode = f[53],
            ActionGeoAdm1Code = f[54],
            ActionGeoAdm2Code = f[55],
            ActionGeoLat = f[56],
            ActionGeoLong = f[57],
            ActionGeoFeatureId = f[58],

            // Source publication timestamp and URL.
            DateAdded = f[59],
            SourceUrl = f[60]
        };
    }
}
