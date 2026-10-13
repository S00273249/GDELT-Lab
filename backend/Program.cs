
using GDELT_Lab.Services;
using GDELT_Lab.Data;
using Microsoft.EntityFrameworkCore;

namespace GDELT_Lab
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create the web application builder.
            var builder = WebApplication.CreateBuilder(args);

            // Register services.
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

            // Configure the SQLite database path.
            var databaseDirectory = Path.GetFullPath(
                Path.Combine(
                    builder.Environment.ContentRootPath,
                    "..",
                    "data"));

            Directory.CreateDirectory(databaseDirectory);

            var databasePath = Path.Combine(
                databaseDirectory,
                "gdelt-lab.db");

            // Register the GdeltDbContext with SQLite.
            builder.Services.AddDbContext<GdeltDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));

            // Build the application.
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AngularFrontend");
            app.UseAuthorization();

            // Health endpoint.
            app.MapGet("/api/health", () => Results.Ok(new
            {
                status = "OK",
                message = "GDELT-Lab API is running"
            }));

            // Live GDELT DOC API search endpoint.
            app.MapGet("/api/gdelt/search", async (
                string? query,
                GdeltSearchService gdeltSearchService,
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
                    var result = await gdeltSearchService.SearchAsync(
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

            // Local GKG search endpoint.
            // Uses the shared local dataset service.
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

            // Local Events, Mentions and GKG search endpoint.
            app.MapGet("/api/gdelt/local/{dataset}", async (
                string dataset,
                string? query,
                int? limit,
                GdeltLocalDatasetService service,
                CancellationToken cancellationToken) =>
            {
                if (!new[] { "events", "mentions", "gkg" }
                    .Contains(dataset, StringComparer.OrdinalIgnoreCase))
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
                        Math.Clamp(limit ?? 10, 1, 100),
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
                        title: "Dataset file not found",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            });

            // GKG ingestion test endpoint.
            app.MapPost("/api/gdelt/ingest/gkg/test-file",
                async (
                    GkgIngestionService service,
                    IWebHostEnvironment environment,
                    CancellationToken cancellationToken) =>
                {
                    var datasetDirectory = Path.GetFullPath(
                        Path.Combine(
                            environment.ContentRootPath,
                            "..",
                            "research-data",
                            "extracted",
                            "gkg"));

                    if (!Directory.Exists(datasetDirectory))
                    {
                        return Results.Problem(
                            "The extracted GKG dataset directory was not found.");
                    }

                    var filePath = Directory
                        .GetFiles(datasetDirectory, "*.csv")
                        .OrderBy(path => Path.GetFileName(path))
                        .FirstOrDefault();

                    if (filePath == null)
                    {
                        return Results.Problem(
                            "No extracted GKG CSV files were found.");
                    }

                    var result = await service.ImportFileAsync(
                        filePath, cancellationToken);

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

            // GKG ingestion verification endpoint.
            app.MapGet("/api/gdelt/ingest/gkg/verify",
            async (GdeltDbContext db, CancellationToken ct) =>
            {
                var count = await db.GkgRecords.CountAsync(ct);

                var sample = await db.GkgRecords
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
                    .FirstOrDefaultAsync(ct);

                return Results.Ok(new
                {
                    RecordCount = count,
                    Sample = sample
                });
            });


            app.MapPost("/api/gdelt/ingest/gkg/import/{fileName}",
                async (
                    string fileName,
                    GkgIngestionService service,
                    IWebHostEnvironment environment,
                    CancellationToken cancellationToken) =>
                {
                    if (Path.GetFileName(fileName) != fileName ||
                        !fileName.EndsWith(".gkg.csv", StringComparison.OrdinalIgnoreCase))
                    {
                        return Results.BadRequest(
                            "Supply a valid GKG filename, not a path.");
                    }

                    var datasetDirectory = Path.GetFullPath(
                        Path.Combine(
                            environment.ContentRootPath,
                            "..",
                            "research-data",
                            "extracted",
                            "gkg"));

                    var filePath = Path.Combine(datasetDirectory, fileName);

                    if (!File.Exists(filePath))
                    {
                        return Results.NotFound(
                            $"GKG file not found: {fileName}");
                    }

                    var result = await service.ImportFileAsync(
                        filePath, cancellationToken);

                    return Results.Ok(new
                    {
                        result.FileName,
                        result.Status,
                        result.RecordsScanned,
                        result.RecordsImported,
                        result.RecordsSkipped,
                        result.ProcessingDurationSeconds,
                        result.ErrorMessage
                    });
                });

            app.MapGet("/api/gdelt/ingest/summary",
            async (GdeltDbContext db, CancellationToken ct) =>
            {
                var summary = await db.IngestionFiles
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
                        RecordsScanned = group.Sum(file => file.RecordsScanned),
                        RecordsImported = group.Sum(file => file.RecordsImported),
                        RecordsSkipped = group.Sum(file => file.RecordsSkipped)
                    })
                    .ToListAsync(ct);

                return Results.Ok(summary);
            });


            app.MapPost("/api/gdelt/ingest/events/import/{fileName}",
                async (
                    string fileName,
                    EventIngestionService service,
                    IWebHostEnvironment environment,
                    CancellationToken cancellationToken) =>
                {
                    if (Path.GetFileName(fileName) != fileName ||
                        !fileName.EndsWith(".export.CSV",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return Results.BadRequest(
                            "Supply a valid Events filename, not a path.");
                    }

                    var datasetDirectory = Path.GetFullPath(
                        Path.Combine(
                            environment.ContentRootPath,
                            "..",
                            "research-data",
                            "extracted",
                            "events"));

                    var filePath = Path.Combine(datasetDirectory, fileName);

                    if (!File.Exists(filePath))
                    {
                        return Results.NotFound(
                            $"Events file not found: {fileName}");
                    }

                    var result = await service.ImportFileAsync(
                        filePath, cancellationToken);

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


            app.MapGet("/api/gdelt/ingest/events/verify",
                async (GdeltDbContext db, CancellationToken ct) =>
                {
                    var count = await db.EventRecords.CountAsync(ct);

                    var sample = await db.EventRecords
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
                        .FirstOrDefaultAsync(ct);

                    return Results.Ok(new
                    {
                        RecordCount = count,
                        Sample = sample
                    });
                });


            app.MapPost("/api/gdelt/ingest/events/fix-first-manifest",
                async (
                    GdeltDbContext db,
                    IWebHostEnvironment environment,
                    CancellationToken ct) =>
                {
                    if (!environment.IsDevelopment())
                        return Results.NotFound();

                    const string fileName = "20261011194500.export.CSV";

                    var manifest = await db.IngestionFiles
                        .SingleOrDefaultAsync(
                            file => file.FileName == fileName &&
                                    file.DatasetType == "Events",
                            ct);

                    if (manifest == null ||
                        manifest.Status != "Completed" ||
                        manifest.RecordsScanned != 756 ||
                        manifest.RecordsImported != 0)
                    {
                        return Results.Conflict(
                            "Manifest does not match the expected state.");
                    }

                    var eventCount = await db.EventRecords.CountAsync(ct);

                    if (eventCount != 756)
                    {
                        return Results.Conflict(
                            $"Expected 756 Events records, found {eventCount}.");
                    }

                    manifest.RecordsImported = 756;
                    await db.SaveChangesAsync(ct);

                    return Results.Ok(new
                    {
                        manifest.FileName,
                        manifest.Status,
                        manifest.RecordsScanned,
                        manifest.RecordsImported,
                        manifest.RecordsSkipped
                    });
                });



            app.MapPost("/api/gdelt/ingest/mentions/import/{fileName}",
                async (
                    string fileName,
                    MentionIngestionService service,
                    IWebHostEnvironment environment,
                    CancellationToken cancellationToken) =>
                {
                    if (Path.GetFileName(fileName) != fileName ||
                        !fileName.EndsWith(".mentions.CSV",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return Results.BadRequest(
                            "Supply a valid Mentions filename, not a path.");
                    }

                    var datasetDirectory = Path.GetFullPath(
                        Path.Combine(
                            environment.ContentRootPath,
                            "..",
                            "research-data",
                            "extracted",
                            "mentions"));

                    var filePath = Path.Combine(datasetDirectory, fileName);

                    if (!File.Exists(filePath))
                    {
                        return Results.NotFound(
                            $"Mentions file not found: {fileName}");
                    }

                    var result = await service.ImportFileAsync(
                        filePath, cancellationToken);

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

            app.MapGet("/api/gdelt/ingest/mentions/verify",
            async (GdeltDbContext db, CancellationToken ct) =>
            {
                var count = await db.MentionRecords.CountAsync(ct);

                var sample = await db.MentionRecords
                    .OrderBy(record => record.Id)
                    .Select(record => new
                    {
                        record.Id,
                        record.GlobalEventId,
                        record.MentionTimeDate,
                        record.MentionSourceName,
                        record.MentionIdentifier
                    })
                    .FirstOrDefaultAsync(ct);

                return Results.Ok(new
                {
                    RecordCount = count,
                    Sample = sample
                });
            });


            app.MapGet("/api/gdelt/ingest/measurements",
                async (GdeltDbContext db, CancellationToken ct) =>
                {
                    var files = await db.IngestionFiles
                        .AsNoTracking()
                        .ToListAsync(ct);

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
    }
}
