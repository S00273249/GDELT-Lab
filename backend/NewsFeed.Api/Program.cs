// dotnet run --project .\backend\NewsFeed.Api\NewsFeed.Api.csproj
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
    string? source,
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

    // Filter by source before counting and paginating.
    if (!string.IsNullOrWhiteSpace(source) &&
        !source.Equals(
            "All sources",
            StringComparison.OrdinalIgnoreCase))
    {
        var sourceIdentifiers = source.Trim() switch
        {
            "BBC News" => new[]
            {
                "bbc.co.uk",
                "bbc.com",
                "BBC News"
            },

            "RTÉ News" => new[]
            {
                "rte.ie",
                "RTÉ News"
            },

            "The Guardian" => new[]
            {
                "theguardian.com",
                "The Guardian"
            },

            "The Irish Times" => new[]
            {
                "irishtimes.com",
                "The Irish Times"
            },

            _ => Array.Empty<string>()
        };

        if (sourceIdentifiers.Length == 0)
        {
            return Results.BadRequest(new
            {
                Message = $"Unknown news source: {source}"
            });
        }

        eligibleArticles = eligibleArticles
            .Where(article =>
                article.SourceCommonName != null &&
                sourceIdentifiers.Contains(
                    article.SourceCommonName));
    }

    // Calculate totals after applying the optional source filter.
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


