
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NewsFeed.Api.Data;
using NewsFeed.Api.Models;
using ArticleMetadata;

// Project 300 - News Feed Sample Data Preparation
//
// Import the selected GDELT GKG records into the separate news-feed
// prototype database. The original database is opened in read-only mode.
//
// This process preserves the original GDELT intelligence fields and
// initialises metadata extraction tracking. Publisher-page enrichment
// is handled separately so that failures can be retried independently.

var repositoryRoot = Directory.GetCurrentDirectory();

// Run publisher metadata enrichment independently of the source import.
if (args.Length > 0 &&
    args[0].Equals("--enrich", StringComparison.OrdinalIgnoreCase))
{
    var enrichmentDirectory = Path.Combine(
        repositoryRoot,
        "research-data",
        "news-feed");

    var enrichmentDatabasePath = Path.Combine(
        enrichmentDirectory,
        "news-feed.db");

    if (!File.Exists(enrichmentDatabasePath))
    {
        Console.Error.WriteLine(
            $"News-feed database not found: {enrichmentDatabasePath}");
        Console.Error.WriteLine(
            "Run the normal import command first.");
        return;
    }

    var limit = int.TryParse(args.ElementAtOrDefault(1), out var requestedLimit)
        ? Math.Clamp(requestedLimit, 1, 544)
        : 10;

    await EnrichArticlesAsync(enrichmentDatabasePath, limit);
    return;
}

var sourceCandidates = new[]
{
    Path.Combine(repositoryRoot, "data", "gdelt-lab.db"),
    Path.Combine(repositoryRoot, "backend", "data", "gdelt-lab.db")
};

var sourceDatabasePath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : sourceCandidates.FirstOrDefault(File.Exists);

if (sourceDatabasePath is null || !File.Exists(sourceDatabasePath))
{
    Console.Error.WriteLine("Could not find the source GDELT database.");
    Console.Error.WriteLine(
        "Run from the repository root or pass the source database path as an argument.");
    return;
}

// Use the same destination path as NewsFeed.Api.
var destinationDirectory = Path.Combine(
    repositoryRoot,
    "research-data",
    "news-feed");

Directory.CreateDirectory(destinationDirectory);

var destinationDatabasePath = Path.Combine(
    destinationDirectory,
    "news-feed.db");

var selectedPublisherDomains = new[]
{
    "bbc.co.uk",
    "bbc.com",
    "theguardian.com",
    "irishtimes.com",
    "rte.ie"
};

var publisherParameters = string.Join(
    ", ",
    selectedPublisherDomains.Select((_, index) => $"@publisher{index}"));

var sourceConnectionString = new SqliteConnectionStringBuilder
{
    DataSource = sourceDatabasePath,
    Mode = SqliteOpenMode.ReadOnly
}.ToString();

var destinationConnectionString =
    $"Data Source={destinationDatabasePath}";

Console.WriteLine("News Feed Sample Data Import");
Console.WriteLine("============================");
Console.WriteLine($"Source:      {sourceDatabasePath}");
Console.WriteLine($"Destination: {destinationDatabasePath}");
Console.WriteLine();

var sourceArticles = new List<NewsArticle>();

