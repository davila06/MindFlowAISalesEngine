using Api.Application.Common.FeatureFlags;
using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Controllers;

/// <summary>
/// OPS-05 | Feature flags snapshot.
/// OPS-09 | Cost and capacity monitoring (aggregate counts).
/// OPS-10 | SRE summary – health, alert summary, background jobs.
/// OPS-12 | Background job observability.
/// OPS-13 | Scheduled job failure alerts.
/// OPS-14 | Environment configuration audit.
/// </summary>
[ApiController]
[Route("api/ops")]
public class OpsController : ControllerBase
{
    private readonly LeadsDbContext _db;
    private readonly IFeatureFlagService _featureFlags;
    private readonly ITenantContext _tenant;
    private readonly HealthCheckService _health;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public OpsController(
        LeadsDbContext db,
        IFeatureFlagService featureFlags,
        ITenantContext tenant,
        HealthCheckService health,
        IConfiguration configuration,
        IWebHostEnvironment env)
    {
        _db = db;
        _featureFlags = featureFlags;
        _tenant = tenant;
        _health = health;
        _configuration = configuration;
        _env = env;
    }

    // ─────────────────────────────────────────────
    // OPS-10 | SRE summary
    // GET /api/ops/sre-summary
    // ─────────────────────────────────────────────
    [HttpGet("sre-summary")]
    public async Task<IActionResult> GetSreSummary(CancellationToken cancellationToken)
    {
        var healthReport = await _health.CheckHealthAsync(cancellationToken);

        var openAlerts = await _db.AlertEvents
            .Where(a => a.Status == "open")
            .CountAsync(cancellationToken);

        var last24h = DateTime.UtcNow.AddHours(-24);
        var resolvedAlerts = await _db.AlertEvents
            .Where(a => a.Status == "resolved" && a.TriggeredAtUtc >= last24h)
            .CountAsync(cancellationToken);

        var poisonQueueDepth = await _db.AlertEvents
            .Where(a => a.MetricName == "PoisonQueueDepth" && a.Status == "open")
            .CountAsync(cancellationToken);

        return Ok(new
        {
            Timestamp = DateTime.UtcNow,
            Environment = _env.EnvironmentName,
            Health = new
            {
                Status = healthReport.Status.ToString(),
                Entries = healthReport.Entries.Select(e => new
                {
                    Name = e.Key,
                    Status = e.Value.Status.ToString(),
                    Description = e.Value.Description,
                    DurationMs = (long)e.Value.Duration.TotalMilliseconds
                })
            },
            Alerts = new
            {
                OpenCount = openAlerts,
                Resolved24h = resolvedAlerts,
                PoisonQueueDepth = poisonQueueDepth
            },
            Features = _featureFlags.GetAll(_tenant.TenantId),
            SloIndicators = new
            {
                // Availability target: 99.5 % (measured externally; reported here as last known)
                AvailabilityTargetPercent = 99.5,
                // MTTR target: < 30 min
                MttrTargetMinutes = 30,
                // Deployment frequency target: ≥ 1/day
                DeployFrequencyTarget = "daily"
            }
        });
    }

    // ─────────────────────────────────────────────
    // OPS-09 | Tenant capacity and cost metrics
    // GET /api/ops/tenant-capacity
    // ─────────────────────────────────────────────
    [HttpGet("tenant-capacity")]
    public async Task<IActionResult> GetTenantCapacity(CancellationToken cancellationToken)
    {
        var leadCount           = await _db.Leads.CountAsync(cancellationToken);
        var opportunityCount    = await _db.Opportunities.CountAsync(cancellationToken);
        var emailLogCount       = await _db.EmailLogs.CountAsync(cancellationToken);
        var ruleCount           = await _db.Rules.CountAsync(cancellationToken);
        var alertThresholdCount = await _db.AlertThresholds.CountAsync(cancellationToken);

        var leadsUtilisation = Math.Min(100.0, leadCount / 10.0);
        var emailUtilisation = Math.Min(100.0, emailLogCount / 100.0);

        return Ok(new
        {
            Timestamp = DateTime.UtcNow,
            Counts = new
            {
                Leads = leadCount,
                Opportunities = opportunityCount,
                EmailLogs = emailLogCount,
                ActiveRules = ruleCount,
                AlertThresholds = alertThresholdCount
            },
            Utilisation = new
            {
                LeadsPercent = Math.Round(leadsUtilisation, 1),
                EmailLogsPercent = Math.Round(emailUtilisation, 1)
            },
            CostIndicators = new
            {
                // Relative cost units – integrate with your billing provider for real $$
                StorageUnits = leadCount + emailLogCount,
                ComputeUnits = ruleCount * 2 + alertThresholdCount
            }
        });
    }

