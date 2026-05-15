using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Api.Application.Email;

namespace Api.Tests;

public class ProposalAutomationTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"proposal_test_{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LeadsDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<LeadsDbContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing || !File.Exists(_dbPath))
        {
            return;
        }

        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }
}

public class ProposalAutomationEndpointTests
{
    private static HttpClient BuildClient() => new ProposalAutomationTestFactory().CreateClient();

    [Fact]
    public async Task CreateProposal_GeneratesPdf_SchedulesReminder_AndCreatesTrackingToken()
    {
        using var client = BuildClient();
        var leadId = await CreateLeadAsync(client);

        var response = await client.PostAsJsonAsync("/api/proposals", new
        {
            LeadId = leadId,
            Title = "Enterprise Plan 2026",
            Amount = 18000m,
            Currency = "USD",
            RecipientName = "Jane Prospect"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(leadId, body.LeadId);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.False(string.IsNullOrWhiteSpace(body.TrackingToken));
        Assert.Equal(1, body.ReminderCount);
        Assert.Equal("Scheduled", body.ReminderStatus);

        var pdfResponse = await client.GetAsync($"/api/proposals/{body.Id}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdfResponse.StatusCode);
        Assert.Equal("application/pdf", pdfResponse.Content.Headers.ContentType?.MediaType);

        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(pdfBytes);
        var pdfHeader = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        Assert.Equal("%PDF", pdfHeader);
    }

    [Fact]
    public async Task CreateProposal_WithNoSmtpConfigured_RegistersSkippedEmailLog()
    {
        using var client = BuildClient();
        var leadId = await CreateLeadAsync(client);

        var response = await client.PostAsJsonAsync("/api/proposals", new
        {
            LeadId = leadId,
            Title = "Automation Package",
            Amount = 9500m,
            Currency = "USD"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var proposal = await response.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(proposal);

        var logsResponse = await client.GetAsync("/api/email/logs");
        Assert.Equal(HttpStatusCode.OK, logsResponse.StatusCode);
        var logs = await logsResponse.Content.ReadFromJsonAsync<List<EmailLogResponseDto>>();
        Assert.NotNull(logs);

        var proposalLog = logs.Find(x =>
            x.LeadId == leadId &&
            x.TemplateName == "proposal.standard" &&
            x.Status == "Skipped");

        Assert.NotNull(proposalLog);
    }

    [Fact]
    public async Task ExecuteDueProposalReminders_WhenDeliveryFails_SchedulesRetryAttempt()
    {
        using var client = BuildClient();
        var leadId = await CreateLeadAsync(client);

        var response = await client.PostAsJsonAsync("/api/proposals", new
        {
            LeadId = leadId,
            Title = "AI Sales Accelerator",
            Amount = 22000m,
            Currency = "USD"
        });
        var proposal = await response.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(proposal);

        var forceDueResponse = await client.PostAsync($"/api/proposals/{proposal.Id}/reminders/force-due", null);
        Assert.Equal(HttpStatusCode.OK, forceDueResponse.StatusCode);

        var executeResponse = await client.PostAsync("/api/proposals/reminders/execute-due", null);
        Assert.Equal(HttpStatusCode.OK, executeResponse.StatusCode);

        var detailsResponse = await client.GetAsync($"/api/proposals/{proposal.Id}");
        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
        var updated = await detailsResponse.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(updated);
        Assert.Equal("Scheduled", updated.ReminderStatus);
        Assert.Equal(2, updated.ReminderAttemptNumber);

        var logsResponse = await client.GetAsync("/api/email/logs");
        var logs = await logsResponse.Content.ReadFromJsonAsync<List<EmailLogResponseDto>>();
        Assert.NotNull(logs);
        Assert.Contains(logs, x => x.TemplateName == "proposal.reminder" && x.Status == "Skipped");
    }

    [Fact]
    public async Task TrackProposal_UpdatesTrackingCounters()
    {
        using var client = BuildClient();
        var leadId = await CreateLeadAsync(client);

        var response = await client.PostAsJsonAsync("/api/proposals", new
        {
            LeadId = leadId,
            Title = "MindFlow Premium",
            Amount = 12000m,
            Currency = "USD"
        });
        var proposal = await response.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(proposal);

        var tracked = await client.GetAsync($"/api/proposals/track/{proposal.TrackingToken}");
        Assert.Equal(HttpStatusCode.OK, tracked.StatusCode);

        var detailsResponse = await client.GetAsync($"/api/proposals/{proposal.Id}");
        var details = await detailsResponse.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(details);
        Assert.Equal(1, details.ViewCount);
        Assert.NotNull(details.LastViewedAtUtc);
    }

    [Fact]
    public async Task PoisonedProposalReminder_IsListedInPoisonQueue_AndCanBeRequeued()
    {
        using var client = BuildClient();
        var leadId = await CreateLeadAsync(client);

        var create = await client.PostAsJsonAsync("/api/proposals", new
        {
            LeadId = leadId,
            Title = "Dead Letter Proposal",
            Amount = 15000m,
            Currency = "USD"
        });
        var proposal = await create.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(proposal);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await client.PostAsync($"/api/proposals/{proposal.Id}/reminders/force-due", null);
            await client.PostAsync("/api/proposals/reminders/execute-due", null);
        }

        var poisonQueueResponse = await client.GetAsync("/api/proposals/reminders/poison-queue");
        Assert.Equal(HttpStatusCode.OK, poisonQueueResponse.StatusCode);

        var poisonQueue = await poisonQueueResponse.Content.ReadFromJsonAsync<List<ProposalReminderJobDto>>();
        Assert.NotNull(poisonQueue);
        var poisonedReminder = poisonQueue.Find(x => x.ProposalId == proposal.Id);
        Assert.NotNull(poisonedReminder);
        Assert.Equal("Poisoned", poisonedReminder.Status);
        Assert.Equal(3, poisonedReminder.AttemptNumber);

        var requeueResponse = await client.PostAsync($"/api/proposals/{proposal.Id}/reminders/requeue", null);
        Assert.Equal(HttpStatusCode.OK, requeueResponse.StatusCode);

        var updatedProposalResponse = await client.GetAsync($"/api/proposals/{proposal.Id}");
        var updatedProposal = await updatedProposalResponse.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(updatedProposal);
        Assert.Equal("Scheduled", updatedProposal.ReminderStatus);
        Assert.Equal(4, updatedProposal.ReminderAttemptNumber);

        var poisonQueueAfterRequeue = await client.GetAsync("/api/proposals/reminders/poison-queue");
        var poisonQueueAfter = await poisonQueueAfterRequeue.Content.ReadFromJsonAsync<List<ProposalReminderJobDto>>();
        Assert.NotNull(poisonQueueAfter);
        Assert.DoesNotContain(poisonQueueAfter, x => x.ProposalId == proposal.Id);
    }

    [Fact]
    public async Task ExecuteDueProposalReminders_RetriesUntilPoisonQueue_WhenDeliveryKeepsFailing()
    {
        using var factory = new ProposalAutomationFailingEmailFactory();
        var client = factory.CreateClient();
        var leadId = await CreateLeadAsync(client);

        var create = await client.PostAsJsonAsync("/api/proposals", new
        {
            LeadId = leadId,
            Title = "Retry Policy Proposal",
            Amount = 21000m,
            Currency = "USD"
        });
        var proposal = await create.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(proposal);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await client.PostAsync($"/api/proposals/{proposal.Id}/reminders/force-due", null);
            var executeResponse = await client.PostAsync("/api/proposals/reminders/execute-due", null);
            Assert.Equal(HttpStatusCode.OK, executeResponse.StatusCode);
        }

        var poisonQueueResponse = await client.GetAsync("/api/proposals/reminders/poison-queue");
        Assert.Equal(HttpStatusCode.OK, poisonQueueResponse.StatusCode);

        var poisonQueue = await poisonQueueResponse.Content.ReadFromJsonAsync<List<ProposalReminderJobDto>>();
        Assert.NotNull(poisonQueue);

        var poisoned = poisonQueue.Find(x => x.ProposalId == proposal.Id);
        Assert.NotNull(poisoned);
        Assert.Equal("Poisoned", poisoned.Status);
        Assert.Equal(3, poisoned.AttemptNumber);
    }

    [Fact]
    public async Task PoisonQueueGrowth_CreatesOperationalAlertEvent()
    {
        using var factory = new ProposalAutomationFailingEmailFactory();
        var client = factory.CreateClient();
        var leadId = await CreateLeadAsync(client);

        var thresholdCreate = await client.PostAsJsonAsync("/api/analytics/advanced/alert-thresholds", new
        {
            EndpointName = "poison-queue/proposal-reminder",
            MaxErrorRatePercent = 100m,
            MaxAverageLatencyMs = 0m,
            NotificationEmail = "ops@novamind.local",
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, thresholdCreate.StatusCode);

        var create = await client.PostAsJsonAsync("/api/proposals", new
        {
            LeadId = leadId,
            Title = "Operational alert proposal",
            Amount = 17000m,
            Currency = "USD"
        });
        var proposal = await create.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(proposal);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await client.PostAsync($"/api/proposals/{proposal.Id}/reminders/force-due", null);
            var executeResponse = await client.PostAsync("/api/proposals/reminders/execute-due", null);
            Assert.Equal(HttpStatusCode.OK, executeResponse.StatusCode);
        }

        var eventsResponse = await client.GetAsync("/api/analytics/advanced/alert-events?endpointName=poison-queue/proposal-reminder&metricName=PoisonQueueDepth");
        Assert.Equal(HttpStatusCode.OK, eventsResponse.StatusCode);

        var eventsPayload = await eventsResponse.Content.ReadFromJsonAsync<AlertEventListDto>();
        Assert.NotNull(eventsPayload);
        Assert.Contains(eventsPayload.Items, x => x.EndpointName == "poison-queue/proposal-reminder" && x.MetricName == "PoisonQueueDepth");
    }

    [Fact]
    public async Task PoisonQueueGrowth_WithinCooldown_DeduplicatesSmallIncrease()
    {
        using var factory = new ProposalAutomationFailingEmailFactory();
        var client = factory.CreateClient();

        await EnsurePoisonQueueThresholdAsync(client, "poison-queue/proposal-reminder");

        var leadA = await CreateLeadAsync(client);
        await CreatePoisonedProposalReminderAsync(client, leadA, "Cooldown A");

        var leadB = await CreateLeadAsync(client);
        await CreatePoisonedProposalReminderAsync(client, leadB, "Cooldown B");

        var eventsCount = await GetPoisonQueueEventsCountAsync(client, "poison-queue/proposal-reminder");
        Assert.Equal(1, eventsCount);
    }

    [Fact]
    public async Task PoisonQueueGrowth_WithinCooldown_AllowsSignificantJump()
    {
        using var factory = new ProposalAutomationFailingEmailFactory();
        var client = factory.CreateClient();

        await EnsurePoisonQueueThresholdAsync(client, "poison-queue/proposal-reminder");

        var leadA = await CreateLeadAsync(client);
        await CreatePoisonedProposalReminderAsync(client, leadA, "Jump A");

        var leadB = await CreateLeadAsync(client);
        await CreatePoisonedProposalReminderAsync(client, leadB, "Jump B");

        var leadC = await CreateLeadAsync(client);
        await CreatePoisonedProposalReminderAsync(client, leadC, "Jump C");

        var eventsCount = await GetPoisonQueueEventsCountAsync(client, "poison-queue/proposal-reminder");
        Assert.True(eventsCount >= 2);
    }

    [Fact]
    public async Task ProposalTemplates_SupportVersioning_AndNewProposalUsesLatestCurrentVersion()
    {
        using var client = BuildClient();
        var leadId = await CreateLeadAsync(client);

        var templateV2 = await client.PostAsJsonAsync("/api/proposals/templates", new
        {
            Name = "proposal.standard",
            DisplayName = "Proposal Standard v2",
            HtmlBody = "<h1>Version 2</h1><p>{{proposal_title}}</p>",
            MakeCurrent = true
        });
        Assert.Equal(HttpStatusCode.Created, templateV2.StatusCode);

        var createProposal = await client.PostAsJsonAsync("/api/proposals", new
        {
            LeadId = leadId,
            Title = "Template Versioned Proposal",
            Amount = 11100m,
            Currency = "USD"
        });
        Assert.Equal(HttpStatusCode.Created, createProposal.StatusCode);

        var proposal = await createProposal.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(proposal);
        Assert.True(proposal.TemplateVersion >= 2);

        var templates = await client.GetAsync("/api/proposals/templates");
        Assert.Equal(HttpStatusCode.OK, templates.StatusCode);
        var templateItems = await templates.Content.ReadFromJsonAsync<List<ProposalTemplateDto>>();
        Assert.NotNull(templateItems);
        Assert.Contains(templateItems, x => x.Name == "proposal.standard" && x.IsCurrent && x.Version >= 2);
    }

    [Fact]
    public async Task ProposalLifecycle_SupportsGranularStatus_Signature_Expiry_AndRenewal()
    {
        using var client = BuildClient();
        var leadId = await CreateLeadAsync(client);

        var create = await client.PostAsJsonAsync("/api/proposals", new
        {
            LeadId = leadId,
            Title = "Lifecycle Proposal",
            Amount = 14000m,
            Currency = "USD"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var proposal = await create.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(proposal);

        var sign = await client.PostAsJsonAsync($"/api/proposals/{proposal.Id}/sign", new
        {
            SignerName = "Jane Prospect",
            SignerEmail = "jane@prospect.test"
        });
        Assert.Equal(HttpStatusCode.OK, sign.StatusCode);

        var signed = await sign.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(signed);
        Assert.Equal("Signed", signed.Status);
        Assert.NotNull(signed.SignedAtUtc);

        var expire = await client.PostAsync($"/api/proposals/{proposal.Id}/expire", null);
        Assert.Equal(HttpStatusCode.OK, expire.StatusCode);

        var renew = await client.PostAsJsonAsync($"/api/proposals/{proposal.Id}/renew", new
        {
            NewExpiryDays = 14
        });
        Assert.Equal(HttpStatusCode.OK, renew.StatusCode);

        var renewed = await renew.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(renewed);
        Assert.Equal(proposal.Id, renewed.RenewedFromProposalId);
        Assert.Equal("Sent", renewed.Status);
    }

    [Fact]
    public async Task ProposalKpis_ReturnConversionAndReminderMetrics()
    {
        using var client = BuildClient();
        var leadId = await CreateLeadAsync(client);

        var stages = await GetStagesAsync(client);
        var proposalStage = stages.First(x => x.Name == "proposal");
        var wonStage = stages.First(x => x.Name == "won");
        var opportunityId = await CreateOpportunityAsync(client, leadId, proposalStage.Id);

        var createProposal = await client.PostAsJsonAsync("/api/proposals", new
        {
            LeadId = leadId,
            Title = "KPI Proposal",
            Amount = 20000m,
            Currency = "USD"
        });
        Assert.Equal(HttpStatusCode.Created, createProposal.StatusCode);
        var proposal = await createProposal.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(proposal);

        var track = await client.GetAsync($"/api/proposals/track/{proposal.TrackingToken}");
        Assert.Equal(HttpStatusCode.OK, track.StatusCode);

        var moveWon = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunityId}/stage", new
        {
            TargetStageId = wonStage.Id,
            Reason = "proposal accepted"
        });
        Assert.Equal(HttpStatusCode.OK, moveWon.StatusCode);

        var kpis = await client.GetAsync("/api/proposals/kpis");
        Assert.Equal(HttpStatusCode.OK, kpis.StatusCode);
        var body = await kpis.Content.ReadFromJsonAsync<ProposalKpiDto>();
        Assert.NotNull(body);
        Assert.True(body.TotalProposals >= 1);
        Assert.True(body.ProposalToWonRate >= 100m);
        Assert.True(body.TrackedProposals >= 1);
    }

    private static async Task<List<(Guid Id, string Name)>> GetStagesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/pipeline/stages");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<PipelineStageDto>>();
        Assert.NotNull(body);
        return body.Select(x => (x.Id, x.Name)).ToList();
    }

    private static async Task<Guid> CreateOpportunityAsync(HttpClient client, Guid leadId, Guid stageId)
    {
        var response = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = leadId,
            StageId = stageId,
            Title = "proposal linked opportunity",
            Value = 25000m
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OpportunityDto>();
        Assert.NotNull(body);
        return body.Id;
    }

    private static async Task EnsurePoisonQueueThresholdAsync(HttpClient client, string endpointName)
    {
        var thresholdCreate = await client.PostAsJsonAsync("/api/analytics/advanced/alert-thresholds", new
        {
            EndpointName = endpointName,
            MaxErrorRatePercent = 100m,
            MaxAverageLatencyMs = 0m,
            NotificationEmail = "ops@novamind.local",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.Created, thresholdCreate.StatusCode);
    }

    private static async Task CreatePoisonedProposalReminderAsync(HttpClient client, Guid leadId, string title)
    {
        var create = await client.PostAsJsonAsync("/api/proposals", new
        {
            LeadId = leadId,
            Title = title,
            Amount = 19000m,
            Currency = "USD"
        });
        var proposal = await create.Content.ReadFromJsonAsync<ProposalResponseDto>();
        Assert.NotNull(proposal);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await client.PostAsync($"/api/proposals/{proposal.Id}/reminders/force-due", null);
            var executeResponse = await client.PostAsync("/api/proposals/reminders/execute-due", null);
            Assert.Equal(HttpStatusCode.OK, executeResponse.StatusCode);
        }
    }

    private static async Task<int> GetPoisonQueueEventsCountAsync(HttpClient client, string endpointName)
    {
        var eventsResponse = await client.GetAsync($"/api/analytics/advanced/alert-events?endpointName={endpointName}&metricName=PoisonQueueDepth");
        Assert.Equal(HttpStatusCode.OK, eventsResponse.StatusCode);

        var payload = await eventsResponse.Content.ReadFromJsonAsync<AlertEventListDto>();
        Assert.NotNull(payload);
        return payload.Items.Count;
    }

    private static async Task<Guid> CreateLeadAsync(HttpClient client)
    {
        var email = $"proposal_{Guid.NewGuid():N}@mindflow.test";
        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = email,
            Phone = BuildUniquePhone(),
            Source = "proposal-test"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LeadResponseDto>();
        Assert.NotNull(body);
        return body.Id;
    }

    private static string BuildUniquePhone()
    {
        var n = Math.Abs(Guid.NewGuid().GetHashCode()) % 10_000_000;
        return $"+1555{n:D7}";
    }
}

public sealed class ProposalAutomationFailingEmailFactory : ProposalAutomationTestFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, AlwaysFailingProposalEmailService>();
        });
    }
}

