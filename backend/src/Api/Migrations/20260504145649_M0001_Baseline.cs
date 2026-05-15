using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class M0001_Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Target = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UserRole = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertThresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MaxErrorRatePercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxAverageLatencyMs = table.Column<decimal>(type: "TEXT", nullable: false),
                    NotificationEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    WebhookUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertThresholds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreferredCountry = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    PreferredIndustry = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    MaxActiveLeads = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100),
                    MinScoreToAssign = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataRetentionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmailLogsRemoved = table.Column<int>(type: "INTEGER", nullable: false),
                    AlertEventsRemoved = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminAuditLogsRemoved = table.Column<int>(type: "INTEGER", nullable: false),
                    ExecutedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataRetentionRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailDispatchJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ProviderType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ToEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    BodyHtml = table.Column<string>(type: "TEXT", nullable: false),
                    TemplateName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AttachmentBytes = table.Column<byte[]>(type: "BLOB", nullable: true),
                    AttachmentFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true),
                    AttachmentContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDispatchJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ToEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TemplateName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Succeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailStopListEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailStopListEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    BodyHtml = table.Column<string>(type: "TEXT", nullable: false),
                    RequiredVariablesSerialized = table.Column<string>(type: "TEXT", nullable: false, defaultValue: ""),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FollowUpJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExecutedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AttemptNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FollowUpPolicySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuietHoursEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    QuietHoursStartHourUtc = table.Column<int>(type: "INTEGER", nullable: false),
                    QuietHoursEndHourUtc = table.Column<int>(type: "INTEGER", nullable: false),
                    RulesJson = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpPolicySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "inbound"),
                    Campaign = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false, defaultValue: "organic"),
                    Country = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false, defaultValue: "xx"),
                    Score = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false, defaultValue: "Low"),
                    ScoringVersion = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false, defaultValue: "unscored"),
                    ScoredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObservabilityAggregateBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    WindowStartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WindowEndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IncrementalRequestCount = table.Column<long>(type: "INTEGER", nullable: false),
                    IncrementalSuccessCount = table.Column<long>(type: "INTEGER", nullable: false),
                    IncrementalErrorCount = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalLatencyMs = table.Column<decimal>(type: "TEXT", nullable: false),
                    SampleCount = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSourceRecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObservabilityAggregateBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObservabilityAggregationCheckpoints",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastProcessedRecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastProcessedRecordId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObservabilityAggregationCheckpoints", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "ObservabilityEndpointAggregationStates",
                columns: table => new
                {
                    EndpointName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastRequestCount = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSuccessCount = table.Column<long>(type: "INTEGER", nullable: false),
                    LastErrorCount = table.Column<long>(type: "INTEGER", nullable: false),
                    LastRecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObservabilityEndpointAggregationStates", x => x.EndpointName);
                });

            migrationBuilder.CreateTable(
                name: "ObservabilityMetricRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EndpointName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RequestCount = table.Column<long>(type: "INTEGER", nullable: false),
                    SuccessCount = table.Column<long>(type: "INTEGER", nullable: false),
                    ErrorCount = table.Column<long>(type: "INTEGER", nullable: false),
                    AverageLatencyMs = table.Column<decimal>(type: "TEXT", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObservabilityMetricRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PipelineStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoisonQueueRemediationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    JobType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    RecommendedAction = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    RemediationPath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ExecutedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ExecutedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolutionLatencyMinutes = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoisonQueueRemediationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProposalTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    HtmlBody = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Trigger = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100),
                    ConflictPolicy = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "first_wins"),
                    ExecutionStartHourUtc = table.Column<int>(type: "INTEGER", nullable: true),
                    ExecutionEndHourUtc = table.Column<int>(type: "INTEGER", nullable: true),
                    CooldownMinutes = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    AllowDestructiveActions = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false, defaultValue: "dev"),
                    ApprovalStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "approved"),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmtpSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "smtp"),
                    ProviderBaseUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ApiKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Host = table.Column<string>(type: "TEXT", maxLength: 253, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FromEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    FromName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EnableSsl = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmtpSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ThresholdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MetricName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ObservedValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    ThresholdValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    TriggeredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NotificationSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "open"),
                    AcknowledgedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SnoozedUntilUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StatusNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertEvents_AlertThresholds_ThresholdId",
                        column: x => x.ThresholdId,
                        principalTable: "AlertThresholds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Industry = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false, defaultValue: "unknown"),
                    Website = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacts_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Segment = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "standard"),
                    PlaybookKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false, defaultValue: "standard-onboarding"),
                    HealthScore = table.Column<decimal>(type: "TEXT", nullable: false),
                    TrackingToken = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TrackingActivations = table.Column<int>(type: "INTEGER", nullable: false),
                    LastTrackingActivatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeadAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Strategy = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RuleKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AssignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadAssignments_AssignmentUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AssignmentUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeadAssignments_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeadAuditSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Actor = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadAuditSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadAuditSnapshots_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    RecipientName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    RecipientEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    TemplateName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TemplateVersion = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    PdfFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PdfContent = table.Column<byte[]>(type: "BLOB", nullable: false),
                    TrackingToken = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ViewCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastViewedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SignedByName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    SignedByEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    RenewedFromProposalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proposals_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Opportunities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opportunities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Opportunities_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Opportunities_PipelineStages_StageId",
                        column: x => x.StageId,
                        principalTable: "PipelineStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RuleActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleActions_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Field = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Operator = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleConditions_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Trigger = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Matched = table.Column<bool>(type: "INTEGER", nullable: false),
                    Applied = table.Column<bool>(type: "INTEGER", nullable: false),
                    ActionsAppliedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedReason = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    DurationMs = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExecutedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleExecutionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleExecutionLogs_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    SnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleRevisions_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DependencyKeysSerialized = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingWelcomeJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AttemptNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExecutedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingWelcomeJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingWelcomeJobs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProposalReminderJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProposalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AttemptNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExecutedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalReminderJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposalReminderJobs_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpportunityStageHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromStageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToStageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Actor = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false, defaultValue: "system"),
                    IsAutomated = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "default")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityStageHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityStageHistory_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpportunityStageHistory_PipelineStages_FromStageId",
                        column: x => x.FromStageId,
                        principalTable: "PipelineStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpportunityStageHistory_PipelineStages_ToStageId",
                        column: x => x.ToStageId,
                        principalTable: "PipelineStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_Action",
                table: "AdminAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_CreatedAtUtc",
                table: "AdminAuditLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_TenantId",
                table: "AdminAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_EndpointName",
                table: "AlertEvents",
                column: "EndpointName");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_MetricName",
                table: "AlertEvents",
                column: "MetricName");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_TenantId",
                table: "AlertEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_ThresholdId",
                table: "AlertEvents",
                column: "ThresholdId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_TriggeredAtUtc",
                table: "AlertEvents",
                column: "TriggeredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AlertThresholds_EndpointName",
                table: "AlertThresholds",
                column: "EndpointName");

            migrationBuilder.CreateIndex(
                name: "IX_AlertThresholds_IsActive",
                table: "AlertThresholds",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AlertThresholds_TenantId",
                table: "AlertThresholds",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentUsers_Email",
                table: "AssignmentUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentUsers_IsActive",
                table: "AssignmentUsers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentUsers_TenantId",
                table: "AssignmentUsers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Industry",
                table: "Companies",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_LeadId",
                table: "Companies",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TenantId",
                table: "Companies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Email",
                table: "Contacts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_LeadId",
                table: "Contacts",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Phone",
                table: "Contacts",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_TenantId",
                table: "Contacts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_LeadId",
                table: "Customers",
                column: "LeadId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId",
                table: "Customers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TrackingToken",
                table: "Customers",
                column: "TrackingToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataRetentionRuns_ExecutedAtUtc",
                table: "DataRetentionRuns",
                column: "ExecutedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchJobs_DueAtUtc",
                table: "EmailDispatchJobs",
                column: "DueAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchJobs_Status",
                table: "EmailDispatchJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchJobs_TenantId",
                table: "EmailDispatchJobs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_LeadId",
                table: "EmailLogs",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_TenantId",
                table: "EmailLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailStopListEntries_Email",
                table: "EmailStopListEntries",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailStopListEntries_TenantId",
                table: "EmailStopListEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_TenantId",
                table: "EmailTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_TenantId_Name_Version",
                table: "EmailTemplates",
                columns: new[] { "TenantId", "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpJobs_LeadId",
                table: "FollowUpJobs",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpJobs_Status",
                table: "FollowUpJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpJobs_TenantId",
                table: "FollowUpJobs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpPolicySettings_TenantId",
                table: "FollowUpPolicySettings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignments_AssignedAtUtc",
                table: "LeadAssignments",
                column: "AssignedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignments_LeadId",
                table: "LeadAssignments",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignments_TenantId",
                table: "LeadAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignments_UserId",
                table: "LeadAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAuditSnapshots_CreatedAtUtc",
                table: "LeadAuditSnapshots",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAuditSnapshots_LeadId",
                table: "LeadAuditSnapshots",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Channel",
                table: "Leads",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Email",
                table: "Leads",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Phone",
                table: "Leads",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Priority",
                table: "Leads",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Score",
                table: "Leads",
                column: "Score");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Source",
                table: "Leads",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_TenantId",
                table: "Leads",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ObservabilityAggregateBatches_EndpointName",
                table: "ObservabilityAggregateBatches",
                column: "EndpointName");

            migrationBuilder.CreateIndex(
                name: "IX_ObservabilityAggregateBatches_EndpointName_WindowStartUtc_WindowEndUtc",
                table: "ObservabilityAggregateBatches",
                columns: new[] { "EndpointName", "WindowStartUtc", "WindowEndUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObservabilityAggregateBatches_WindowStartUtc",
                table: "ObservabilityAggregateBatches",
                column: "WindowStartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ObservabilityMetricRecords_EndpointName",
                table: "ObservabilityMetricRecords",
                column: "EndpointName");

            migrationBuilder.CreateIndex(
                name: "IX_ObservabilityMetricRecords_RecordedAtUtc",
                table: "ObservabilityMetricRecords",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_CustomerId",
                table: "OnboardingTasks",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_CustomerId_Key",
                table: "OnboardingTasks",
                columns: new[] { "CustomerId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_TenantId",
                table: "OnboardingTasks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingWelcomeJobs_CustomerId",
                table: "OnboardingWelcomeJobs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingWelcomeJobs_DueAtUtc",
                table: "OnboardingWelcomeJobs",
                column: "DueAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingWelcomeJobs_Status",
                table: "OnboardingWelcomeJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingWelcomeJobs_TenantId",
                table: "OnboardingWelcomeJobs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_LeadId",
                table: "Opportunities",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_StageId",
                table: "Opportunities",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_TenantId",
                table: "Opportunities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityStageHistory_FromStageId",
                table: "OpportunityStageHistory",
                column: "FromStageId");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityStageHistory_OpportunityId",
                table: "OpportunityStageHistory",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityStageHistory_TenantId",
                table: "OpportunityStageHistory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityStageHistory_ToStageId",
                table: "OpportunityStageHistory",
                column: "ToStageId");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineStages_TenantId",
                table: "PipelineStages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineStages_TenantId_Name",
                table: "PipelineStages",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PipelineStages_TenantId_Order",
                table: "PipelineStages",
                columns: new[] { "TenantId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoisonQueueRemediationRuns_ExecutedAtUtc",
                table: "PoisonQueueRemediationRuns",
                column: "ExecutedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PoisonQueueRemediationRuns_JobType",
                table: "PoisonQueueRemediationRuns",
                column: "JobType");

            migrationBuilder.CreateIndex(
                name: "IX_PoisonQueueRemediationRuns_Outcome",
                table: "PoisonQueueRemediationRuns",
                column: "Outcome");

            migrationBuilder.CreateIndex(
                name: "IX_PoisonQueueRemediationRuns_TenantId",
                table: "PoisonQueueRemediationRuns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalReminderJobs_ProposalId",
                table: "ProposalReminderJobs",
                column: "ProposalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProposalReminderJobs_Status",
                table: "ProposalReminderJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalReminderJobs_TenantId",
                table: "ProposalReminderJobs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_LeadId",
                table: "Proposals",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_TenantId",
                table: "Proposals",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_TrackingToken",
                table: "Proposals",
                column: "TrackingToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProposalTemplates_Name_IsCurrent",
                table: "ProposalTemplates",
                columns: new[] { "Name", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_ProposalTemplates_Name_Version",
                table: "ProposalTemplates",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProposalTemplates_TenantId",
                table: "ProposalTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleActions_RuleId",
                table: "RuleActions",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleActions_TenantId",
                table: "RuleActions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleConditions_RuleId",
                table: "RuleConditions",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleConditions_TenantId",
                table: "RuleConditions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleExecutionLogs_EntityId",
                table: "RuleExecutionLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleExecutionLogs_ExecutedAtUtc",
                table: "RuleExecutionLogs",
                column: "ExecutedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RuleExecutionLogs_RuleId",
                table: "RuleExecutionLogs",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleExecutionLogs_TenantId",
                table: "RuleExecutionLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleRevisions_CreatedAtUtc",
                table: "RuleRevisions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RuleRevisions_RuleId",
                table: "RuleRevisions",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleRevisions_TenantId",
                table: "RuleRevisions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleRevisions_Version",
                table: "RuleRevisions",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_ApprovalStatus",
                table: "Rules",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_Environment",
                table: "Rules",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_IsActive",
                table: "Rules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_Priority",
                table: "Rules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_TenantId",
                table: "Rules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_Trigger",
                table: "Rules",
                column: "Trigger");

            migrationBuilder.CreateIndex(
                name: "IX_SmtpSettings_TenantId",
                table: "SmtpSettings",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "AlertEvents");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "DataRetentionRuns");

            migrationBuilder.DropTable(
                name: "EmailDispatchJobs");

            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropTable(
                name: "EmailStopListEntries");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "FollowUpJobs");

            migrationBuilder.DropTable(
                name: "FollowUpPolicySettings");

            migrationBuilder.DropTable(
                name: "LeadAssignments");

            migrationBuilder.DropTable(
                name: "LeadAuditSnapshots");

            migrationBuilder.DropTable(
                name: "ObservabilityAggregateBatches");

            migrationBuilder.DropTable(
                name: "ObservabilityAggregationCheckpoints");

            migrationBuilder.DropTable(
                name: "ObservabilityEndpointAggregationStates");

            migrationBuilder.DropTable(
                name: "ObservabilityMetricRecords");

            migrationBuilder.DropTable(
                name: "OnboardingTasks");

            migrationBuilder.DropTable(
                name: "OnboardingWelcomeJobs");

            migrationBuilder.DropTable(
                name: "OpportunityStageHistory");

            migrationBuilder.DropTable(
                name: "PoisonQueueRemediationRuns");

            migrationBuilder.DropTable(
                name: "ProposalReminderJobs");

            migrationBuilder.DropTable(
                name: "ProposalTemplates");

            migrationBuilder.DropTable(
                name: "RuleActions");

            migrationBuilder.DropTable(
                name: "RuleConditions");

            migrationBuilder.DropTable(
                name: "RuleExecutionLogs");

            migrationBuilder.DropTable(
                name: "RuleRevisions");

            migrationBuilder.DropTable(
                name: "SmtpSettings");

            migrationBuilder.DropTable(
                name: "AlertThresholds");

            migrationBuilder.DropTable(
                name: "AssignmentUsers");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Opportunities");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropTable(
                name: "Rules");

            migrationBuilder.DropTable(
                name: "PipelineStages");

            migrationBuilder.DropTable(
                name: "Leads");
        }
    }
}
