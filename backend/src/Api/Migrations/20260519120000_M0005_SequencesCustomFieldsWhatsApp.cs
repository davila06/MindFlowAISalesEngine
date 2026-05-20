using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class M0005_SequencesCustomFieldsWhatsApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ===== Sequences =====
            migrationBuilder.CreateTable(
                name: "Sequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Sequences", x => x.Id));

            migrationBuilder.CreateTable(
                name: "SequenceSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SequenceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ActionValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DelayDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenceSteps", x => x.Id);
                    table.ForeignKey("FK_SequenceSteps_Sequences_SequenceId", x => x.SequenceId, "Sequences", "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SequenceEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default"),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SequenceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "active"),
                    NextStepOrder = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    NextStepDueAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EnrolledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExitedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExitReason = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_SequenceEnrollments", x => x.Id));

            migrationBuilder.CreateIndex("IX_Sequences_TenantId", "Sequences", "TenantId");
            migrationBuilder.CreateIndex("IX_SequenceSteps_SequenceId", "SequenceSteps", "SequenceId");
            migrationBuilder.CreateIndex("IX_SequenceEnrollments_TenantId", "SequenceEnrollments", "TenantId");
            migrationBuilder.CreateIndex("IX_SequenceEnrollments_LeadId", "SequenceEnrollments", "LeadId");
            migrationBuilder.CreateIndex("IX_SequenceEnrollments_SequenceId", "SequenceEnrollments", "SequenceId");
            migrationBuilder.CreateIndex("IX_SequenceEnrollments_Status", "SequenceEnrollments", "Status");
            migrationBuilder.CreateIndex("IX_SequenceEnrollments_NextStepDueAtUtc", "SequenceEnrollments", "NextStepDueAtUtc");

            // ===== Custom Fields =====
            migrationBuilder.CreateTable(
                name: "CustomFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default"),
                    Key = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    FieldType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Lead"),
                    Options = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_CustomFieldDefinitions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default"),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FieldKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_CustomFieldValues", x => x.Id));

            migrationBuilder.CreateIndex("IX_CustomFieldDefinitions_TenantId", "CustomFieldDefinitions", "TenantId");
            migrationBuilder.CreateIndex("IX_CustomFieldDefinitions_TenantId_Key", "CustomFieldDefinitions",
                new[] { "TenantId", "Key" }, unique: true);
            migrationBuilder.CreateIndex("IX_CustomFieldValues_TenantId", "CustomFieldValues", "TenantId");
            migrationBuilder.CreateIndex("IX_CustomFieldValues_EntityId", "CustomFieldValues", "EntityId");
            migrationBuilder.CreateIndex("IX_CustomFieldValues_TenantId_EntityId_FieldKey", "CustomFieldValues",
                new[] { "TenantId", "EntityId", "FieldKey" }, unique: true);

            // ===== WhatsApp =====
            migrationBuilder.CreateTable(
                name: "WhatsAppContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default"),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    OptedIn = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    OptedInAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OptedOutAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_WhatsAppContacts", x => x.Id));

            migrationBuilder.CreateTable(
                name: "WhatsAppMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default"),
                    ExternalMessageId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ContactPhone = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                    TemplateName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_WhatsAppMessages", x => x.Id));

            migrationBuilder.CreateIndex("IX_WhatsAppContacts_TenantId", "WhatsAppContacts", "TenantId");
            migrationBuilder.CreateIndex("IX_WhatsAppContacts_PhoneNumber", "WhatsAppContacts", "PhoneNumber");
            migrationBuilder.CreateIndex("IX_WhatsAppContacts_TenantId_PhoneNumber", "WhatsAppContacts",
                new[] { "TenantId", "PhoneNumber" }, unique: true);
            migrationBuilder.CreateIndex("IX_WhatsAppMessages_TenantId", "WhatsAppMessages", "TenantId");
            migrationBuilder.CreateIndex("IX_WhatsAppMessages_ContactPhone", "WhatsAppMessages", "ContactPhone");
            migrationBuilder.CreateIndex("IX_WhatsAppMessages_ExternalMessageId", "WhatsAppMessages", "ExternalMessageId");
            migrationBuilder.CreateIndex("IX_WhatsAppMessages_SentAtUtc", "WhatsAppMessages", "SentAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("SequenceEnrollments");
            migrationBuilder.DropTable("SequenceSteps");
            migrationBuilder.DropTable("Sequences");
            migrationBuilder.DropTable("CustomFieldValues");
            migrationBuilder.DropTable("CustomFieldDefinitions");
            migrationBuilder.DropTable("WhatsAppMessages");
            migrationBuilder.DropTable("WhatsAppContacts");
        }
    }
}