file sealed class LeadResponseDto
{
    public Guid Id { get; init; }
    public string? Email { get; init; }
}

file sealed class ProposalResponseDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string TrackingToken { get; init; } = string.Empty;
    public int ViewCount { get; init; }
    public DateTime? LastViewedAtUtc { get; init; }
    public int ReminderCount { get; init; }
    public string ReminderStatus { get; init; } = string.Empty;
    public int ReminderAttemptNumber { get; init; }
    public int TemplateVersion { get; init; }
    public DateTime? SignedAtUtc { get; init; }
    public Guid? RenewedFromProposalId { get; init; }
}

file sealed class ProposalTemplateDto
{
    public string Name { get; init; } = string.Empty;
    public int Version { get; init; }
    public bool IsCurrent { get; init; }
}

file sealed class ProposalKpiDto
{
    public int TotalProposals { get; init; }
    public decimal ProposalToWonRate { get; init; }
    public int TrackedProposals { get; init; }
}

file sealed class PipelineStageDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

file sealed class OpportunityDto
{
    public Guid Id { get; init; }
}

file sealed class EmailLogResponseDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string? ToEmail { get; init; }
    public string? Subject { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime SentAtUtc { get; init; }
}

file sealed class ProposalReminderJobDto
{
    public Guid Id { get; init; }
    public Guid ProposalId { get; init; }
    public Guid LeadId { get; init; }
    public string? ToEmail { get; init; }
    public string Status { get; init; } = string.Empty;
    public int AttemptNumber { get; init; }
    public DateTime DueAtUtc { get; init; }
    public DateTime? ExecutedAtUtc { get; init; }
    public string? ErrorMessage { get; init; }
}

