
using GDELT_Lab.Data;
using GDELT_Lab.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Xml.Linq;

// dotnet run --project backend/GDELT-Lab.csproj --launch-profile http
namespace GDELT_Lab
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create the web application builder.
            var builder = WebApplication.CreateBuilder(args);

            // Register application services.
            builder.Services.AddHttpClient<GdeltSearchService>();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton<GdeltLocalDatasetService>();
            builder.Services.AddScoped<GkgIngestionService>();
            builder.Services.AddScoped<EventIngestionService>();
            builder.Services.AddScoped<MentionIngestionService>();

            // Allow requests from the Angular development server.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AngularFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Configure the SQLite database outside the backend project directory.
            var databaseDirectory = Path.GetFullPath(
                Path.Combine(
                    builder.Environment.ContentRootPath,
                    "..",
                    "..", node--version
npm--versionRemove - Item package -lock.json - Force - ErrorAction SilentlyContinue
                    "data"));

            Directory.CreateDirectory(databaseDirectory);

            var databasePath = Path.Combine(
                databaseDirectory,
                "gdelt-lab.db");

            builder.Services.AddDbContext<GdeltDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));

            var app = builder.Build();

            // Enable API documentation in development environments.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AngularFrontend");
            app.UseAuthorization();

            // Report whether the API is running.
            app.MapGet("/api/health", () => Results.Ok(new
            {
                status = "OK",
                message = "GDELT-Lab API is running"
            }));

            // Search the live GDELT DOC API.
            app.MapGet("/api/gdelt/search", async (
                string? query,
                GdeltSearchService service,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Results.BadRequest(new
                    {
                        error = "A search query is required."
                    });
                }

                try
                {
                    var result = await service.SearchAsync(
                        query,
                        cancellationToken);

                    return Results.Ok(result);
                }
                catch (HttpRequestException ex)
                {
                    return Results.Problem(
                        title: "GDELT request failed",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status502BadGateway);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(
                        title: "Invalid GDELT response",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status502BadGateway);
                }
            });

            // Search the local GKG dataset.
            app.MapGet("/api/gdelt/local-search", async (
                string? query,
                int? limit,
                GdeltLocalDatasetService service,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Results.BadRequest(new
                    {
                        error = "A search query is required."
                    });
                }

                var resultLimit = Math.Clamp(limit ?? 10, 1, 100);

                try
                {
                    var result = await service.SearchAsync(
                        "gkg",
                        query,
                        resultLimit,
                        cancellationToken);

                    return Results.Ok(result);
                }
                catch (DirectoryNotFoundException ex)
                {
                    return Results.Problem(
                        title: "Dataset directory not found",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
                catch (FileNotFoundException ex)
                {
                    return Results.Problem(
                        title: "GKG dataset file not found",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            });


            // Search any supported local dataset with pagination.
            app.MapGet("/api/gdelt/local/{dataset}", async (
                string dataset,
                string? query,
                int? limit,
                int? page,
                GdeltLocalDatasetService service,
                CancellationToken cancellationToken) =>
            {
                if (!IsSupportedDataset(dataset))
                {
                    return Results.BadRequest(new
                    {
                        error = "Dataset must be events, mentions or gkg."
                    });
                }

                try
                {
                    var result = await service.SearchAsync(
                        dataset,
                        query,
                        Math.Clamp(limit ?? 25, 1, 100),
                        cancellationToken,
                        Math.Max(page ?? 1, 1));

                    return Results.Ok(result);
                }
                catch (DirectoryNotFoundException ex)
                {
                    return Results.Problem(
                        title: "Dataset directory not found",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
                catch (FileNotFoundException ex)
                {
                    return Results.Problem(
                        title: "Dataset file not found",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            });


            // Import a specified GKG file.
            app.MapPost("/api/gdelt/ingest/gkg/import/{fileName}",
                async (
                    string fileName,
                    GkgIngestionService service,
                    IWebHostEnvironment environment,
                    CancellationToken cancellationToken) =>
                {
                    if (Path.GetFileName(fileName) != fileName ||
                        !fileName.EndsWith(
                            ".gkg.csv",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return Results.BadRequest(
                            "Supply a valid GKG filename, not a path.");
                    }

                    var filePath = Path.Combine(
                        GetDatasetDirectory(environment, "gkg"),
                        fileName);

                    if (!File.Exists(filePath))
                    {
                        return Results.NotFound(
                            $"GKG file not found: {fileName}");
                    }

                    var result = await service.ImportFileAsync(
                        filePath,
                        cancellationToken);

                    return Results.Ok(new
                    {
                        result.FileName,
                        result.DatasetType,
                        result.Status,
                        result.RecordsScanned,
                        result.RecordsImported,
                        result.RecordsSkipped,
                        result.ProcessingDurationSeconds,
                        result.ErrorMessage
                    });
                });

            // Verify the number of imported GKG records and inspect a sample.
            app.MapGet("/api/gdelt/ingest/gkg/verify",
                async (
                    GdeltDbContext db,
                    CancellationToken cancellationToken) =>
                {
                    var count = await db.GkgRecords.CountAsync(
                        cancellationToken);

                    var sample = await db.GkgRecords
                        .AsNoTracking()
                        .OrderBy(record => record.GkgRecordId)
                        .Select(record => new
                        {
                            record.GkgRecordId,
                            record.RecordTimestampUtc,
                            record.SourceCommonName,
                            record.DocumentIdentifier,
                            record.V2Tone,
                            record.Themes,
                            record.Locations
                        })
                        .FirstOrDefaultAsync(cancellationToken);

                    return Results.Ok(new
                    {
                        RecordCount = count,
                        Sample = sample
                    });
                });

            // Summarise ingestion results by dataset and processing status.
            app.MapGet("/api/gdelt/ingest/summary",
                async (
                    GdeltDbContext db,
                    CancellationToken cancellationToken) =>
                {
                    var summary = await db.IngestionFiles
                        .AsNoTracking()
                        .GroupBy(file => new
                        {
                            file.DatasetType,
                            file.Status
                        })
                        .Select(group => new
                        {
                            group.Key.DatasetType,
                            group.Key.Status,
                            FileCount = group.Count(),
                            RecordsScanned = group.Sum(
                                file => file.RecordsScanned),
                            RecordsImported = group.Sum(
                                file => file.RecordsImported),
                            RecordsSkipped = group.Sum(
                                file => file.RecordsSkipped)
                        })
                        .ToListAsync(cancellationToken);

                    return Results.Ok(summary);
                });

            // Import a specified Events file.
            app.MapPost("/api/gdelt/ingest/events/import/{fileName}",
                async (
                    string fileName,
                    EventIngestionService service,
                    IWebHostEnvironment environment,
                    CancellationToken cancellationToken) =>
                {
                    if (Path.GetFileName(fileName) != fileName ||
                        !fileName.EndsWith(
                            ".export.CSV",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return Results.BadRequest(
                            "Supply a valid Events filename, not a path.");
                    }

                    var filePath = Path.Combine(
                        GetDatasetDirectory(environment, "events"),
                        fileName);

                    if (!File.Exists(filePath))
                    {
                        return Results.NotFound(
                            $"Events file not found: {fileName}");
                    }

                    var result = await service.ImportFileAsync(
                        filePath,
                        cancellationToken);

                    return Results.Ok(new
                    {
                        result.FileName,
                        result.DatasetType,
                        result.Status,
                        result.RecordsScanned,
                        result.RecordsImported,
                        result.RecordsSkipped,
                        result.ProcessingDurationSeconds,
                        result.ErrorMessage
                    });
                });

            // Verify the number of imported Events records and inspect a sample.
            app.MapGet("/api/gdelt/ingest/events/verify",
                async (
                    GdeltDbContext db,
                    CancellationToken cancellationToken) =>
                {
                    var count = await db.EventRecords.CountAsync(
                        cancellationToken);

                    var sample = await db.EventRecords
                        .AsNoTracking()
                        .OrderBy(record => record.GlobalEventId)
                        .Select(record => new
                        {
                            record.GlobalEventId,
                            record.Day,
                            record.Actor1Name,
                            record.Actor2Name,
                            record.EventCode,
                            record.SourceUrl
                        })
                        .FirstOrDefaultAsync(cancellationToken);

                    return Results.Ok(new
                    {
                        RecordCount = count,
                        Sample = sample
                    });
                });

            // Import a specified Mentions file.
            app.MapPost("/api/gdelt/ingest/mentions/import/{fileName}",
                async (
                    string fileName,
                    MentionIngestionService service,
                    IWebHostEnvironment environment,
                    CancellationToken cancellationToken) =>
                {
                    if (Path.GetFileName(fileName) != fileName ||
                        !fileName.EndsWith(
                            ".mentions.CSV",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return Results.BadRequest(
                            "Supply a valid Mentions filename, not a path.");
                    }

                    var filePath = Path.Combine(
                        GetDatasetDirectory(environment, "mentions"),
                        fileName);

                    if (!File.Exists(filePath))
                    {
                        return Results.NotFound(
                            $"Mentions file not found: {fileName}");
                    }

                    var result = await service.ImportFileAsync(
                        filePath,
                        cancellationToken);

                    return Results.Ok(new
                    {
                        result.FileName,
                        result.DatasetType,
                        result.Status,
                        result.RecordsScanned,
                        result.RecordsImported,
                        result.RecordsSkipped,
                        result.ProcessingDurationSeconds,
                        result.ErrorMessage
                    });
                });

            // Verify the number of imported Mentions records and inspect a sample.
            app.MapGet("/api/gdelt/ingest/mentions/verify",
                async (
                    GdeltDbContext db,
                    CancellationToken cancellationToken) =>
                {
                    var count = await db.MentionRecords.CountAsync(
                        cancellationToken);

                    var sample = await db.MentionRecords
                        .AsNoTracking()
                        .OrderBy(record => record.Id)
                        .Select(record => new
                        {
                            record.Id,
                            record.GlobalEventId,
                            record.MentionTimeDate,
                            record.MentionSourceName,
                            record.MentionIdentifier
                        })
                        .FirstOrDefaultAsync(cancellationToken);

                    return Results.Ok(new
                    {
                        RecordCount = count,
                        Sample = sample
                    });
                });

            // Report ingestion counts and processing-time measurements.
            app.MapGet("/api/gdelt/ingest/measurements",
                async (
                    GdeltDbContext db,
                    CancellationToken cancellationToken) =>
                {
                    var files = await db.IngestionFiles
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                    var measurements = files
                        .GroupBy(file => file.DatasetType)
                        .Select(group => new
                        {
                            DatasetType = group.Key,
                            FileCount = group.Count(),
                            CompletedFiles = group.Count(
                                file => file.Status == "Completed"),
                            FailedFiles = group.Count(
                                file => file.Status == "Failed"),
                            RecordsScanned = group.Sum(
                                file => file.RecordsScanned),
                            RecordsImported = group.Sum(
                                file => file.RecordsImported),
                            RecordsSkipped = group.Sum(
                                file => file.RecordsSkipped),
                            TotalProcessingSeconds = Math.Round(
                                group.Sum(file =>
                                    file.ProcessingDurationSeconds ?? 0), 2),
                            AverageSecondsPerFile = Math.Round(
                                group.Average(file =>
                                    file.ProcessingDurationSeconds ?? 0), 3)
                        })
                        .OrderBy(result => result.DatasetType)
                        .ToList();

                    return Results.Ok(new
                    {
                        TotalFiles = files.Count,
                        TotalCompletedFiles = files.Count(
                            file => file.Status == "Completed"),
                        TotalFailedFiles = files.Count(
                            file => file.Status == "Failed"),
                        TotalRecordsImported = files.Sum(
                            file => file.RecordsImported),
                        Datasets = measurements
                    });
                });

            app.MapControllers();

            app.Run();
        }

        /// <summary>
        /// Checks whether a dataset name is supported by the local search API.
        /// </summary>
        private static bool IsSupportedDataset(string dataset) =>
            dataset.Equals("events", StringComparison.OrdinalIgnoreCase) ||
            dataset.Equals("mentions", StringComparison.OrdinalIgnoreCase) ||
            dataset.Equals("gkg", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Resolves the directory containing an extracted dataset.
        /// </summary>
        private static string GetDatasetDirectory(
            IWebHostEnvironment environment,
            string dataset)
        {
            if (!IsSupportedDataset(dataset))
            {
                throw new ArgumentException(
                    "Dataset must be events, mentions or gkg.",
                    nameof(dataset));
            }

            return Path.GetFullPath(
                Path.Combine(
                    environment.ContentRootPath,
                    "..",
                    "research-data",
                    "extracted",
                    dataset.ToLowerInvariant()));
        }
    }
}
