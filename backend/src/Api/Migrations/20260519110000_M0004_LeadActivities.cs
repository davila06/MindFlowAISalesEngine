using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class M0004_LeadActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeadActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default"),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActivityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Actor = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, defaultValue: "system"),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadActivities", x => x.Id);
                });

            migrationBuilder.CreateIndex(name: "IX_LeadActivities_TenantId", table: "LeadActivities", column: "TenantId");
            migrationBuilder.CreateIndex(name: "IX_LeadActivities_LeadId", table: "LeadActivities", column: "LeadId");
            migrationBuilder.CreateIndex(name: "IX_LeadActivities_ActivityType", table: "LeadActivities", column: "ActivityType");
            migrationBuilder.CreateIndex(name: "IX_LeadActivities_OccurredAtUtc", table: "LeadActivities", column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LeadActivities");
        }
    }
}
