using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public sealed class AnalyticsAdvancedTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"analytics_advanced_test_{Guid.NewGuid():N}.db";

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
        if (disposing && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}

public class AnalyticsAdvancedEndpointTests
{
    private static readonly Guid NewStageId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid QualifiedStageId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProposalStageId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid WonStageId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static HttpClient BuildClient() => new AnalyticsAdvancedTestFactory().CreateClient();

    [Fact]
    public async Task GetOverview_WithNoData_ReturnsZeroedKpis()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/analytics/advanced/overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AnalyticsAdvancedOverviewDto>();
        Assert.NotNull(body);
        Assert.Equal(0, body.Funnel.WonCount);
        Assert.Equal(0m, body.Revenue.WonRevenue);
        Assert.Equal(0m, body.OnboardingActivation.ActivationRate);
    }

    [Fact]
    public async Task GetOverview_WithCommercialFlow_ReturnsExpectedFunnelAndRevenue()
    {
        using var client = BuildClient();

        await CreateAssignmentUserAsync(client);

        var leadWonId = await CreateLeadAsync(client, "web");
        var leadOpenId = await CreateLeadAsync(client, "referral");

        var wonOpportunityId = await CreateOpportunityAsync(client, leadWonId, NewStageId, 10000m, "Deal Won");
        await MoveOpportunityAsync(client, wonOpportunityId, QualifiedStageId, "qualified");
        await MoveOpportunityAsync(client, wonOpportunityId, ProposalStageId, "proposal");
        await MoveOpportunityAsync(client, wonOpportunityId, WonStageId, "won");

        var openOpportunityId = await CreateOpportunityAsync(client, leadOpenId, NewStageId, 5000m, "Deal Open");
        await MoveOpportunityAsync(client, openOpportunityId, QualifiedStageId, "qualified");

        var response = await client.GetAsync("/api/analytics/advanced/overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AnalyticsAdvancedOverviewDto>();
        Assert.NotNull(body);

        Assert.Equal(2, body.Funnel.NewCount);
        Assert.Equal(2, body.Funnel.QualifiedCount);
        Assert.Equal(1, body.Funnel.ProposalCount);
        Assert.Equal(1, body.Funnel.WonCount);

        Assert.Equal(10000m, body.Revenue.WonRevenue);
        Assert.Equal(15000m, body.Revenue.PipelineRevenue);
        Assert.Equal(10000m, body.Revenue.AverageDealSize);

        Assert.Equal(1, body.OnboardingActivation.NewCustomers);
        Assert.Equal(0, body.OnboardingActivation.ActivatedCustomers);
        Assert.Equal(0m, body.OnboardingActivation.ActivationRate);
    }

    [Fact]
    public async Task AnalyticsAdvanced_IndividualEndpoints_ReturnOk()
    {
        using var client = BuildClient();

        var endpoints = new[]
        {
            "/api/analytics/advanced/funnel",
            "/api/analytics/advanced/revenue",
            "/api/analytics/advanced/velocity",
            "/api/analytics/advanced/sla",
            "/api/analytics/advanced/onboarding-activation"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await client.GetAsync(endpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task OnboardingActivation_WithTrackingEvent_ReturnsActivatedCustomer()
    {
        using var client = BuildClient();

        var leadId = await CreateLeadAsync(client, "ads");
        var opportunityId = await CreateOpportunityAsync(client, leadId, NewStageId, 2000m, "Activation deal");
        await MoveOpportunityAsync(client, opportunityId, WonStageId, "won directly");

        var customerResponse = await client.GetAsync($"/api/onboarding/customers/by-lead/{leadId}");
        Assert.Equal(HttpStatusCode.OK, customerResponse.StatusCode);

        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.NotNull(customer);

        var trackingResponse = await client.GetAsync($"/api/onboarding/track/{customer.TrackingToken}");
        Assert.Equal(HttpStatusCode.OK, trackingResponse.StatusCode);

        var response = await client.GetAsync("/api/analytics/advanced/onboarding-activation");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<OnboardingActivationKpiDto>();
        Assert.NotNull(body);
        Assert.Equal(1, body.NewCustomers);
        Assert.Equal(1, body.ActivatedCustomers);
        Assert.Equal(100m, body.ActivationRate);
    }

    private static async Task CreateAssignmentUserAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Analytics Agent",
            Email = $"assign_{Guid.NewGuid():N}@mindflow.test",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<Guid> CreateLeadAsync(HttpClient client, string source)
    {
        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"analytics_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = source
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LeadDto>();
        Assert.NotNull(body);
        return body.Id;
    }

    private static async Task<Guid> CreateOpportunityAsync(HttpClient client, Guid leadId, Guid stageId, decimal value, string title)
    {
        var response = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = leadId,
            StageId = stageId,
            Title = title,
            Value = value
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<OpportunityDto>();
        Assert.NotNull(body);
        return body.Id;
    }

    private static async Task MoveOpportunityAsync(HttpClient client, Guid opportunityId, Guid targetStageId, string reason)
    {
        var response = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunityId}/stage", new
        {
            TargetStageId = targetStageId,
            Reason = reason
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string BuildUniquePhone()
    {
        var n = Math.Abs(Guid.NewGuid().GetHashCode()) % 10_000_000;
        return $"+1555{n:D7}";
    }
}

file sealed class LeadDto
{
    public Guid Id { get; init; }
}

file sealed class OpportunityDto
{
    public Guid Id { get; init; }
}

file sealed class CustomerDto
{
    public Guid Id { get; init; }
    public string TrackingToken { get; init; } = string.Empty;
}

file sealed class AnalyticsAdvancedOverviewDto
{
    public FunnelKpiDto Funnel { get; init; } = new();
    public RevenueKpiDto Revenue { get; init; } = new();
    public OnboardingActivationKpiDto OnboardingActivation { get; init; } = new();
}

file sealed class FunnelKpiDto
{
    public int NewCount { get; init; }
    public int QualifiedCount { get; init; }
    public int ProposalCount { get; init; }
    public int WonCount { get; init; }
}

file sealed class RevenueKpiDto
{
    public decimal WonRevenue { get; init; }
    public decimal PipelineRevenue { get; init; }
    public decimal AverageDealSize { get; init; }
}

file sealed class OnboardingActivationKpiDto
{
    public int NewCustomers { get; init; }
    public int ActivatedCustomers { get; init; }
    public decimal ActivationRate { get; init; }
}
