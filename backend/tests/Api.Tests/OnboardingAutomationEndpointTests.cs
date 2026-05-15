using System.Net;
using System.Net.Http.Json;
using Api.Application.Email;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.Tests;

public class OnboardingAutomationTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"onboarding_test_{Guid.NewGuid():N}.db";

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

public class OnboardingAutomationEndpointTests
{
    private static HttpClient BuildClient() => new OnboardingAutomationTestFactory().CreateClient();

    [Fact]
    public async Task MoveOpportunityToWon_CreatesCustomer_AndDefaultOnboardingTasks()
    {
        using var client = BuildClient();

        var leadId = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);
        var proposalStage = stages.First(x => x.Name == "proposal");
        var wonStage = stages.First(x => x.Name == "won");

        var opportunityId = await CreateOpportunityAsync(client, leadId, proposalStage.Id);

        var moveResponse = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunityId}/stage", new
        {
            TargetStageId = wonStage.Id,
            Reason = "deal signed"
        });

        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

        var customerResponse = await client.GetAsync($"/api/onboarding/customers/by-lead/{leadId}");
        Assert.Equal(HttpStatusCode.OK, customerResponse.StatusCode);

        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponseDto>();
        Assert.NotNull(customer);
        Assert.Equal(leadId, customer.LeadId);
        Assert.Equal("Active", customer.Status);
        Assert.False(string.IsNullOrWhiteSpace(customer.TrackingToken));

        var tasksResponse = await client.GetAsync($"/api/onboarding/customers/{customer.Id}/tasks");
        Assert.Equal(HttpStatusCode.OK, tasksResponse.StatusCode);

        var tasks = await tasksResponse.Content.ReadFromJsonAsync<List<OnboardingTaskResponseDto>>();
        Assert.NotNull(tasks);
        Assert.Equal(3, tasks.Count);
        Assert.All(tasks, t => Assert.Equal("Pending", t.Status));
    }

    [Fact]
    public async Task MoveOpportunityToWon_WithNoSmtpConfigured_LogsSkippedWelcomeEmail()
    {
        using var client = BuildClient();

        var leadId = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);
        var opportunityId = await CreateOpportunityAsync(client, leadId, stages.First(x => x.Name == "proposal").Id);

        var moveResponse = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunityId}/stage", new
        {
            TargetStageId = stages.First(x => x.Name == "won").Id,
            Reason = "contract completed"
        });
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

        var logsResponse = await client.GetAsync("/api/email/logs");
        Assert.Equal(HttpStatusCode.OK, logsResponse.StatusCode);

        var logs = await logsResponse.Content.ReadFromJsonAsync<List<EmailLogResponseDto>>();
        Assert.NotNull(logs);
        Assert.Contains(logs, x => x.LeadId == leadId && x.TemplateName == "customer.welcome" && x.Status == "Skipped");
    }

    [Fact]
    public async Task OnboardingTrack_UpdatesActivationCounters()
    {
        using var client = BuildClient();

        var leadId = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);
        var opportunityId = await CreateOpportunityAsync(client, leadId, stages.First(x => x.Name == "proposal").Id);

        var moveResponse = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunityId}/stage", new
        {
            TargetStageId = stages.First(x => x.Name == "won").Id,
            Reason = "won for onboarding"
        });
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

        var customerResponse = await client.GetAsync($"/api/onboarding/customers/by-lead/{leadId}");
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponseDto>();
        Assert.NotNull(customer);

        var trackResponse = await client.GetAsync($"/api/onboarding/track/{customer.TrackingToken}");
        Assert.Equal(HttpStatusCode.OK, trackResponse.StatusCode);

        var refreshedResponse = await client.GetAsync($"/api/onboarding/customers/by-lead/{leadId}");
        var refreshed = await refreshedResponse.Content.ReadFromJsonAsync<CustomerResponseDto>();
        Assert.NotNull(refreshed);
        Assert.Equal(1, refreshed.TrackingActivations);
        Assert.NotNull(refreshed.LastTrackingActivatedAtUtc);
    }

    [Fact]
    public async Task MoveOpportunityToWon_Twice_DoesNotCreateDuplicateCustomer()
    {
        using var client = BuildClient();

        var leadId = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);
        var proposal = stages.First(x => x.Name == "proposal");
        var won = stages.First(x => x.Name == "won");
        var opportunityId = await CreateOpportunityAsync(client, leadId, proposal.Id);

        var first = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunityId}/stage", new
        {
            TargetStageId = won.Id,
            Reason = "first won"
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunityId}/stage", new
        {
            TargetStageId = won.Id,
            Reason = "repeat won"
        });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var listResponse = await client.GetAsync("/api/onboarding/customers");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var customers = await listResponse.Content.ReadFromJsonAsync<List<CustomerResponseDto>>();
        Assert.NotNull(customers);
        Assert.Single(customers, x => x.LeadId == leadId);
    }

    [Fact]
    public async Task MoveOpportunityToWon_WithWelcomeFailures_MovesJobToPoisonQueue_AndAllowsRequeue()
    {
        using var factory = new OnboardingFailingEmailFactory();
        var client = factory.CreateClient();

        var leadId = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);
        var opportunityId = await CreateOpportunityAsync(client, leadId, stages.First(x => x.Name == "proposal").Id);

        var moveResponse = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunityId}/stage", new
        {
            TargetStageId = stages.First(x => x.Name == "won").Id,
            Reason = "won with onboarding retry"
        });
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

        var customerResponse = await client.GetAsync($"/api/onboarding/customers/by-lead/{leadId}");
        Assert.Equal(HttpStatusCode.OK, customerResponse.StatusCode);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponseDto>();
        Assert.NotNull(customer);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var forceDueResponse = await client.PostAsync($"/api/onboarding/welcome-jobs/customers/{customer.Id}/force-due", null);
            Assert.Equal(HttpStatusCode.OK, forceDueResponse.StatusCode);

            var executeResponse = await client.PostAsync("/api/onboarding/welcome-jobs/execute-due", null);
            Assert.Equal(HttpStatusCode.OK, executeResponse.StatusCode);
        }

        var poisonResponse = await client.GetAsync("/api/onboarding/welcome-jobs/poison-queue");
        Assert.Equal(HttpStatusCode.OK, poisonResponse.StatusCode);

        var poisonJobs = await poisonResponse.Content.ReadFromJsonAsync<List<OnboardingWelcomeJobResponseDto>>();
        Assert.NotNull(poisonJobs);
        var poisoned = poisonJobs.Find(x => x.LeadId == leadId);
        Assert.NotNull(poisoned);
        Assert.Equal("Poisoned", poisoned.Status);
        Assert.Equal(3, poisoned.AttemptNumber);

        var requeueResponse = await client.PostAsync($"/api/onboarding/welcome-jobs/{poisoned.Id}/requeue", null);
        Assert.Equal(HttpStatusCode.OK, requeueResponse.StatusCode);

        var poisonAfterRequeueResponse = await client.GetAsync("/api/onboarding/welcome-jobs/poison-queue");
        var poisonAfterRequeue = await poisonAfterRequeueResponse.Content.ReadFromJsonAsync<List<OnboardingWelcomeJobResponseDto>>();
        Assert.NotNull(poisonAfterRequeue);
        Assert.DoesNotContain(poisonAfterRequeue, x => x.Id == poisoned.Id);
    }

    [Fact]
    public async Task MoveOpportunityToWon_CreatesSegmentedPlaybook_WithDependenciesAndDueDates()
    {
        using var client = BuildClient();

        var leadId = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);
        var opportunityId = await CreateOpportunityAsync(client, leadId, stages.First(x => x.Name == "proposal").Id);

        var moveResponse = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunityId}/stage", new
        {
            TargetStageId = stages.First(x => x.Name == "won").Id,
            Reason = "segmented onboarding"
        });
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

        var customerResponse = await client.GetAsync($"/api/onboarding/customers/by-lead/{leadId}");
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponseDto>();
        Assert.NotNull(customer);
        Assert.False(string.IsNullOrWhiteSpace(customer.PlaybookKey));
        Assert.False(string.IsNullOrWhiteSpace(customer.Segment));

        var tasksResponse = await client.GetAsync($"/api/onboarding/customers/{customer.Id}/tasks");
        var tasks = await tasksResponse.Content.ReadFromJsonAsync<List<OnboardingTaskResponseDto>>();
        Assert.NotNull(tasks);
        Assert.True(tasks.Count >= 3);
        Assert.Contains(tasks, x => x.DependencyKeys.Count > 0);
        Assert.All(tasks, x => Assert.NotNull(x.DueAtUtc));
    }

    [Fact]
    public async Task OnboardingTaskDependencies_BlockCompletionUntilPrerequisitesAreMet()
    {
        using var client = BuildClient();

        var leadId = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);
        var opportunityId = await CreateOpportunityAsync(client, leadId, stages.First(x => x.Name == "proposal").Id);

        await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunityId}/stage", new
        {
            TargetStageId = stages.First(x => x.Name == "won").Id,
            Reason = "dependency onboarding"
        });

        var customerResponse = await client.GetAsync($"/api/onboarding/customers/by-lead/{leadId}");
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponseDto>();
        Assert.NotNull(customer);

        var tasksResponse = await client.GetAsync($"/api/onboarding/customers/{customer.Id}/tasks");
        var tasks = await tasksResponse.Content.ReadFromJsonAsync<List<OnboardingTaskResponseDto>>();
        Assert.NotNull(tasks);

        var dependentTask = tasks.First(x => x.DependencyKeys.Count > 0);
        var blockedCompletion = await client.PostAsync($"/api/onboarding/tasks/{dependentTask.Id}/complete", null);
        Assert.Equal(HttpStatusCode.BadRequest, blockedCompletion.StatusCode);

        var prerequisiteTask = tasks.First(x => x.Key == dependentTask.DependencyKeys[0]);
        var prerequisiteCompletion = await client.PostAsync($"/api/onboarding/tasks/{prerequisiteTask.Id}/complete", null);
        Assert.Equal(HttpStatusCode.OK, prerequisiteCompletion.StatusCode);

        var completion = await client.PostAsync($"/api/onboarding/tasks/{dependentTask.Id}/complete", null);
        Assert.Equal(HttpStatusCode.OK, completion.StatusCode);
    }

    [Fact]
    public async Task OnboardingOverview_ReturnsActivationHealthAndSlaMetrics()
    {
        using var client = BuildClient();

        var leadId = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);
        var opportunityId = await CreateOpportunityAsync(client, leadId, stages.First(x => x.Name == "proposal").Id);

        await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunityId}/stage", new
        {
            TargetStageId = stages.First(x => x.Name == "won").Id,
            Reason = "overview onboarding"
        });

        var customerResponse = await client.GetAsync($"/api/onboarding/customers/by-lead/{leadId}");
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponseDto>();
        Assert.NotNull(customer);

        await client.GetAsync($"/api/onboarding/track/{customer.TrackingToken}");

        var overview = await client.GetAsync("/api/onboarding/overview");
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);
        var body = await overview.Content.ReadFromJsonAsync<OnboardingOverviewDto>();
        Assert.NotNull(body);
        Assert.True(body.TotalCustomers >= 1);
        Assert.True(body.EarlyActivationRatePercent >= 100m);
        Assert.True(body.AverageHealthScore >= 0m);
    }

    [Fact]
    public async Task EvaluateLifecycle_ProducesChurnRiskSignals()
    {
        using var client = BuildClient();

        var leadId = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);
        var opportunityId = await CreateOpportunityAsync(client, leadId, stages.First(x => x.Name == "proposal").Id);

        await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunityId}/stage", new
        {
            TargetStageId = stages.First(x => x.Name == "won").Id,
            Reason = "lifecycle onboarding"
        });

        var evaluate = await client.PostAsync("/api/onboarding/lifecycle/evaluate", null);
        Assert.Equal(HttpStatusCode.OK, evaluate.StatusCode);

        var customerResponse = await client.GetAsync($"/api/onboarding/customers/by-lead/{leadId}");
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponseDto>();
        Assert.NotNull(customer);
        Assert.Contains(customer.Status, new[] { "Active", "AtRisk", "ChurnRisk" });
        Assert.True(customer.HealthScore >= 0m);
    }

    private static async Task<Guid> CreateLeadAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"onb_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "onboarding-test"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LeadResponseDto>();
        Assert.NotNull(body);
        return body.Id;
    }

    private static async Task<List<(Guid Id, string Name)>> GetStagesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/pipeline/stages");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<PipelineStageResponseDto>>();
        Assert.NotNull(body);
        return body.Select(x => (x.Id, x.Name)).ToList();
    }

    private static async Task<Guid> CreateOpportunityAsync(HttpClient client, Guid leadId, Guid stageId)
    {
        var response = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = leadId,
            StageId = stageId,
            Title = "onboarding deal",
            Value = 15000m
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OpportunityResponseDto>();
        Assert.NotNull(body);
        return body.Id;
    }

    private static string BuildUniquePhone()
    {
        var n = Math.Abs(Guid.NewGuid().GetHashCode()) % 10_000_000;
        return $"+1555{n:D7}";
    }
}