// Return related articles ranked by shared GDELT metadata.
app.MapGet("/api/news/{id:int}/related", async (
    int id,
    NewsFeedDbContext dbContext,
    int? limit,
    CancellationToken cancellationToken) =>
{
    var resultLimit = Math.Clamp(limit ?? 6, 1, 12);

    // Broad themes contribute less than specific connections.
    var broadThemes = new HashSet<string>(
        new[] { "EDUCATION" },
        StringComparer.OrdinalIgnoreCase);

    // Scoring weights are centralised for easier tuning.
    const double themeWeight = 1.0;
    const double broadThemeWeight = 0.25;
    const double maxThemeScore = 12.0;
    const double personWeight = 8.0;
    const double organisationWeight = 6.0;
    const double cityWeight = 3.0;
    const double countryWeight = 1.0;

    // Extract comparable values from GDELT metadata.
    static HashSet<string> ParseValues(
        string? raw,
        int fieldIndex = 0)
    {
        var values = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(raw))
            return values;

        foreach (var entry in raw.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = entry.Split(',');

            if (fields.Length <= fieldIndex)
                continue;

            var value = fields[fieldIndex].Trim();

            if (!string.IsNullOrWhiteSpace(value))
                values.Add(value);
        }

        return values;
    }

    static HashSet<string> ParseThemes(string? raw)
    {
        var themes = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(raw))
            return themes;

        foreach (var entry in raw.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries))
        {
            var theme = entry.Split(',')[0].Trim();

            if (string.IsNullOrWhiteSpace(theme))
                continue;

            // Exclude classifications that often connect unrelated
            // articles through shared ethnicity or language metadata.
            if (theme.StartsWith(
                    "TAX_ETHNICITY_",
                    StringComparison.OrdinalIgnoreCase) ||
                theme.StartsWith(
                    "TAX_WORLDLANGUAGES_",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            themes.Add(theme);
        }

        return themes;
    }

    static List<string> SharedValues(
        HashSet<string> current,
        HashSet<string> candidate)
    {
        return candidate
            .Where(current.Contains)
            .OrderBy(value => value)
            .ToList();
    }

    // Restrict results to articles with successfully extracted metadata,
    // a headline and an article URL. Exclude BBC audio pages, which are
    // not suitable as related news articles.
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

    var currentArticle = await eligibleArticles
        .Where(article => article.Id == id)
        .Select(article => new
        {
            article.Id,
            article.V2Themes,
            article.V2Persons,
            article.V2Organizations,
            article.V2Locations
        })
        .FirstOrDefaultAsync(cancellationToken);

    if (currentArticle is null)
    {
        return Results.NotFound(new
        {
            Message = $"Article {id} was not found."
        });
    }

    var currentThemes =
        ParseThemes(currentArticle.V2Themes);

    var currentPeople =
        ParseValues(currentArticle.V2Persons);

    var currentOrganisations =
        ParseValues(currentArticle.V2Organizations);

    var currentLocations =
        ParseLocations(currentArticle.V2Locations);

    var currentLocationKeys =
        ParseLocationKeys(currentArticle.V2Locations);

    // Without any extracted themes, people, organisations or locations,
    // there is no metadata available to establish relatedness.
    if (currentThemes.Count == 0 &&
        currentPeople.Count == 0 &&
        currentOrganisations.Count == 0 &&
        currentLocations.Count == 0)
    {
        return Results.Ok(Array.Empty<object>());
    }

    // Load eligible candidates with only the fields needed for scoring
    // and the response. GDELT metadata is parsed and compared in memory.
    var candidates = await eligibleArticles
        .Where(article => article.Id != id)
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
        .ToListAsync(cancellationToken);

    // Rank by relevance first, then prefer newer articles. Use the ID
    // as a final tie-breaker to keep the ordering consistent.
    var relatedArticles = candidates
        .Select(article =>
        {
            var candidateThemes =
                ParseThemes(article.Themes);

            var candidatePeople =
                ParseValues(article.People);

            var candidateOrganisations =
                ParseValues(article.Organisations);

            var candidateLocations =
                ParseLocations(article.Locations);

            var candidateLocationKeys =
                ParseLocationKeys(article.Locations);

            var sharedThemes =
                SharedValues(currentThemes, candidateThemes);

            var sharedPeople =
                SharedValues(currentPeople, candidatePeople);

            var sharedOrganisations =
                SharedValues(
                    currentOrganisations,
                    candidateOrganisations);

            // Preserve readable location names in the response.
            var sharedLocations =
                SharedValues(currentLocations, candidateLocations);

            // Compare locations using normalised geographic keys.
            var sharedLocationKeys =
                SharedValues(
                    currentLocationKeys,
                    candidateLocationKeys);

            var sharedCityKeys = sharedLocationKeys
                .Where(key => key.StartsWith(
                    "city:",
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            var sharedCountryKeys = sharedLocationKeys
                .Where(key => key.StartsWith(
                    "country:",
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Cap theme contributions so they cannot dominate ranking.
            var themeScore = sharedThemes.Sum(theme =>
                broadThemes.Contains(theme)
                    ? broadThemeWeight
                    : themeWeight);

            themeScore = Math.Min(themeScore, maxThemeScore);

            var score = themeScore;

            score += sharedPeople.Count * personWeight;
            score += sharedOrganisations.Count * organisationWeight;

            // Find countries already represented by exact city matches.
            var sharedCityCountryCodes = sharedCityKeys
                .Select(key => key.Split(':')[1])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Exact city matches receive the stronger geographic score.
            score += sharedCityKeys.Count * cityWeight;

            // Avoid counting a country's points again when a matching
            // city in that same country already contributes to the score.
            var countryOnlyMatches = sharedCountryKeys.Count(key =>
            {
                var parts = key.Split(':');

                return parts.Length >= 2 &&
                    !sharedCityCountryCodes.Contains(parts[1]);
            });

            score += countryOnlyMatches * countryWeight;

            // Return normalised city names and matching country codes
            // to help explain geographic connections.
            var sharedCityNames = sharedCityKeys
                .Select(key =>
                {
                    var parts = key.Split(':');
                    return parts.Length >= 3 ? parts[2] : key;
                })
                .OrderBy(value => value)
                .ToList();

            var sharedCountryCodes = sharedCountryKeys
                .Select(key =>
                {
                    var parts = key.Split(':');
                    return parts.Length >= 2 ? parts[1] : key;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList();

            return new
            {
                Article = article,
                SharedThemes = sharedThemes,
                SharedPeople = sharedPeople,
                SharedOrganisations = sharedOrganisations,
                SharedLocations = sharedLocations,
                SharedCityNames = sharedCityNames,
                SharedCountryCodes = sharedCountryCodes,
                Score = score
            };
        })
        .Where(result => result.Score > 0)
        .OrderByDescending(result => result.Score)
        .ThenByDescending(result => result.Article.PublishedAtUtc)
        .ThenByDescending(result => result.Article.Id)
        .Take(resultLimit)
        .Select(result => new
        {
            result.Article.Id,
            result.Article.GkgRecordId,
            result.Article.Headline,
            result.Article.Description,
            result.Article.ImageUrl,
            result.Article.Publisher,
            result.Article.PublishedAtUtc,
            result.Article.ArticleUrl,
            result.Article.Themes,
            result.Article.Locations,
            result.Article.People,
            result.Article.Organisations,
            result.Article.Tone,

            // Explain why this article was recommended.
            result.Score,
            result.SharedThemes,
            result.SharedPeople,
            result.SharedOrganisations,
            result.SharedLocations,
            result.SharedCityNames,
            result.SharedCountryCodes
        })
        .ToList();

    return Results.Ok(relatedArticles);
})
.WithName("GetRelatedNewsArticles")
.WithSummary("Get related articles ranked by shared GDELT metadata");


// Parse readable GDELT location names.
static HashSet<string> ParseLocations(string? raw)
{
    var locations = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase);

    if (string.IsNullOrWhiteSpace(raw))
        return locations;

    foreach (var entry in raw.Split(
        ';',
        StringSplitOptions.RemoveEmptyEntries))
    {
        var fields = entry.Split('#');

        // GDELT V2Locations:
        // type#full name#country code#...
        if (fields.Length > 1)
        {
            var name = fields[1].Trim();

            if (!string.IsNullOrWhiteSpace(name))
                locations.Add(name);
        }
    }

    return locations;
}


// Build comparable city and country keys from GDELT locations.
static HashSet<string> ParseLocationKeys(string? raw)
{
    var keys = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase);

    if (string.IsNullOrWhiteSpace(raw))
        return keys;

    // Recognise canonical country names to avoid treating demonyms as
    // countries. This starter list should be expanded as needed for
    // countries present in the GDELT data.
    var countryNames = new HashSet<string>(
        new[]
        {
            "Russia",
            "Ukraine",
            "United States",
            "United Kingdom",
            "Ireland",
            "Germany",
            "France",
            "Poland",
            "Canada",
            "Finland",
            "Estonia",
            "Latvia",
            "Belgium",
            "Israel",
            "Iran",
            "Luxembourg"
        },
        StringComparer.OrdinalIgnoreCase);

    foreach (var entry in raw.Split(
        ';',
        StringSplitOptions.RemoveEmptyEntries))
    {
        var fields = entry.Split('#');

        if (fields.Length < 3)
            continue;

        var locationType = fields[0].Trim();
        var locationName = fields[1].Trim();
        var countryCode = fields[2].Trim()
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(locationName))
            continue;

        // Type 1 can contain country names and demonyms.
        // Only recognise canonical country names here.
        if (locationType == "1" &&
            !string.IsNullOrWhiteSpace(countryCode) &&
            countryNames.Contains(locationName))
        {
            keys.Add($"country:{countryCode}");
        }

        // Type 4 represents city-level locations.
        if (locationType == "4" &&
            !string.IsNullOrWhiteSpace(countryCode))
        {
            var cityName = locationName
                .Split(',')[0]
                .Trim();

            if (!string.IsNullOrWhiteSpace(cityName))
            {
                keys.Add(
                    $"city:{countryCode}:{NormalizeLocationName(cityName)}");
            }

            // A city also identifies its associated country.
            keys.Add($"country:{countryCode}");
        }
    }

    return keys;
}


// Normalise city names so common spelling variations can match.
static string NormalizeLocationName(string name)
{
    var normalized = string.Concat(
        name.Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit));

    return normalized switch
    {
        "kyyiv" => "kyiv",
        "kiev" => "kyiv",
        _ => normalized
    };
}

app.Run();
