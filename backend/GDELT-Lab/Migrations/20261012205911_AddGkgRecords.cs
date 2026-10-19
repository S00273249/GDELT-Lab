using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GDELT_Lab.Migrations
{
    /// <inheritdoc />
    public partial class AddGkgRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GkgRecords",
                columns: table => new
                {
                    GkgRecordId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RecordTimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceCollectionIdentifier = table.Column<string>(type: "TEXT", nullable: true),
                    SourceCommonName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DocumentIdentifier = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Counts = table.Column<string>(type: "TEXT", nullable: true),
                    V2Counts = table.Column<string>(type: "TEXT", nullable: true),
                    Themes = table.Column<string>(type: "TEXT", nullable: true),
                    V2Themes = table.Column<string>(type: "TEXT", nullable: true),
                    Locations = table.Column<string>(type: "TEXT", nullable: true),
                    V2Locations = table.Column<string>(type: "TEXT", nullable: true),
                    Persons = table.Column<string>(type: "TEXT", nullable: true),
                    V2Persons = table.Column<string>(type: "TEXT", nullable: true),
                    Organizations = table.Column<string>(type: "TEXT", nullable: true),
                    V2Organizations = table.Column<string>(type: "TEXT", nullable: true),
                    V2Tone = table.Column<string>(type: "TEXT", nullable: true),
                    Dates = table.Column<string>(type: "TEXT", nullable: true),
                    Gcam = table.Column<string>(type: "TEXT", nullable: true),
                    SharingImage = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedImages = table.Column<string>(type: "TEXT", nullable: true),
                    SocialImageEmbeds = table.Column<string>(type: "TEXT", nullable: true),
                    SocialVideoEmbeds = table.Column<string>(type: "TEXT", nullable: true),
                    Quotations = table.Column<string>(type: "TEXT", nullable: true),
                    AllNames = table.Column<string>(type: "TEXT", nullable: true),
                    Amounts = table.Column<string>(type: "TEXT", nullable: true),
                    TranslationInfo = table.Column<string>(type: "TEXT", nullable: true),
                    Extras = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GkgRecords", x => x.GkgRecordId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GkgRecords_RecordTimestampUtc",
                table: "GkgRecords",
                column: "RecordTimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_GkgRecords_SourceCommonName",
                table: "GkgRecords",
                column: "SourceCommonName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GkgRecords");
        }
    }
}
