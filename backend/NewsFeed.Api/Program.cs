
using Microsoft.EntityFrameworkCore;
using NewsFeed.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure the separate news-feed prototype database.
var repositoryRoot = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", ".."));

var databaseDirectory = Path.Combine(
    repositoryRoot,
    "research-data",
    "news-feed");

Directory.CreateDirectory(databaseDirectory);

var databasePath = Path.Combine(
    databaseDirectory,
    "news-feed.db");

builder.Services.AddDbContext<NewsFeedDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Return successfully enriched articles for the initial news feed.
// Failed records and BBC Sounds audio pages remain in storage but are
// excluded from this endpoint.
app.MapGet("/api/news", async (
    NewsFeedDbContext dbContext,
    int? limit,
    CancellationToken cancellationToken) =>
{
    var resultLimit = Math.Clamp(limit ?? 50, 1, 100);

    var articles = await dbContext.NewsArticles
        .AsNoTracking()
        .Where(article =>
            article.MetadataExtractionStatus == "Success")
        .Where(article =>
            article.Headline != null &&
            article.Headline != "")
        .Where(article =>
            article.DocumentIdentifier != null &&
            article.DocumentIdentifier != "")
        .Where(article =>
            !EF.Functions.Like(
                article.DocumentIdentifier!,
                "%/sounds/play/%"))
        .OrderByDescending(article => article.RecordTimestampUtc)
        .Take(resultLimit)
        .Select(article => new
        {
            article.Id,
            article.GkgRecordId,
            Headline = article.Headline,
            Description = article.Description,
            ImageUrl = article.ImageUrl,
            Publisher = article.SourceCommonName,
            PublishedAtUtc = article.RecordTimestampUtc,
            ArticleUrl = article.DocumentIdentifier,

            // Original GDELT intelligence fields for future UI features.
            Themes = article.V2Themes,
            Locations = article.V2Locations,
            People = article.V2Persons,
            Organisations = article.V2Organizations,
            Tone = article.V2Tone
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(articles);
})
.WithName("GetNewsArticles")
.WithSummary("Get enriched news articles for the news feed");

app.Run();
