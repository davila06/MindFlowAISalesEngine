using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class M0003_EmailTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TrackingToken",
                table: "EmailLogs",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "(lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))))");

            migrationBuilder.AddColumn<int>(
                name: "OpenCount",
                table: "EmailLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClickCount",
                table: "EmailLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstOpenedAtUtc",
                table: "EmailLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOpenedAtUtc",
                table: "EmailLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstClickedAtUtc",
                table: "EmailLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAppleMpp",
                table: "EmailLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_TrackingToken",
                table: "EmailLogs",
                column: "TrackingToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_EmailLogs_TrackingToken", table: "EmailLogs");
            migrationBuilder.DropColumn(name: "TrackingToken", table: "EmailLogs");
            migrationBuilder.DropColumn(name: "OpenCount", table: "EmailLogs");
            migrationBuilder.DropColumn(name: "ClickCount", table: "EmailLogs");
            migrationBuilder.DropColumn(name: "FirstOpenedAtUtc", table: "EmailLogs");
            migrationBuilder.DropColumn(name: "LastOpenedAtUtc", table: "EmailLogs");
            migrationBuilder.DropColumn(name: "FirstClickedAtUtc", table: "EmailLogs");
            migrationBuilder.DropColumn(name: "IsAppleMpp", table: "EmailLogs");
        }
    }
}
