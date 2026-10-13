
using GDELT_Lab.Models;
using Microsoft.EntityFrameworkCore;

namespace GDELT_Lab.Data;

public class GdeltDbContext : DbContext
{
    // Initialise the context with options configured by dependency injection.
    public GdeltDbContext(DbContextOptions<GdeltDbContext> options)
        : base(options)
    {

    }

    // Define DbSet properties for each entity.
    public DbSet<IngestionFile> IngestionFiles => Set<IngestionFile>();
    public DbSet<GkgRecord> GkgRecords => Set<GkgRecord>();
    public DbSet<EventRecord> EventRecords => Set<EventRecord>();
    public DbSet<MentionRecord> MentionRecords => Set<MentionRecord>();

    // Configure entity keys, property constraints, and indexes.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply the base entity configuration.
        base.OnModelCreating(modelBuilder);

        // Configure the IngestionFile entity.
        modelBuilder.Entity<IngestionFile>(entity =>
        {
            entity.HasKey(file => file.Id);

            entity.Property(file => file.FileName)
                .IsRequired()
                .HasMaxLength(260);

            entity.Property(file => file.DatasetType)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(file => file.SourcePath)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(file => file.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(file => file.Checksum)
                .HasMaxLength(128);

            entity.Property(file => file.ErrorMessage)
                .HasMaxLength(4000);

            // Prevent duplicate manifest entries for the same dataset file.
            entity.HasIndex(file => new
            {
                file.FileName,
                file.DatasetType
            }).IsUnique();

            // Support filtering manifest entries by status or dataset type.
            entity.HasIndex(file => file.Status);
            entity.HasIndex(file => file.DatasetType);
        });

        // Configure the GkgRecord entity.
        modelBuilder.Entity<GkgRecord>(entity =>
        {
            entity.HasKey(record => record.GkgRecordId);

            entity.Property(record => record.GkgRecordId)
                .HasMaxLength(100);

            entity.Property(record => record.SourceCommonName)
                .HasMaxLength(500);

            entity.Property(record => record.DocumentIdentifier)
                .HasMaxLength(2048);

            // Support filtering by timestamp and source name.
            entity.HasIndex(record => record.RecordTimestampUtc);
            entity.HasIndex(record => record.SourceCommonName);
        });

        // Configure the EventRecord entity.
        modelBuilder.Entity<EventRecord>(entity =>
        {
            entity.HasKey(record => record.GlobalEventId);

            entity.Property(record => record.GlobalEventId)
                .HasMaxLength(100);

            entity.Property(record => record.Day)
                .HasMaxLength(8);

            entity.Property(record => record.EventCode)
                .HasMaxLength(10);

            entity.Property(record => record.SourceUrl)
                .HasMaxLength(4000);

            // Support filtering by day, event code, and date added.
            entity.HasIndex(record => record.Day);
            entity.HasIndex(record => record.EventCode);
            entity.HasIndex(record => record.DateAdded);
        });

        // Configure the MentionRecord entity.
        modelBuilder.Entity<MentionRecord>(entity =>
        {
            entity.HasKey(record => record.Id);

            entity.Property(record => record.GlobalEventId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(record => record.MentionIdentifier)
                .HasMaxLength(4000);

            // Support filtering by event, mention time, and source name.
            entity.HasIndex(record => record.GlobalEventId);
            entity.HasIndex(record => record.MentionTimeDate);
            entity.HasIndex(record => record.MentionSourceName);
        });
    }
}
