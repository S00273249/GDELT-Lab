
using Microsoft.EntityFrameworkCore;
using NewsFeed.Api.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace NewsFeed.Api.Data;

public class NewsFeedDbContext : DbContext
{
    public NewsFeedDbContext(
        DbContextOptions<NewsFeedDbContext> options)
        : base(options)
    {
    }

    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.HasKey(article => article.Id);

            entity.Property(article => article.GkgRecordId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(article => article.SourceCommonName)
                .HasMaxLength(500);

            entity.Property(article => article.DocumentIdentifier)
                .HasMaxLength(2048);

            entity.Property(article => article.MetadataExtractionStatus)
                .IsRequired()
                .HasMaxLength(50);

            // Each original GKG record should appear only once
            // in the prepared prototype dataset.
            entity.HasIndex(article => article.GkgRecordId)
                .IsUnique();

            // Support common feed filtering and sorting.
            entity.HasIndex(article => article.RecordTimestampUtc);
            entity.HasIndex(article => article.SourceCommonName);
        });
    }
}
