using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GDELT_Lab.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventRecords",
                columns: table => new
                {
                    GlobalEventId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Day = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    MonthYear = table.Column<string>(type: "TEXT", nullable: true),
                    Year = table.Column<string>(type: "TEXT", nullable: true),
                    FractionDate = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1Name = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1CountryCode = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1KnownGroupCode = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1EthnicCode = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1Religion1Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1Religion2Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1Type1Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1Type2Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1Type3Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2Name = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2CountryCode = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2KnownGroupCode = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2EthnicCode = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2Religion1Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2Religion2Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2Type1Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2Type2Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2Type3Code = table.Column<string>(type: "TEXT", nullable: true),
                    IsRootEvent = table.Column<string>(type: "TEXT", nullable: true),
                    EventCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    EventBaseCode = table.Column<string>(type: "TEXT", nullable: true),
                    EventRootCode = table.Column<string>(type: "TEXT", nullable: true),
                    QuadClass = table.Column<string>(type: "TEXT", nullable: true),
                    GoldsteinScale = table.Column<string>(type: "TEXT", nullable: true),
                    NumMentions = table.Column<string>(type: "TEXT", nullable: true),
                    NumSources = table.Column<string>(type: "TEXT", nullable: true),
                    NumArticles = table.Column<string>(type: "TEXT", nullable: true),
                    AvgTone = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1GeoType = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1GeoFullName = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1GeoCountryCode = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1GeoAdm1Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1GeoAdm2Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1GeoLat = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1GeoLong = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1GeoFeatureId = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2GeoType = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2GeoFullName = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2GeoCountryCode = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2GeoAdm1Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2GeoAdm2Code = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2GeoLat = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2GeoLong = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2GeoFeatureId = table.Column<string>(type: "TEXT", nullable: true),
                    ActionGeoType = table.Column<string>(type: "TEXT", nullable: true),
                    ActionGeoFullName = table.Column<string>(type: "TEXT", nullable: true),
                    ActionGeoCountryCode = table.Column<string>(type: "TEXT", nullable: true),
                    ActionGeoAdm1Code = table.Column<string>(type: "TEXT", nullable: true),
                    ActionGeoAdm2Code = table.Column<string>(type: "TEXT", nullable: true),
                    ActionGeoLat = table.Column<string>(type: "TEXT", nullable: true),
                    ActionGeoLong = table.Column<string>(type: "TEXT", nullable: true),
                    ActionGeoFeatureId = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<string>(type: "TEXT", nullable: true),
                    SourceUrl = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRecords", x => x.GlobalEventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventRecords_DateAdded",
                table: "EventRecords",
                column: "DateAdded");

            migrationBuilder.CreateIndex(
                name: "IX_EventRecords_Day",
                table: "EventRecords",
                column: "Day");

            migrationBuilder.CreateIndex(
                name: "IX_EventRecords_EventCode",
                table: "EventRecords",
                column: "EventCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventRecords");
        }
    }
}