// Read the selected GKG records without modifying the original database.
await using (var sourceConnection =
    new SqliteConnection(sourceConnectionString))
{
    await sourceConnection.OpenAsync();

    var sourceSql = $"""
        SELECT
            GkgRecordId,
            RecordTimestampUtc,
            SourceCollectionIdentifier,
            SourceCommonName,
            DocumentIdentifier,
            Counts,
            V2Counts,
            Themes,
            V2Themes,
            Locations,
            V2Locations,
            Persons,
            V2Persons,
            Organizations,
            V2Organizations,
            V2Tone,
            Dates,
            Gcam,
            SharingImage,
            RelatedImages,
            SocialImageEmbeds,
            SocialVideoEmbeds,
            Quotations,
            AllNames,
            Amounts,
            TranslationInfo,
            Extras
        FROM GkgRecords
        WHERE SourceCommonName IN ({publisherParameters})
        ORDER BY RecordTimestampUtc DESC, GkgRecordId;
        """;

    await using var command = sourceConnection.CreateCommand();
    command.CommandText = sourceSql;

    for (var i = 0; i < selectedPublisherDomains.Length; i++)
    {
        command.Parameters.AddWithValue(
            $"@publisher{i}",
            selectedPublisherDomains[i]);
    }

    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        var timestampText = ReadRequiredString(
            reader, "RecordTimestampUtc");

        var timestamp = DateTime.Parse(
            timestampText,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal |
            DateTimeStyles.AdjustToUniversal);

        var article = new NewsArticle
        {
            // Original GDELT identity and source information.
            GkgRecordId = ReadRequiredString(reader, "GkgRecordId"),
            RecordTimestampUtc = timestamp,
            SourceCollectionIdentifier =
                ReadNullableString(reader, "SourceCollectionIdentifier"),
            SourceCommonName =
                ReadNullableString(reader, "SourceCommonName"),
            DocumentIdentifier =
                ReadNullableString(reader, "DocumentIdentifier"),

            // Publisher metadata will be populated in the enrichment step.
            Headline = null,
            Description = null,
            ImageUrl = null,

            // Retain the original GDELT image independently.
            GdeltSharingImage =
                ReadNullableString(reader, "SharingImage"),

            // Track metadata extraction separately from source import.
            HeadlineSource = null,
            DescriptionSource = null,
            ImageSource = null,
            MetadataExtractionStatus = "Pending",
            MetadataLastAttemptUtc = null,
            MetadataError = null,

            // Original GDELT intelligence fields.
            Counts = ReadNullableString(reader, "Counts"),
            V2Counts = ReadNullableString(reader, "V2Counts"),
            Themes = ReadNullableString(reader, "Themes"),
            V2Themes = ReadNullableString(reader, "V2Themes"),
            Locations = ReadNullableString(reader, "Locations"),
            V2Locations = ReadNullableString(reader, "V2Locations"),
            Persons = ReadNullableString(reader, "Persons"),
            V2Persons = ReadNullableString(reader, "V2Persons"),
            Organizations = ReadNullableString(reader, "Organizations"),
            V2Organizations =
                ReadNullableString(reader, "V2Organizations"),
            V2Tone = ReadNullableString(reader, "V2Tone"),
            Dates = ReadNullableString(reader, "Dates"),
            Gcam = ReadNullableString(reader, "Gcam"),

            // Additional original GDELT information.
            RelatedImages = ReadNullableString(reader, "RelatedImages"),
            SocialImageEmbeds =
                ReadNullableString(reader, "SocialImageEmbeds"),
            SocialVideoEmbeds =
                ReadNullableString(reader, "SocialVideoEmbeds"),
            Quotations = ReadNullableString(reader, "Quotations"),
            AllNames = ReadNullableString(reader, "AllNames"),
            Amounts = ReadNullableString(reader, "Amounts"),
            TranslationInfo =
                ReadNullableString(reader, "TranslationInfo"),
            Extras = ReadNullableString(reader, "Extras")
        };

        sourceArticles.Add(article);
    }
}

Console.WriteLine(
    $"Records read from original GDELT database: {sourceArticles.Count:N0}");

if (sourceArticles.Count != 544)
{
    Console.Error.WriteLine(
        "Import stopped: expected 544 source records. No destination records were added.");
    return;
}

var sourceIds = sourceArticles
    .Select(article => article.GkgRecordId)
    .ToHashSet(StringComparer.Ordinal);

if (sourceIds.Count != sourceArticles.Count)
{
    Console.Error.WriteLine(
        "Import stopped: duplicate GKG record IDs were found in the source selection.");
    return;
}

// Connect to the separate prototype database using the API's EF Core model.
var options = new DbContextOptionsBuilder<NewsFeedDbContext>()
    .UseSqlite(destinationConnectionString)
    .Options;

await using var dbContext = new NewsFeedDbContext(options);

// Apply the existing API migrations rather than creating a separate schema.
await dbContext.Database.MigrateAsync();

// Avoid reimporting records when this preparation tool is run again.
var existingIds = await dbContext.NewsArticles
    .Where(article => sourceIds.Contains(article.GkgRecordId))
    .Select(article => article.GkgRecordId)
    .ToListAsync();

var existingIdSet = existingIds.ToHashSet(StringComparer.Ordinal);

var newArticles = sourceArticles
    .Where(article => !existingIdSet.Contains(article.GkgRecordId))
    .ToList();

dbContext.NewsArticles.AddRange(newArticles);

await dbContext.SaveChangesAsync();

var importedSelectedCount = await dbContext.NewsArticles
    .CountAsync(article => sourceIds.Contains(article.GkgRecordId));

var totalDestinationCount =
    await dbContext.NewsArticles.CountAsync();

var distinctDestinationIds = await dbContext.NewsArticles
    .Select(article => article.GkgRecordId)
    .Distinct()
    .CountAsync();

