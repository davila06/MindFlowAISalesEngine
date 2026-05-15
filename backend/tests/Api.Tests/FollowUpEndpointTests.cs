using System.Net;
using System.Net.Http.Json;
using Api.Domain.FollowUp;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Api.Infrastructure.Persistence;
using Api.Application.Email;
using Api.Application.FollowUp;

namespace Api.Tests;

/// <summary>
/// Isolated factory for follow-up tests: unique SQLite DB per instance to
/// prevent state contamination between tests.
/// </summary>
public class FollowUpTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"followup_test_{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LeadsDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<LeadsDbContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));
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

public class FollowUpEndpointTests
{
    private static HttpClient BuildClient() => new FollowUpTestFactory().CreateClient();

    // ─── RED 1: Intake lead → follow-up job scheduled ────────────────────────
    [Fact]
    public async Task IntakeLead_SchedulesFollowUpJob_ListedInJobs()
    {
        using var client = BuildClient();

        var email = $"fu_{Guid.NewGuid():N}@test.com";
        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = email,
            Phone = BuildUniquePhone(),
            Source = "followup-test"
        });

        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);
        var lead = await intake.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(lead);

        var jobs = await client.GetAsync("/api/followup/jobs");
        Assert.Equal(HttpStatusCode.OK, jobs.StatusCode);

        var body = await jobs.Content.ReadFromJsonAsync<List<FollowUpJobResponse>>();
        Assert.NotNull(body);

        var job = body.Find(j => j.LeadId == lead.Id);
        Assert.NotNull(job);
        Assert.Equal("Scheduled", job.Status);
        Assert.Equal(1, job.AttemptNumber);

        // Due at should be ~48h from now
        var expectedDue = DateTime.UtcNow.AddHours(48);
        Assert.True(job.DueAtUtc >= expectedDue.AddMinutes(-5));
        Assert.True(job.DueAtUtc <= expectedDue.AddMinutes(5));
    }

    // ─── RED 2: Cancel follow-up for a lead ─────────────────────────────────
    [Fact]
    public async Task CancelFollowUpByLead_MarksJobAsCancelled()
    {
        using var client = BuildClient();

        var email = $"fu_{Guid.NewGuid():N}@test.com";
        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = email,
            Phone = BuildUniquePhone(),
            Source = "cancel-test"
        });
        var lead = await intake.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(lead);

        var cancel = await client.PostAsJsonAsync(
            $"/api/followup/leads/{lead.Id}/cancel",
            new { Reason = "Lead responded" });

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        // Verify job is now cancelled
        var jobs = await client.GetAsync("/api/followup/jobs");
        var body = await jobs.Content.ReadFromJsonAsync<List<FollowUpJobResponse>>();
        Assert.NotNull(body);

        var job = body.Find(j => j.LeadId == lead.Id);
        Assert.NotNull(job);
        Assert.Equal("Cancelled", job.Status);
        Assert.Equal("Lead responded", job.CancelReason);
    }

    // ─── RED 3: Cancel a specific job by id ──────────────────────────────────
    [Fact]
    public async Task CancelFollowUpById_MarksSpecificJobCancelled()
    {
        using var client = BuildClient();

        var email = $"fu_{Guid.NewGuid():N}@test.com";
        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = email,
            Phone = BuildUniquePhone(),
            Source = "cancel-id-test"
        });
        var lead = await intake.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(lead);

        var jobs = await client.GetAsync("/api/followup/jobs");
        var body = await jobs.Content.ReadFromJsonAsync<List<FollowUpJobResponse>>();
        var job = body!.Find(j => j.LeadId == lead.Id);
        Assert.NotNull(job);

        var cancel = await client.PostAsJsonAsync(
            $"/api/followup/jobs/{job.Id}/cancel",
            new { Reason = "Manual cancel" });

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var refreshed = await client.GetAsync("/api/followup/jobs");
        var refreshedBody = await refreshed.Content.ReadFromJsonAsync<List<FollowUpJobResponse>>();
        var updated = refreshedBody!.Find(j => j.Id == job.Id);

        Assert.NotNull(updated);
        Assert.Equal("Cancelled", updated.Status);
        Assert.Equal("Manual cancel", updated.CancelReason);
    }

    // ─── RED 4: GET /api/followup/jobs returns empty list for fresh db ────────
    [Fact]
    public async Task GetJobs_WithNoLeads_ReturnsEmptyList()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/followup/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<FollowUpJobResponse>>();
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    // ─── RED 5: GET /api/followup/leads/{leadId}/jobs → list for that lead ────
    [Fact]
    public async Task GetJobsByLead_ReturnsJobsForThatLeadOnly()
    {
        using var client = BuildClient();

        var email1 = $"fu_{Guid.NewGuid():N}@test.com";
        var email2 = $"fu_{Guid.NewGuid():N}@test.com";

        var intake1 = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = email1, Phone = BuildUniquePhone(), Source = "lead-jobs-test"
        });
        var intake2 = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = email2, Phone = BuildUniquePhone(), Source = "lead-jobs-test"
        });

        var lead1 = await intake1.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(lead1);

        var jobs = await client.GetAsync($"/api/followup/leads/{lead1.Id}/jobs");
        Assert.Equal(HttpStatusCode.OK, jobs.StatusCode);

        var body = await jobs.Content.ReadFromJsonAsync<List<FollowUpJobResponse>>();
        Assert.NotNull(body);
        Assert.All(body, j => Assert.Equal(lead1.Id, j.LeadId));
        Assert.Single(body);
    }

    [Fact]
    public async Task RequeueDeadLetterJob_CreatesRetryAttempt()
    {
        using var factory = new FollowUpTestFactory();
        var client = factory.CreateClient();

        try
        {
            var intake = await client.PostAsJsonAsync("/api/leads/intake", new
            {
                Email = $"fu_{Guid.NewGuid():N}@test.com",
                Phone = BuildUniquePhone(),
                Source = "dead-letter-test"
            });
            var lead = await intake.Content.ReadFromJsonAsync<LeadResponse>();
            Assert.NotNull(lead);

            Guid failedJobId;
            using (var scope = factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();
                var job = await dbContext.FollowUpJobs.SingleAsync(x => x.LeadId == lead.Id);
                job.MarkFailed("smtp-timeout");
                await dbContext.SaveChangesAsync();
                failedJobId = job.Id;
            }

            var deadLetterResponse = await client.GetAsync("/api/followup/dead-letter");
            Assert.Equal(HttpStatusCode.OK, deadLetterResponse.StatusCode);

            var deadLetters = await deadLetterResponse.Content.ReadFromJsonAsync<List<FollowUpJobResponse>>();
            Assert.NotNull(deadLetters);
            Assert.Contains(deadLetters, x => x.Id == failedJobId && x.Status == FollowUpJobStatus.Failed);

            var requeueResponse = await client.PostAsync($"/api/followup/jobs/{failedJobId}/requeue", content: null);
            Assert.Equal(HttpStatusCode.OK, requeueResponse.StatusCode);

            var jobsResponse = await client.GetAsync($"/api/followup/leads/{lead.Id}/jobs");
            var jobs = await jobsResponse.Content.ReadFromJsonAsync<List<FollowUpJobResponse>>();
            Assert.NotNull(jobs);

            Assert.Contains(jobs, x => x.Id == failedJobId && x.Status == FollowUpJobStatus.Failed);
            Assert.Contains(jobs, x => x.Id != failedJobId && x.Status == FollowUpJobStatus.Scheduled && x.AttemptNumber == 2);
        }
        finally
        {
            client.Dispose();
        }
    }

    [Fact]
    public async Task ExecuteDueFollowUp_RetriesUntilPoisonQueue_WhenDeliveryKeepsFailing()
    {
        using var factory = new FollowUpFailingEmailFactory();
        var client = factory.CreateClient();

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"fu_{Guid.NewGuid():N}@test.com",
            Phone = BuildUniquePhone(),
            Source = "retry-policy-test"
        });
        var lead = await intake.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(lead);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await ForceLatestFollowUpDueAsync(factory, lead.Id);

            using var scope = factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IFollowUpService>();
            await service.ExecuteDueJobsAsync(CancellationToken.None);
        }

        var poisonQueueResponse = await client.GetAsync("/api/followup/poison-queue");
        Assert.Equal(HttpStatusCode.OK, poisonQueueResponse.StatusCode);

        var poisonQueue = await poisonQueueResponse.Content.ReadFromJsonAsync<List<FollowUpJobResponse>>();
        Assert.NotNull(poisonQueue);

        var poisoned = poisonQueue.Find(x => x.LeadId == lead.Id);
        Assert.NotNull(poisoned);
        Assert.Equal("Poisoned", poisoned.Status);
        Assert.Equal(3, poisoned.AttemptNumber);
    }

    [Fact]
    public async Task FollowUpPolicy_AppliesSegmentDelayAndQuietHours()
    {
        using var client = BuildClient();

        var nowHour = DateTime.UtcNow.Hour;
        var quietHoursEnd = (nowHour + 1) % 24;

        var policyResponse = await client.PutAsJsonAsync(
            "/api/followup/policy",
            new
            {
                QuietHoursEnabled = true,
                QuietHoursStartHourUtc = nowHour,
                QuietHoursEndHourUtc = quietHoursEnd,
                Rules = new[]
                {
                    new { StageName = "new", MinimumScore = 80, DelayHours = 24 }
                }
            });

        Assert.Equal(HttpStatusCode.OK, policyResponse.StatusCode);

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"policy_{Guid.NewGuid():N}@test.com",
            Phone = BuildUniquePhone(),
            Source = "referral"
        });

        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);
        var lead = await intake.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(lead);

        var jobsResponse = await client.GetAsync($"/api/followup/leads/{lead.Id}/jobs");
        var jobs = await jobsResponse.Content.ReadFromJsonAsync<List<FollowUpJobResponse>>();
        Assert.NotNull(jobs);

        var job = Assert.Single(jobs);
        var minExpected = DateTime.UtcNow.AddHours(24);
        var maxExpected = DateTime.UtcNow.AddHours(26);

        Assert.True(job.DueAtUtc >= minExpected.AddMinutes(-5));
        Assert.True(job.DueAtUtc <= maxExpected.AddMinutes(5));
        Assert.Equal(quietHoursEnd, job.DueAtUtc.Hour);
    }

    [Fact]
    public async Task StopList_SuppressesWelcomeAndFollowUpScheduling()
    {
        using var client = BuildClient();

        var email = $"suppressed_{Guid.NewGuid():N}@test.com";
        var unsubscribeResponse = await client.PostAsJsonAsync(
            "/api/email/stop-list",
            new { Email = email, Reason = "unsubscribe" });

        Assert.Equal(HttpStatusCode.OK, unsubscribeResponse.StatusCode);

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = email,
            Phone = BuildUniquePhone(),
            Source = "web"
        });

        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);
        var lead = await intake.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(lead);

        var jobsResponse = await client.GetAsync($"/api/followup/leads/{lead.Id}/jobs");
        var jobs = await jobsResponse.Content.ReadFromJsonAsync<List<FollowUpJobResponse>>();
        Assert.NotNull(jobs);
        Assert.Empty(jobs);

        var logsResponse = await client.GetAsync("/api/email/logs");
        var logs = await logsResponse.Content.ReadFromJsonAsync<List<EmailLogResponse>>();
        Assert.NotNull(logs);

        var welcomeLog = logs.Find(x => x.TemplateName == "lead.welcome");
        Assert.NotNull(welcomeLog);
        Assert.Equal("Suppressed", welcomeLog.Status);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────
    private static string BuildUniquePhone()
    {
        var n = Math.Abs(Guid.NewGuid().GetHashCode()) % 10_000_000;
        return $"+1555{n:D7}";
    }

    private static async Task ForceLatestFollowUpDueAsync(WebApplicationFactory<Program> factory, Guid leadId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();
        var job = await dbContext.FollowUpJobs
            .Where(x => x.LeadId == leadId)
            .OrderByDescending(x => x.AttemptNumber)
            .ThenByDescending(x => x.ScheduledAtUtc)
            .FirstAsync();

        dbContext.Entry(job).Property(nameof(FollowUpJob.DueAtUtc)).CurrentValue = DateTime.UtcNow.AddSeconds(-1);
        await dbContext.SaveChangesAsync();
    }
}