    // ─────────────────────────────────────────────
    // OPS-12 | Background job observability
    // GET /api/ops/job-status
    // ─────────────────────────────────────────────
    [HttpGet("job-status")]
    public async Task<IActionResult> GetJobStatus(CancellationToken cancellationToken)
    {
        var last24h = DateTime.UtcNow.AddHours(-24);

        // FollowUpJob status constants: Scheduled, Sent, Failed, Poisoned, Cancelled
        var pendingFollowUps = await _db.FollowUpJobs
            .Where(j => j.Status == "Scheduled")
            .CountAsync(cancellationToken);
        var failedFollowUps = await _db.FollowUpJobs
            .Where(j => (j.Status == "Failed" || j.Status == "Poisoned")
                        && j.ScheduledAtUtc >= last24h)
            .CountAsync(cancellationToken);

        // EmailDispatchJob status constants: Queued, Sent, Failed, Poisoned, Cancelled
        var pendingEmailJobs = await _db.EmailDispatchJobs
            .Where(j => j.Status == "Queued")
            .CountAsync(cancellationToken);
        var failedEmailJobs = await _db.EmailDispatchJobs
            .Where(j => (j.Status == "Failed" || j.Status == "Poisoned")
                        && j.CreatedAtUtc >= last24h)
            .CountAsync(cancellationToken);

        // ProposalReminderJob status constants: Scheduled, Sent, Failed, Poisoned, Cancelled
        var pendingReminderJobs = await _db.ProposalReminderJobs
            .Where(j => j.Status == "Scheduled")
            .CountAsync(cancellationToken);
        var failedReminderJobs = await _db.ProposalReminderJobs
            .Where(j => (j.Status == "Failed" || j.Status == "Poisoned")
                        && j.ScheduledAtUtc >= last24h)
            .CountAsync(cancellationToken);

        return Ok(new
        {
            Timestamp = DateTime.UtcNow,
            Jobs = new[]
            {
                new
                {
                    JobType = "FollowUpDispatch",
                    Pending = pendingFollowUps,
                    Failed24h = failedFollowUps,
                    AlertLevel = failedFollowUps > 5 ? "critical" : failedFollowUps > 0 ? "warning" : "ok"
                },
                new
                {
                    JobType = "EmailDispatch",
                    Pending = pendingEmailJobs,
                    Failed24h = failedEmailJobs,
                    AlertLevel = failedEmailJobs > 10 ? "critical" : failedEmailJobs > 0 ? "warning" : "ok"
                },
                new
                {
                    JobType = "ProposalReminder",
                    Pending = pendingReminderJobs,
                    Failed24h = failedReminderJobs,
                    AlertLevel = failedReminderJobs > 5 ? "critical" : failedReminderJobs > 0 ? "warning" : "ok"
                }
            }
        });
    }