Console.WriteLine();
Console.WriteLine("Import verification");
Console.WriteLine("-------------------");
Console.WriteLine($"Source records selected:    {sourceArticles.Count:N0}");
Console.WriteLine($"Records already present:    {existingIdSet.Count:N0}");
Console.WriteLine($"New records inserted:       {newArticles.Count:N0}");
Console.WriteLine($"Selected records in target: {importedSelectedCount:N0}");
Console.WriteLine($"Total target records:       {totalDestinationCount:N0}");
Console.WriteLine($"Distinct GKG record IDs:    {distinctDestinationIds:N0}");

if (importedSelectedCount == 544 &&
    distinctDestinationIds == totalDestinationCount)
{
    Console.WriteLine();
    Console.WriteLine("SUCCESS: The sample dataset has been imported and verified.");
}
else
{
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "WARNING: Verification did not match expectations. Investigate before enrichment.");
}

Console.WriteLine(
    "The original GDELT database was opened in read-only mode.");

// Read a required SQLite value as a string, rejecting unexpected NULLs.
static string ReadRequiredString(
    SqliteDataReader reader,
    string columnName)
{
    var value = ReadNullableString(reader, columnName);

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidDataException(
            $"Required source column '{columnName}' is empty.");
    }

    return value;
}

// Read nullable GDELT fields without converting database NULLs to text.
static string? ReadNullableString(
    SqliteDataReader reader,
    string columnName)
{
    var value = reader[columnName];

    return value is DBNull
        ? null
        : Convert.ToString(value, CultureInfo.InvariantCulture);
}

// Enrich a limited batch of imported articles using publisher metadata.
static async Task EnrichArticlesAsync(
    string databasePath,
    int limit)
{
    var options = new DbContextOptionsBuilder<NewsFeedDbContext>()
        .UseSqlite($"Data Source={databasePath}")
        .Options;

    await using var dbContext = new NewsFeedDbContext(options);

    var articles = await dbContext.NewsArticles
        .Where(article => article.MetadataExtractionStatus == "Pending")
        .OrderByDescending(article => article.RecordTimestampUtc)
        .Take(limit)
        .ToListAsync();

    Console.WriteLine("News Feed Metadata Enrichment");
    Console.WriteLine("==============================");
    Console.WriteLine($"Pending articles selected: {articles.Count}");
    Console.WriteLine();

    if (articles.Count == 0)
    {
        Console.WriteLine("No pending articles to process.");
        return;
    }

    using var extractor = new ArticleMetadataExtractor();

    var completed = 0;
    var failed = 0;

    foreach (var article in articles)
    {
        Console.WriteLine($"[{completed + failed + 1}/{articles.Count}]");
        Console.WriteLine($"Publisher: {article.SourceCommonName}");
        Console.WriteLine($"URL: {article.DocumentIdentifier}");

        var result = await extractor.ExtractAsync(
            article.DocumentIdentifier ?? string.Empty,
            article.GdeltSharingImage);

        // Store the extracted metadata without changing the original
        // GDELT intelligence fields.
        article.Headline = result.Headline;
        article.Description = result.Description;
        article.ImageUrl = result.ImageUrl;

        article.HeadlineSource = result.HeadlineSource;
        article.DescriptionSource = result.DescriptionSource;
        article.ImageSource = result.ImageSource;

        article.MetadataExtractionStatus = result.Status;
        article.MetadataLastAttemptUtc = DateTime.UtcNow;
        article.MetadataError = result.ErrorMessage;

        // Save every article immediately so progress survives interruption.
        await dbContext.SaveChangesAsync();

        if (result.Status == "Success")
        {
            completed++;
        }
        else
        {
            failed++;
        }

        Console.WriteLine($"Status: {result.Status}");
        Console.WriteLine($"Headline: {result.Headline ?? "(none)"}");
        Console.WriteLine($"Description: {result.Description ?? "(none)"}");
        Console.WriteLine($"Image: {result.ImageUrl ?? "(none)"}");
        Console.WriteLine();

        // Avoid sending requests to publishers too rapidly.
        await Task.Delay(TimeSpan.FromMilliseconds(750));
    }

    Console.WriteLine("Enrichment summary");
    Console.WriteLine("------------------");
    Console.WriteLine($"Processed: {articles.Count}");
    Console.WriteLine($"Successful page retrievals: {completed}");
    Console.WriteLine($"Other outcomes: {failed}");
    Console.WriteLine();
    Console.WriteLine("Enrichment results saved to the prototype database.");
}