public sealed class FollowUpFailingEmailFactory : FollowUpTestFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, AlwaysFailingEmailService>();
        });
    }
}

// ─── Local DTOs ─────────────────────────────────────────────────────────────
file sealed class LeadResponse
{
    public Guid Id { get; init; }
    public string? Email { get; init; }
}

file sealed class FollowUpJobResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string? ToEmail { get; init; }
    public string Status { get; init; } = string.Empty;
    public int AttemptNumber { get; init; }
    public DateTime ScheduledAtUtc { get; init; }
    public DateTime DueAtUtc { get; init; }
    public DateTime? ExecutedAtUtc { get; init; }
    public DateTime? CancelledAtUtc { get; init; }
    public string? CancelReason { get; init; }
    public string? ErrorMessage { get; init; }
}

file sealed class EmailLogResponse
{
    public string TemplateName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

file sealed class AlwaysFailingEmailService : IEmailService
{
    public Task SendLeadWelcomeAsync(Guid leadId, string? toEmail, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SendLeadFollowUpAsync(Guid leadId, string? toEmail, CancellationToken cancellationToken)
        => throw new InvalidOperationException("SimulatedFollowUpFailure");

    public Task<bool> SendProposalAsync(Guid leadId, string? toEmail, string recipientName, string proposalTitle, decimal amount, string currency, string trackingUrl, byte[] pdfBytes, string pdfFileName, CancellationToken cancellationToken)
        => Task.FromResult(false);

    public Task<bool> SendProposalReminderAsync(Guid leadId, string? toEmail, string recipientName, string proposalTitle, string trackingUrl, CancellationToken cancellationToken)
        => Task.FromResult(false);

    public Task<bool> SendCustomerWelcomeAsync(Guid leadId, string? toEmail, string trackingUrl, CancellationToken cancellationToken)
        => Task.FromResult(false);

    public Task<bool> SendAnalyticsDegradationAlertAsync(string toEmail, string endpointName, string metricName, decimal observedValue, decimal thresholdValue, DateTime triggeredAtUtc, CancellationToken cancellationToken)
        => Task.FromResult(false);
}
