
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
        _dataRoot = Path.GetFullPath(
            Path.Combine(
                environment.ContentRootPath,
                "..",
                "research-data",
                "extracted"));
    }

    /// <summary>
    /// Searches the complete selected dataset and returns one page
    /// of matching records.
    /// </summary>
    public async Task<object> SearchAsync(
        string dataset,
        string? query,
        int limit,
        CancellationToken cancellationToken,
        int page = 1)
    {
        var normalisedDataset = dataset.ToLowerInvariant();

        var (folder, pattern) = normalisedDataset switch
        {
            "events" => ("events", "*.export.CSV"),
            "mentions" => ("mentions", "*.mentions.CSV"),
            "gkg" => ("gkg", "*.gkg.csv"),
            _ => throw new ArgumentException(
                "Dataset must be events, mentions or gkg.")
        };

        if (!Directory.Exists(_dataRoot))
        {
            throw new DirectoryNotFoundException(
                $"Extracted data directory not found: {_dataRoot}");
        }

        var directory = Path.Combine(_dataRoot, folder);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"Dataset directory not found: {directory}");
        }

        var files = Directory.GetFiles(directory, pattern)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();

        if (files.Length == 0)
        {
            throw new FileNotFoundException(
                $"No {dataset} dataset files were found in {directory}.");
        }

        // Keep page sizes within the supported range.
        limit = Math.Clamp(limit, 1, 100);
        page = Math.Max(page, 1);

        var offset = ((long)page - 1) * limit;
        var results = new List<object>(limit);
        long totalCount = 0;
        long recordsScanned = 0;

        var normalisedQuery = query?.Trim();

        // Scan every source file so totalCount represents all matches,
        // not just the records displayed on the current page.
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var reader = new StreamReader(file);
            string? line;

            while ((line = await reader.ReadLineAsync(
                cancellationToken)) is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                recordsScanned++;

                if (!string.IsNullOrWhiteSpace(normalisedQuery) &&
                    !line.Contains(
                        normalisedQuery,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fields = line.Split('\t');

                if (fields.Length == 0)
                {
                    continue;
                }

                // Preserve the existing GKG record validation.
                if (normalisedDataset == "gkg" && fields.Length < 27)
                {
                    continue;
                }

                // Count every valid matching record.
                totalCount++;

                // Retain only records belonging to the requested page.
                var zeroBasedIndex = totalCount - 1;

                if (zeroBasedIndex < offset ||
                    zeroBasedIndex >= offset + limit)
                {
                    continue;
                }

                if (normalisedDataset == "gkg")
                {
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
                    results.Add(new
                    {
                        dataset = normalisedDataset,
                        recordId = fields[0],
                        fields
                    });
                }
            }
        }

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling((double)totalCount / limit);

        return new
        {
            dataset = normalisedDataset,
            query = normalisedQuery,
            filesAvailable = files.Length,
            count = results.Count,
            totalCount,
            page,
            pageSize = limit,
            totalPages,
            recordsScanned,
            results
        };
    }
}