file sealed class AlertEventDto
{
    public Guid Id { get; init; }
    public string EndpointName { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public decimal ObservedValue { get; init; }
    public decimal ThresholdValue { get; init; }
}

file sealed class AlertEventListDto
{
    public List<AlertEventDto> Items { get; init; } = [];
}

file sealed class AlwaysFailingProposalEmailService : IEmailService
{
    public Task SendLeadWelcomeAsync(Guid leadId, string? toEmail, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SendLeadFollowUpAsync(Guid leadId, string? toEmail, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<bool> SendProposalAsync(Guid leadId, string? toEmail, string recipientName, string proposalTitle, decimal amount, string currency, string trackingUrl, byte[] pdfBytes, string pdfFileName, CancellationToken cancellationToken)
        => Task.FromResult(false);

    public Task<bool> SendProposalReminderAsync(Guid leadId, string? toEmail, string recipientName, string proposalTitle, string trackingUrl, CancellationToken cancellationToken)
        => Task.FromResult(false);

    public Task<bool> SendCustomerWelcomeAsync(Guid leadId, string? toEmail, string trackingUrl, CancellationToken cancellationToken)
        => Task.FromResult(false);

    public Task<bool> SendAnalyticsDegradationAlertAsync(string toEmail, string endpointName, string metricName, decimal observedValue, decimal thresholdValue, DateTime triggeredAtUtc, CancellationToken cancellationToken)
        => Task.FromResult(false);
}
