
namespace GDELT_Lab.Services;

/// <summary>
/// Searches locally extracted GDELT dataset files without making
/// additional requests to the live GDELT API.
/// </summary>
public class GdeltLocalDatasetService
{
    private readonly string _dataRoot;

    public GdeltLocalDatasetService(IWebHostEnvironment environment)
    {
        // Resolve the location of the extracted research datasets
        // relative to the backend project's content root.
        _dataRoot = Path.GetFullPath(
            Path.Combine(
                environment.ContentRootPath,
                "..",
                "research-data",
                "extracted"));
    }

    /// <summary>
    /// Searches the selected local dataset and returns matching records.
    /// </summary>
    /// <param name="dataset">Dataset to search: events, mentions or gkg.</param>
    /// <param name="query">Optional case-insensitive text filter.</param>
    /// <param name="limit">Maximum number of matching records to return.</param>
    /// <param name="cancellationToken">Token used to cancel the search.</param>
    /// <returns>Search metadata and the matching records.</returns>
    public async Task<object> SearchAsync(
        string dataset,
        string? query,
        int limit,
        CancellationToken cancellationToken)
    {
        // Map each supported dataset name to its folder and filename pattern.
        var (folder, pattern) = dataset.ToLowerInvariant() switch
        {
            "events" => ("events", "*.export.CSV"),
            "mentions" => ("mentions", "*.mentions.CSV"),
            "gkg" => ("gkg", "*.gkg.csv"),
            _ => throw new ArgumentException(
                "Dataset must be events, mentions or gkg.")
        };

        var directory = Path.Combine(_dataRoot, folder);

        // Ensure the requested dataset directory exists.
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"Dataset directory not found: {directory}");
        }

        // Find matching files and examine the most recently modified first.
        var files = Directory.GetFiles(directory, pattern)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();

        if (files.Length == 0)
        {
            throw new FileNotFoundException(
                $"No {dataset} dataset files were found in {directory}.");
        }

        var results = new List<object>();
        var recordsScanned = 0;
        var normalisedQuery = query?.Trim();

        // Read files sequentially to avoid loading the entire dataset into memory.
        foreach (var file in files)
        {
            using var reader = new StreamReader(file);
            string? line;

            while ((line = await reader.ReadLineAsync(
                cancellationToken)) is not null)
            {
                // Stop processing promptly if the request has been cancelled.
                cancellationToken.ThrowIfCancellationRequested();
                recordsScanned++;

                // GDELT source files use tab-separated fields.
                var fields = line.Split('\t');

                if (fields.Length == 0)
                {
                    continue;
                }

                // When a query is supplied, match it against the entire
                // source line without distinguishing uppercase and lowercase.
                if (!string.IsNullOrWhiteSpace(normalisedQuery) &&
                    !line.Contains(
                        normalisedQuery,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (dataset.Equals(
                    "gkg",
                    StringComparison.OrdinalIgnoreCase))
                {
                    // GKG records in the current snapshot are expected
                    // to contain 27 fields.
                    if (fields.Length < 27)
                    {
                        continue;
                    }

                    // Return selected GKG columns using readable property names.
                    results.Add(new
                    {
                        dataset = "gkg",
                        recordId = fields[0],
                        timestamp = fields[1],
                        source = fields[3],
                        url = fields[4],
                        themes = fields[7],
                        locations = fields[9],
                        people = fields[11],
                        organisations = fields[13],
                        tone = fields[15]
                    });
                }
                else
                {
                    // Return the original fields for Events and Mentions
                    // until dedicated response mappings are implemented.
                    results.Add(new
                    {
                        dataset = dataset.ToLowerInvariant(),
                        recordId = fields[0],
                        fields
                    });
                }

                // Return immediately once the requested number of matches
                // has been collected.
                if (results.Count >= limit)
                {
                    return new
                    {
                        dataset = dataset.ToLowerInvariant(),
                        query = normalisedQuery,
                        filesAvailable = files.Length,
                        count = results.Count,
                        recordsScanned,
                        results
                    };
                }
            }
        }

        // Return all matches found if the search finishes before reaching the limit.
        return new
        {
            dataset = dataset.ToLowerInvariant(),
            query = normalisedQuery,
            filesAvailable = files.Length,
            count = results.Count,
            recordsScanned,
            results
        };
    }
}
