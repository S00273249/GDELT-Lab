using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GDELT_Lab.Migrations
{
    /// <inheritdoc />
    public partial class UseMentionSurrogateKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MentionRecords",
                table: "MentionRecords");

            migrationBuilder.AlterColumn<string>(
                name: "SentenceId",
                table: "MentionRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "MentionIdentifier",
                table: "MentionRecords",
                type: "TEXT",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "MentionTimeDate",
                table: "MentionRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "MentionRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MentionRecords",
                table: "MentionRecords",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_MentionRecords_GlobalEventId",
                table: "MentionRecords",
                column: "GlobalEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MentionRecords",
                table: "MentionRecords");

            migrationBuilder.DropIndex(
                name: "IX_MentionRecords_GlobalEventId",
                table: "MentionRecords");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "MentionRecords");

            migrationBuilder.AlterColumn<string>(
                name: "SentenceId",
                table: "MentionRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MentionTimeDate",
                table: "MentionRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MentionIdentifier",
                table: "MentionRecords",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MentionRecords",
                table: "MentionRecords",
                columns: new[] { "GlobalEventId", "MentionTimeDate", "MentionIdentifier", "SentenceId" });
        }
    }
}
