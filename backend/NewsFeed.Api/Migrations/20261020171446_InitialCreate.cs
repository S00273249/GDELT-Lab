using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsFeed.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsArticles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GkgRecordId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RecordTimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceCollectionIdentifier = table.Column<string>(type: "TEXT", nullable: true),
                    SourceCommonName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DocumentIdentifier = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Headline = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    GdeltSharingImage = table.Column<string>(type: "TEXT", nullable: true),
                    HeadlineSource = table.Column<string>(type: "TEXT", nullable: true),
                    DescriptionSource = table.Column<string>(type: "TEXT", nullable: true),
                    ImageSource = table.Column<string>(type: "TEXT", nullable: true),
                    MetadataExtractionStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MetadataLastAttemptUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MetadataError = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_NewsArticles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_GkgRecordId",
                table: "NewsArticles",
                column: "GkgRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_RecordTimestampUtc",
                table: "NewsArticles",
                column: "RecordTimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_SourceCommonName",
                table: "NewsArticles",
                column: "SourceCommonName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsArticles");
        }
    }
}
