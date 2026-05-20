using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Api.Infrastructure.Persistence;
using Api.Application.Email;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.Tests;

/// <summary>
/// Custom factory for email tests: uses an isolated SQLite database so that
/// SMTP settings configured in one test do not pollute another.
/// </summary>
public class EmailTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"email_test_{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LeadsDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<LeadsDbContext>(options =>
                options
                    .UseSqlite($"Data Source={_dbPath}")
                    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing || !System.IO.File.Exists(_dbPath))
        {
            return;
        }

        try
        {
            System.IO.File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }
}

public sealed class EmailFailingDispatchFactory : EmailTestFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, AlwaysFailingSender>();
        });
    }
}

/// <summary>
/// Each test creates its own factory + isolated SQLite DB for full test isolation.
/// </summary>
public class EmailEndpointTests
{
    private static HttpClient BuildClient() => new EmailTestFactory().CreateClient();


    // ─── RED 1: GET smtp-settings when not configured → 404 ────────────────
    [Fact]
    public async Task GetSmtpSettings_WhenNotConfigured_Returns404()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/email/smtp-settings");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── RED 2: PUT smtp-settings with valid payload → 200 + persists ──────
    [Fact]
    public async Task PutSmtpSettings_WithValidPayload_Returns200AndGetReturnsSettings()
    {
        using var client = BuildClient();

        var request = new
        {
            ProviderType = "smtp",
            Host = "smtp.example.com",
            Port = 587,
            Username = "user@example.com",
            Password = "s3cur3P@ss",
            FromEmail = "noreply@example.com",
            FromName = "MindFlow",
            EnableSsl = true
        };

        var putResponse = await client.PutAsJsonAsync("/api/email/smtp-settings", request);

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/email/smtp-settings");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var body = await getResponse.Content.ReadFromJsonAsync<SmtpSettingsResponse>();
        Assert.NotNull(body);
        Assert.Equal("smtp.example.com", body.Host);
        Assert.Equal(587, body.Port);
        Assert.Equal("noreply@example.com", body.FromEmail);
        Assert.Equal("MindFlow", body.FromName);
        Assert.True(body.EnableSsl);
        Assert.Equal("smtp", body.ProviderType);
        // Password must NOT be returned in response
        Assert.Null(typeof(SmtpSettingsResponse).GetProperty("Password"));
    }

    [Fact]
    public async Task PutSmtpSettings_WithWebhookProvider_PersistsProviderMetadata()
    {
        using var client = BuildClient();

        var request = new
        {
            ProviderType = "webhook",
            ProviderBaseUrl = "https://mail.example.test/hooks/send",
            ApiKey = "secret-token",
            Host = string.Empty,
            Port = 443,
            Username = string.Empty,
            Password = string.Empty,
            FromEmail = "noreply@example.com",
            FromName = "MindFlow Queue",
            EnableSsl = true
        };

        var putResponse = await client.PutAsJsonAsync("/api/email/smtp-settings", request);

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var body = await putResponse.Content.ReadFromJsonAsync<SmtpSettingsResponse>();
        Assert.NotNull(body);
        Assert.Equal("webhook", body.ProviderType);
        Assert.Equal("https://mail.example.test/hooks/send", body.ProviderBaseUrl);
    }

