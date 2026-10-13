using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GDELT_Lab.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IngestionFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    DatasetType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SourcePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Checksum = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    RecordsScanned = table.Column<long>(type: "INTEGER", nullable: false),
                    RecordsImported = table.Column<long>(type: "INTEGER", nullable: false),
                    RecordsSkipped = table.Column<long>(type: "INTEGER", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProcessingDurationSeconds = table.Column<double>(type: "REAL", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionFiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionFiles_DatasetType",
                table: "IngestionFiles",
                column: "DatasetType");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionFiles_FileName_DatasetType",
                table: "IngestionFiles",
                columns: new[] { "FileName", "DatasetType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngestionFiles_Status",
                table: "IngestionFiles",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngestionFiles");
        }
    }
}
