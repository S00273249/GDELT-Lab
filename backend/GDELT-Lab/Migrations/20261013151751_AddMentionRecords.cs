using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GDELT_Lab.Migrations
{
    /// <inheritdoc />
    public partial class AddMentionRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MentionRecords",
                columns: table => new
                {
                    GlobalEventId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MentionTimeDate = table.Column<string>(type: "TEXT", nullable: false),
                    MentionIdentifier = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    SentenceId = table.Column<string>(type: "TEXT", nullable: false),
                    EventTimeDate = table.Column<string>(type: "TEXT", nullable: true),
                    MentionType = table.Column<string>(type: "TEXT", nullable: true),
                    MentionSourceName = table.Column<string>(type: "TEXT", nullable: true),
                    Actor1CharOffset = table.Column<string>(type: "TEXT", nullable: true),
                    Actor2CharOffset = table.Column<string>(type: "TEXT", nullable: true),
                    ActionCharOffset = table.Column<string>(type: "TEXT", nullable: true),
                    InRawText = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    MentionDocLen = table.Column<string>(type: "TEXT", nullable: true),
                    MentionDocTone = table.Column<string>(type: "TEXT", nullable: true),
                    ReservedField14 = table.Column<string>(type: "TEXT", nullable: true),
                    ReservedField15 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentionRecords", x => new { x.GlobalEventId, x.MentionTimeDate, x.MentionIdentifier, x.SentenceId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_MentionRecords_MentionSourceName",
                table: "MentionRecords",
                column: "MentionSourceName");

            migrationBuilder.CreateIndex(
                name: "IX_MentionRecords_MentionTimeDate",
                table: "MentionRecords",
                column: "MentionTimeDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MentionRecords");
        }
    }
}