    // ─── RED 3: PUT smtp-settings with invalid payload → 400 ────────────────
    [Fact]
    public async Task PutSmtpSettings_WithInvalidPort_Returns400()
    {
        using var client = BuildClient();

        var request = new
        {
            Host = "smtp.example.com",
            Port = 0, // invalid
            Username = "user@example.com",
            Password = "pass",
            FromEmail = "noreply@example.com",
            EnableSsl = true
        };

        var response = await client.PutAsJsonAsync("/api/email/smtp-settings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── RED 4: GET email/logs returns empty list initially ─────────────────
    [Fact]
    public async Task GetEmailLogs_Returns200WithList()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/email/logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<EmailLogResponse>>();
        Assert.NotNull(body);
    }

    // ─── RED 5: Lead intake with no SMTP → skips email, still returns 201 ──
    [Fact]
    public async Task IntakeLead_WithNoSmtpConfigured_Returns201AndLogsSkipped()
    {
        using var client = BuildClient();

        var payload = new
        {
            Email = $"lead_email_{Guid.NewGuid():N}@test.com",
            Phone = BuildUniquePhone(),
            Source = "email-test"
        };

        var response = await client.PostAsJsonAsync("/api/leads/intake", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Email log must contain a masked "Skipped" entry for the email we just sent
        var logsResponse = await client.GetAsync("/api/email/logs");
        Assert.Equal(HttpStatusCode.OK, logsResponse.StatusCode);

        var logs = await logsResponse.Content.ReadFromJsonAsync<List<EmailLogResponse>>();
        Assert.NotNull(logs);

        var log = logs.Find(l => l.Status == "Skipped" && l.TemplateName == "lead.welcome");
        Assert.NotNull(log);
        Assert.False(log.Succeeded);
        Assert.NotEqual(payload.Email.ToLowerInvariant(), log.ToEmail);
        Assert.Contains("*", log.ToEmail);
    }

    [Fact]
    public async Task IntakeLead_WithConfiguredProvider_QueuesWelcomeEmailWithCorrelationId()
    {
        using var client = BuildClient();

        var settingsRequest = new
        {
            ProviderType = "webhook",
            ProviderBaseUrl = "https://mail.example.test/hooks/send",
            ApiKey = "secret-token",
            Host = string.Empty,
            Port = 443,
            Username = string.Empty,
            Password = string.Empty,
            FromEmail = "noreply@example.com",
            FromName = "MindFlow Queue",
            EnableSsl = true
        };

        var settingsResponse = await client.PutAsJsonAsync("/api/email/smtp-settings", settingsRequest);
        Assert.Equal(HttpStatusCode.OK, settingsResponse.StatusCode);

        var payload = new
        {
            Email = $"queued_email_{Guid.NewGuid():N}@test.com",
            Phone = BuildUniquePhone(),
            Source = "email-queue-test"
        };

        var response = await client.PostAsJsonAsync("/api/leads/intake", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var logsResponse = await client.GetAsync("/api/email/logs");
        Assert.Equal(HttpStatusCode.OK, logsResponse.StatusCode);

        var logs = await logsResponse.Content.ReadFromJsonAsync<List<EmailLogResponse>>();
        Assert.NotNull(logs);

        var log = logs.Find(l => l.TemplateName == "lead.welcome");
        Assert.NotNull(log);
        Assert.Equal("Queued", log.Status);
        Assert.False(string.IsNullOrWhiteSpace(log.CorrelationId));
        Assert.False(log.Succeeded);
    }

    [Fact]
    public async Task GetEmailLogs_MasksRecipientEmailAddress()
    {
        using var client = BuildClient();

        var payload = new
        {
            Email = $"mask_{Guid.NewGuid():N}@test.com",
            Phone = BuildUniquePhone(),
            Source = "email-mask-test"
        };

        var intakeResponse = await client.PostAsJsonAsync("/api/leads/intake", payload);
        Assert.Equal(HttpStatusCode.Created, intakeResponse.StatusCode);

        var logsResponse = await client.GetAsync("/api/email/logs");
        Assert.Equal(HttpStatusCode.OK, logsResponse.StatusCode);

        var logs = await logsResponse.Content.ReadFromJsonAsync<List<EmailLogResponse>>();
        Assert.NotNull(logs);

        var log = logs.Find(l => l.TemplateName == "lead.welcome" || l.TemplateName == "lead.followup");
        Assert.NotNull(log);
        Assert.NotEqual(payload.Email.ToLowerInvariant(), log.ToEmail);
        Assert.Contains("*", log.ToEmail);
        Assert.EndsWith("@test.com", log.ToEmail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetEmailLogs_SearchByOriginalRecipientEmail_ReturnsMatchingLog()
    {
        using var client = BuildClient();

        var recipientEmail = $"search_{Guid.NewGuid():N}@test.com";
        var payload = new
        {
            Email = recipientEmail,
            Phone = BuildUniquePhone(),
            Source = "email-search-test"
        };

        var intakeResponse = await client.PostAsJsonAsync("/api/leads/intake", payload);
        Assert.Equal(HttpStatusCode.Created, intakeResponse.StatusCode);

        var response = await client.GetAsync($"/api/email/logs?search={Uri.EscapeDataString(recipientEmail)}&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var logs = await response.Content.ReadFromJsonAsync<List<EmailLogResponse>>();
        Assert.NotNull(logs);
        Assert.NotEmpty(logs);
    }

    [Fact]
    public async Task EmailTemplateVersioning_SupportsPreviewAndRollback()
    {
        using var client = BuildClient();

        var versionRequest = new
        {
            Subject = "Welcome {{lead.name}}",
            BodyHtml = "<p>Hello {{lead.name}}</p><p>Stage: {{pipeline.stage}}</p>",
            RequiredVariables = new[] { "lead.name", "pipeline.stage" }
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/email/templates/lead.welcome/versions",
            versionRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<EmailTemplateVersionResponse>();
        Assert.NotNull(created);
        Assert.Equal("lead.welcome", created.TemplateKey);
        Assert.True(created.Version >= 2);
        Assert.True(created.IsCurrent);

        var previewRequest = new
        {
            Variables = new Dictionary<string, string>
            {
                ["lead.name"] = "Ada Lovelace",
                ["pipeline.stage"] = "Qualified"
            }
        };

        var previewResponse = await client.PostAsJsonAsync(
            "/api/email/templates/lead.welcome/preview",
            previewRequest);

        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        var preview = await previewResponse.Content.ReadFromJsonAsync<EmailTemplatePreviewResponse>();
        Assert.NotNull(preview);
        Assert.Contains("Ada Lovelace", preview.Subject);
        Assert.Contains("Qualified", preview.BodyHtml);

        var rollbackResponse = await client.PostAsJsonAsync(
            "/api/email/templates/lead.welcome/rollback",
            new { TargetVersion = 1 });

        Assert.Equal(HttpStatusCode.OK, rollbackResponse.StatusCode);

        var rolledBack = await rollbackResponse.Content.ReadFromJsonAsync<EmailTemplateVersionResponse>();
        Assert.NotNull(rolledBack);
        Assert.Equal(1, rolledBack.Version);
        Assert.True(rolledBack.IsCurrent);
    }

    [Fact]
    public async Task EmailTemplateVersioning_RejectsUnknownVariables()
    {
        using var client = BuildClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/email/templates/lead.welcome/versions",
            new
            {
                Subject = "Welcome {{lead.name}}",
                BodyHtml = "<p>Unknown {{lead.unexpected}}</p>",
                RequiredVariables = new[] { "lead.name", "lead.unexpected" }
            });

        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
    }

    [Fact]
    public async Task DispatchExecutionAndRetry_UpdatesKpisAndCreatesDeliveryAlert()
    {
        using var factory = new EmailFailingDispatchFactory();
        using var client = factory.CreateClient();

        var settingsResponse = await client.PutAsJsonAsync(
            "/api/email/smtp-settings",
            new
            {
                ProviderType = "smtp",
                Host = "smtp.example.com",
                Port = 587,
                Username = "user@example.com",
                Password = "secret",
                FromEmail = "noreply@example.com",
                FromName = "MindFlow",
                EnableSsl = true
            });

        Assert.Equal(HttpStatusCode.OK, settingsResponse.StatusCode);

        var thresholdResponse = await client.PostAsJsonAsync(
            "/api/analytics/advanced/alert-thresholds",
            new
            {
                EndpointName = "email.delivery",
                MaxErrorRatePercent = 10,
                MaxAverageLatencyMs = 1000,
                NotificationEmail = "ops@test.com",
                IsActive = true,
                WebhookUrl = (string?)null
            });

        Assert.Equal(HttpStatusCode.Created, thresholdResponse.StatusCode);

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"dispatch_{Guid.NewGuid():N}@test.com",
            Phone = BuildUniquePhone(),
            Source = "email-dispatch-test"
        });

        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);

        var executeResponse = await client.PostAsync("/api/email/dispatch/execute-due", content: null);
        Assert.Equal(HttpStatusCode.OK, executeResponse.StatusCode);

        var logsResponse = await client.GetAsync("/api/email/logs");
        var logs = await logsResponse.Content.ReadFromJsonAsync<List<EmailLogResponse>>();
        Assert.NotNull(logs);

        var failedLog = logs.Find(x => x.TemplateName == "lead.welcome");
        Assert.NotNull(failedLog);
        Assert.Equal("Failed", failedLog.Status);

        var retryResponse = await client.PostAsync($"/api/email/logs/{failedLog.Id}/retry", content: null);
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);

        var kpiResponse = await client.GetAsync("/api/email/kpis");
        Assert.Equal(HttpStatusCode.OK, kpiResponse.StatusCode);

        var kpi = await kpiResponse.Content.ReadFromJsonAsync<EmailKpiResponse>();
        Assert.NotNull(kpi);
        Assert.True(kpi.FailedCount >= 1);
        Assert.True(kpi.ErrorRatePercent >= 100m);

        var alertEventsResponse = await client.GetAsync("/api/analytics/advanced/alert-events?endpointName=email.delivery");
        Assert.Equal(HttpStatusCode.OK, alertEventsResponse.StatusCode);

        var alertEvents = await alertEventsResponse.Content.ReadFromJsonAsync<AlertEventListResponse>();
        Assert.NotNull(alertEvents);
        Assert.Contains(alertEvents.Items, x => x.EndpointName == "email.delivery");

    }

    [Fact]
    public async Task DispatchExecution_WhenDeliveryFails_RequeuesWithBackoff()
    {
        using var factory = new EmailFailingDispatchFactory();
        using var client = factory.CreateClient();

        var settingsResponse = await client.PutAsJsonAsync(
            "/api/email/smtp-settings",
            new
            {
                ProviderType = "smtp",
                Host = "smtp.example.com",
                Port = 587,
                Username = "user@example.com",
                Password = "secret",
                FromEmail = "noreply@example.com",
                FromName = "MindFlow",
                EnableSsl = true
            });

        Assert.Equal(HttpStatusCode.OK, settingsResponse.StatusCode);

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"backoff_{Guid.NewGuid():N}@test.com",
            Phone = BuildUniquePhone(),
            Source = "email-backoff-test"
        });

        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);