    // ─────────────────────────────────────────────
    // OPS-13 | Scheduled job failure alerts
    // GET /api/ops/job-alerts
    // ─────────────────────────────────────────────
    [HttpGet("job-alerts")]
    public async Task<IActionResult> GetJobAlerts(
        [FromQuery] int thresholdFailures = 1,
        CancellationToken cancellationToken = default)
    {
        var last24h = DateTime.UtcNow.AddHours(-24);

        var failedFollowUps = await _db.FollowUpJobs
            .Where(j => (j.Status == "Failed" || j.Status == "Poisoned")
                        && j.ScheduledAtUtc >= last24h)
            .CountAsync(cancellationToken);

        var failedEmailJobs = await _db.EmailDispatchJobs
            .Where(j => (j.Status == "Failed" || j.Status == "Poisoned")
                        && j.CreatedAtUtc >= last24h)
            .CountAsync(cancellationToken);

        var failedReminderJobs = await _db.ProposalReminderJobs
            .Where(j => (j.Status == "Failed" || j.Status == "Poisoned")
                        && j.ScheduledAtUtc >= last24h)
            .CountAsync(cancellationToken);

        var alerts = new List<object>();

        if (failedFollowUps >= thresholdFailures)
            alerts.Add(new { Job = "FollowUpDispatch", FailedCount = failedFollowUps, Window = "24h",
                Severity = failedFollowUps > 5 ? "critical" : "warning" });

        if (failedEmailJobs >= thresholdFailures)
            alerts.Add(new { Job = "EmailDispatch", FailedCount = failedEmailJobs, Window = "24h",
                Severity = failedEmailJobs > 10 ? "critical" : "warning" });

        if (failedReminderJobs >= thresholdFailures)
            alerts.Add(new { Job = "ProposalReminder", FailedCount = failedReminderJobs, Window = "24h",
                Severity = failedReminderJobs > 5 ? "critical" : "warning" });

        return Ok(new
        {
            Timestamp   = DateTime.UtcNow,
            AlertCount  = alerts.Count,
            HasCritical = alerts.Any(a => (string)((dynamic)a).Severity == "critical"),
            Alerts      = alerts
        });
    }

    // ─────────────────────────────────────────────
    // OPS-14 | Environment configuration audit
    // GET /api/ops/config-audit
    // ─────────────────────────────────────────────
    [HttpGet("config-audit")]
    public IActionResult GetConfigAudit()
    {
        static string Mask(string? value) =>
            string.IsNullOrEmpty(value) ? "(not set)" :
            value.Length <= 4 ? "****" :
            string.Concat(value.AsSpan(0, 2), new string('*', Math.Max(0, value.Length - 4)), value.AsSpan(value.Length - 2));

        var findings = new List<object>();

        // Check JWT signing key is not the default development key
        var jwtKey = _configuration["Security:JwtSigningKey"] ?? "";
        if (jwtKey.Contains("DEV_ONLY", StringComparison.OrdinalIgnoreCase))
            findings.Add(new { Key = "Security:JwtSigningKey", Severity = "critical", Message = "Development signing key detected in non-development environment." });

        // Check CORS origins are explicit (no wildcard)
        var corsOrigins = _configuration.GetSection("Security:AllowedCorsOrigins").Get<string[]>() ?? [];
        if (corsOrigins.Any(o => o == "*"))
            findings.Add(new { Key = "Security:AllowedCorsOrigins", Severity = "critical", Message = "Wildcard CORS origin '*' detected." });

        // Check data governance retention is configured
        var retentionDays = _configuration.GetValue<int>("DataGovernance:RetentionDays");
        if (retentionDays == 0)
            findings.Add(new { Key = "DataGovernance:RetentionDays", Severity = "warning", Message = "Retention days not configured (defaults to 0 – no retention)." });

        return Ok(new
        {
            Timestamp = DateTime.UtcNow,
            Environment = _env.EnvironmentName,
            ConfigurationKeys = new
            {
                JwtIssuer = _configuration["Security:JwtIssuer"],
                JwtAudience = _configuration["Security:JwtAudience"],
                JwtSigningKeyMasked = Mask(_configuration["Security:JwtSigningKey"]),
                CorsOrigins = corsOrigins,
                DataRetentionDays = retentionDays,
                StrictMode = _configuration.GetValue<bool>("Security:StrictMode")
            },
            Findings = findings,
            FindingCount = findings.Count,
            HasCritical = findings.Any(f => ((dynamic)f).Severity == "critical")
        });
    }

    // ─────────────────────────────────────────────
    // OPS-05 | Feature flags snapshot
    // GET /api/ops/feature-flags
    // ─────────────────────────────────────────────
    [HttpGet("feature-flags")]
    public IActionResult GetFeatureFlags()
    {
        return Ok(new
        {
            Timestamp = DateTime.UtcNow,
            TenantId = _tenant.TenantId,
            Flags = _featureFlags.GetAll(_tenant.TenantId)
        });
    }
}