public sealed class OnboardingFailingEmailFactory : OnboardingAutomationTestFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, AlwaysFailingOnboardingEmailService>();
        });
    }
}

file sealed class LeadResponseDto
{
    public Guid Id { get; init; }
}

file sealed class PipelineStageResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

file sealed class OpportunityResponseDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public Guid StageId { get; init; }
}

file sealed class CustomerResponseDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string TrackingToken { get; init; } = string.Empty;
    public int TrackingActivations { get; init; }
    public DateTime? LastTrackingActivatedAtUtc { get; init; }
    public string Segment { get; init; } = string.Empty;
    public string PlaybookKey { get; init; } = string.Empty;
    public decimal HealthScore { get; init; }
}

file sealed class OnboardingTaskResponseDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<string> DependencyKeys { get; init; } = [];
    public DateTime? DueAtUtc { get; init; }
}

file sealed class OnboardingOverviewDto
{
    public int TotalCustomers { get; init; }
    public decimal EarlyActivationRatePercent { get; init; }
    public decimal AverageHealthScore { get; init; }
}

file sealed class EmailLogResponseDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

file sealed class OnboardingWelcomeJobResponseDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public Guid LeadId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int AttemptNumber { get; init; }
}

file sealed class AlwaysFailingOnboardingEmailService : IEmailService
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