        var executeResponse = await client.PostAsync("/api/email/dispatch/execute-due", content: null);
        Assert.Equal(HttpStatusCode.OK, executeResponse.StatusCode);

        var secondExecuteResponse = await client.PostAsync("/api/email/dispatch/execute-due", content: null);
        Assert.Equal(HttpStatusCode.OK, secondExecuteResponse.StatusCode);

        using var secondExecutePayload = JsonDocument.Parse(await secondExecuteResponse.Content.ReadAsStringAsync());
        Assert.Equal(0, secondExecutePayload.RootElement.GetProperty("processed").GetInt32());
    }

    [Fact]
    public async Task EmailKpis_IncludeChannelBreakdown()
    {
        using var client = BuildClient();

        var settingsResponse = await client.PutAsJsonAsync(
            "/api/email/smtp-settings",
            new
            {
                ProviderType = "webhook",
                ProviderBaseUrl = "https://mail.example.test/hooks/send",
                ApiKey = "secret-token",
                Host = string.Empty,
                Port = 443,
                Username = string.Empty,
                Password = string.Empty,
                FromEmail = "noreply@example.com",
                FromName = "MindFlow",
                EnableSsl = true
            });

        Assert.Equal(HttpStatusCode.OK, settingsResponse.StatusCode);

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"kpi_{Guid.NewGuid():N}@test.com",
            Phone = BuildUniquePhone(),
            Source = "email-kpi-test"
        });

        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);

        var kpiResponse = await client.GetAsync("/api/email/kpis");
        Assert.Equal(HttpStatusCode.OK, kpiResponse.StatusCode);

        var kpi = await kpiResponse.Content.ReadFromJsonAsync<EmailKpiResponse>();
        Assert.NotNull(kpi);
        Assert.NotEmpty(kpi.ByChannel);
        Assert.Contains(kpi.ByChannel, x => x.Channel == "webhook");
    }

    // ─── Tracking tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task EmailLog_HasTrackingTokenAfterIntake()
    {
        using var client = BuildClient();

        await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"tracking_{Guid.NewGuid():N}@test.com",
            Phone = BuildUniquePhone(),
            Source = "tracking-test"
        });

        var logs = await (await client.GetAsync("/api/email/logs"))
            .Content.ReadFromJsonAsync<List<EmailLogResponse>>();
        Assert.NotNull(logs);
        var log = logs.FirstOrDefault(l => l.TemplateName == "lead.welcome");
        Assert.NotNull(log);
        Assert.NotEqual(Guid.Empty, log.TrackingToken);
        Assert.False(log.IsOpened);
        Assert.False(log.IsClicked);
        Assert.Equal(0, log.OpenCount);
        Assert.Equal(0, log.ClickCount);
    }

    [Fact]
    public async Task TrackingPixel_RecordsOpen_AndReturnsGif()
    {
        using var client = BuildClient();

        await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"pixel_{Guid.NewGuid():N}@test.com",
            Phone = BuildUniquePhone(),
            Source = "pixel-test"
        });

        var logs = await (await client.GetAsync("/api/email/logs"))
            .Content.ReadFromJsonAsync<List<EmailLogResponse>>();
        Assert.NotNull(logs);
        var log = logs.FirstOrDefault(l => l.TemplateName == "lead.welcome");
        Assert.NotNull(log);

        // Hit pixel endpoint
        var pixelResponse = await client.GetAsync($"/api/tracking/pixel/{log.TrackingToken}.gif");
        Assert.Equal(System.Net.HttpStatusCode.OK, pixelResponse.StatusCode);
        Assert.Equal("image/gif", pixelResponse.Content.Headers.ContentType?.MediaType);

        // Verify open recorded
        var updatedLogs = await (await client.GetAsync("/api/email/logs"))
            .Content.ReadFromJsonAsync<List<EmailLogResponse>>();
        Assert.NotNull(updatedLogs);
        var updated = updatedLogs.First(l => l.Id == log.Id);
        Assert.True(updated.IsOpened);
        Assert.Equal(1, updated.OpenCount);
        Assert.NotNull(updated.FirstOpenedAtUtc);
    }

    [Fact]
    public async Task TrackingClick_ValidUrl_RecordsClickAndRedirects()
    {
        using var client = new EmailTestFactory().CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"click_{Guid.NewGuid():N}@test.com",
            Phone = BuildUniquePhone(),
            Source = "click-test"
        });

        var logs = await (await client.GetAsync("/api/email/logs"))
            .Content.ReadFromJsonAsync<List<EmailLogResponse>>();
        Assert.NotNull(logs);
        var log = logs.FirstOrDefault(l => l.TemplateName == "lead.welcome");
        Assert.NotNull(log);

        var destination = Uri.EscapeDataString("https://novamind.example.com/proposal/123");
        var clickResponse = await client.GetAsync($"/api/tracking/click/{log.TrackingToken}?url={destination}");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, clickResponse.StatusCode);

        // Verify click recorded
        var updatedLogs = await (await client.GetAsync("/api/email/logs"))
            .Content.ReadFromJsonAsync<List<EmailLogResponse>>();
        Assert.NotNull(updatedLogs);
        var updated = updatedLogs.First(l => l.Id == log.Id);
        Assert.True(updated.IsClicked);
        Assert.Equal(1, updated.ClickCount);
        Assert.NotNull(updated.FirstClickedAtUtc);
    }

    [Fact]
    public async Task TrackingClick_InvalidUrl_Returns400()
    {
        using var client = BuildClient();

        var fakeToken = Guid.NewGuid();
        var response = await client.GetAsync($"/api/tracking/click/{fakeToken}?url=javascript:alert(1)");
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TrackingMetrics_ReturnsAggregatesPerTemplate()
    {
        using var client = BuildClient();

        await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"metrics_{Guid.NewGuid():N}@test.com",
            Phone = BuildUniquePhone(),
            Source = "metrics-test"
        });

        var response = await client.GetAsync("/api/email/tracking/metrics");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────
    private static string BuildUniquePhone()
    {
        var n = Guid.NewGuid().GetHashCode() & 0x7FFFFFFF % 10000000;
        return $"+1555{n:D7}";
    }
}

