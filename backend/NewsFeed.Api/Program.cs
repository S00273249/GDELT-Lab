
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AngularDev");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Return eligible, enriched articles using database-level pagination.
app.MapGet("/api/news", async (
    NewsFeedDbContext dbContext,
    int? page,
    int? pageSize,
    CancellationToken cancellationToken) =>
{
    var currentPage = Math.Max(page ?? 1, 1);
    var resultPageSize = Math.Clamp(pageSize ?? 12, 1, 50);

    // Keep the eligibility rules identical for the count and article queries.
    var eligibleArticles = dbContext.NewsArticles
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
                "%/sounds/play/%"));

    var totalCount = await eligibleArticles.CountAsync(
        cancellationToken);

    var totalPages = (int)Math.Ceiling(
        totalCount / (double)resultPageSize);

    var articles = await eligibleArticles
        .OrderByDescending(article => article.RecordTimestampUtc)
        .ThenByDescending(article => article.Id)
        .Skip((currentPage - 1) * resultPageSize)
        .Take(resultPageSize)
        .Select(article => new
        {
            article.Id,
            article.GkgRecordId,
            Headline = article.Headline,
            Description = article.Description,

            // Prefer publisher metadata, then use the GDELT image.
            ImageUrl =
                !string.IsNullOrWhiteSpace(article.ImageUrl)
                    ? article.ImageUrl
                    : article.GdeltSharingImage,

            Publisher = article.SourceCommonName,
            PublishedAtUtc = article.RecordTimestampUtc,
            ArticleUrl = article.DocumentIdentifier,

            // Original GDELT intelligence fields.
            Themes = article.V2Themes,
            Locations = article.V2Locations,
            People = article.V2Persons,
            Organisations = article.V2Organizations,
            Tone = article.V2Tone
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(new
    {
        Items = articles,
        Page = currentPage,
        PageSize = resultPageSize,
        TotalCount = totalCount,
        TotalPages = totalPages
    });
})
.WithName("GetNewsArticles")
.WithSummary("Get paginated, enriched news articles");


app.MapGet("/api/news/{id:int}", async (
    int id,
    NewsFeedDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var article = await dbContext.NewsArticles
        .AsNoTracking()
        .Where(article => article.Id == id)
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
        .Select(article => new
        {
            article.Id,
            article.GkgRecordId,
            Headline = article.Headline,
            Description = article.Description,
            ImageUrl =
                !string.IsNullOrWhiteSpace(article.ImageUrl)
                    ? article.ImageUrl
                    : article.GdeltSharingImage,
            Publisher = article.SourceCommonName,
            PublishedAtUtc = article.RecordTimestampUtc,
            ArticleUrl = article.DocumentIdentifier,
            Themes = article.V2Themes,
            Locations = article.V2Locations,
            People = article.V2Persons,
            Organisations = article.V2Organizations,
            Tone = article.V2Tone
        })
        .FirstOrDefaultAsync(cancellationToken);

    return article is null
        ? Results.NotFound()
        : Results.Ok(article);
})
.WithName("GetNewsArticleById")
.WithSummary("Get a single enriched news article by ID");


app.Run();