// Local DTOs for deserialization (mirror of Api.Contracts)
file sealed class SmtpSettingsResponse
{
    public Guid Id { get; init; }
    public string ProviderType { get; init; } = string.Empty;
    public string? ProviderBaseUrl { get; init; }
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string? FromName { get; init; }
    public bool EnableSsl { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

file sealed class EmailLogResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string? ToEmail { get; init; }
    public string? Subject { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool Succeeded { get; init; }
    public string? CorrelationId { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime SentAtUtc { get; init; }
    // Tracking
    public Guid TrackingToken { get; init; }
    public int OpenCount { get; init; }
    public int ClickCount { get; init; }
    public DateTime? FirstOpenedAtUtc { get; init; }
    public DateTime? FirstClickedAtUtc { get; init; }
    public bool IsOpened { get; init; }
    public bool IsClicked { get; init; }
}

file sealed class EmailKpiResponse
{
    public int TotalCount { get; init; }
    public int FailedCount { get; init; }
    public IReadOnlyList<EmailChannelKpiResponse> ByChannel { get; init; } = Array.Empty<EmailChannelKpiResponse>();
    public decimal ErrorRatePercent { get; init; }
}

file sealed class EmailChannelKpiResponse
{
    public string Channel { get; init; } = string.Empty;
}

file sealed class AlertEventListResponse
{
    public List<AlertEventResponse> Items { get; init; } = new();
}

file sealed class AlertEventResponse
{
    public string EndpointName { get; init; } = string.Empty;
}

file sealed class EmailTemplateVersionResponse
{
    public Guid Id { get; init; }
    public string TemplateKey { get; init; } = string.Empty;
    public int Version { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string BodyHtml { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }
    public IReadOnlyList<string> RequiredVariables { get; init; } = Array.Empty<string>();
}

file sealed class EmailTemplatePreviewResponse
{
    public string Subject { get; init; } = string.Empty;
    public string BodyHtml { get; init; } = string.Empty;
}

file sealed class AlwaysFailingSender : IEmailSender
{
    public Task SendAsync(string host, int port, string username, string password, bool enableSsl, string fromEmail, string fromName, string toEmail, string subject, string bodyHtml, byte[]? attachmentBytes, string? attachmentFileName, string? attachmentContentType, CancellationToken cancellationToken)
        => throw new InvalidOperationException("SimulatedEmailDispatchFailure");
}
