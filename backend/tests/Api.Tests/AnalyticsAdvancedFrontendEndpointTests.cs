using System.Net;
using System.Net.Http.Json;

namespace Api.Tests;

public class AnalyticsAdvancedFrontendEndpointTests
{
    private static readonly Guid WonStageId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid NewStageId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static HttpClient BuildClient() => new DashboardTestFactory().CreateClient();

    [Fact]
    public async Task LegacyAnalyticsAdvancedHtml_IsNotServed()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/analytics-advanced.html");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdvancedAnalytics_WithInvalidGroupBy_ReturnsBadRequest()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/analytics/advanced/overview?groupBy=quarter");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdvancedAnalytics_WithValidGroupBy_ReturnsOverviewPayload()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/analytics/advanced/overview?groupBy=day");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AdvancedOverviewDto>();
        Assert.NotNull(payload);
        Assert.NotNull(payload.Funnel);
        Assert.NotNull(payload.Revenue);
    }

    [Fact]
    public async Task AdvancedAnalyticsOverviewCsv_ReturnsCsvFile()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/analytics/advanced/overview/csv?groupBy=day");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Section,Metric,Value", csv);
        Assert.Contains("Funnel,NewCount", csv);
        Assert.Contains("Revenue,WonRevenue", csv);
    }

    [Fact]
    public async Task WeeklyReportRun_ReturnsWeeklyPayload()
    {
        using var client = BuildClient();

        var response = await client.PostAsync("/api/analytics/advanced/reports/weekly/run", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<WeeklyAnalyticsReportDto>();
        Assert.NotNull(payload);
        Assert.NotNull(payload.DashboardOverview);
        Assert.NotNull(payload.AdvancedOverview);
        Assert.True(payload.WindowStartUtc <= payload.WindowEndUtc);
    }

    [Fact]
    public async Task ScopeMetrics_ReturnsSellerTeamAndTenantBreakdown()
    {
        using var client = BuildClient();

        var userAId = await CreateAssignmentUser(client, "Seller A", $"sellera_{Guid.NewGuid():N}@mindflow.test");
        var userBId = await CreateAssignmentUser(client, "Seller B", $"sellerb_{Guid.NewGuid():N}@mindflow.test");

        var lead1 = await CreateLead(client, "ao03");
        var lead2 = await CreateLead(client, "ao03");
        var lead3 = await CreateLead(client, "ao03");

        await CreateOpportunity(client, lead1, WonStageId, 1000m, "Won A");
        await CreateOpportunity(client, lead2, NewStageId, 500m, "Open B");
        await CreateOpportunity(client, lead3, WonStageId, 2000m, "Won A2");

        var response = await client.GetAsync("/api/analytics/advanced/metrics/scope?groupBy=day");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ScopeMetricsDto>();
        Assert.NotNull(payload);
        Assert.NotNull(payload.Tenant);
        Assert.Equal("default", payload.Tenant.TenantId);
        Assert.NotNull(payload.Sellers);
        Assert.Equal(2, payload.Sellers.Count);
        Assert.Contains(payload.Sellers, s => s.UserId == userAId && s.AssignedLeadsCount == 2 && s.WonLeadsCount == 2);
        Assert.Contains(payload.Sellers, s => s.UserId == userBId && s.AssignedLeadsCount == 1 && s.WonLeadsCount == 0);
        Assert.NotNull(payload.Teams);
        Assert.NotEmpty(payload.Teams);
        Assert.Contains(payload.Teams, t => t.TeamKey == "round_robin");
    }

    [Fact]
    public async Task PeriodOverPeriodComparison_ReturnsCurrentPreviousAndDelta()
    {
        using var client = BuildClient();

        var leadId = await CreateLead(client, "ao04");
        await CreateOpportunity(client, leadId, WonStageId, 1500m, "Won AO-04");

        var startUtc = DateTime.UtcNow.AddDays(-1).ToString("O");
        var endUtc = DateTime.UtcNow.AddDays(1).ToString("O");

        var response = await client.GetAsync($"/api/analytics/advanced/comparisons/period-over-period?groupBy=day&startDateUtc={Uri.EscapeDataString(startUtc)}&endDateUtc={Uri.EscapeDataString(endUtc)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PeriodOverPeriodComparisonDto>();
        Assert.NotNull(payload);
        Assert.NotNull(payload.Current);
        Assert.NotNull(payload.Previous);
        Assert.NotNull(payload.Delta);
        Assert.True(payload.Current.Revenue.WonRevenue >= 1500m);
        Assert.Equal(0m, payload.Previous.Revenue.WonRevenue);
        Assert.True(payload.Delta.WonRevenueDelta >= 1500m);
    }

    [Fact]
    public async Task Segmentation_ReturnsSourceCampaignAndIndustryBreakdown()
    {
        using var client = BuildClient();

        var leadA = await CreateLead(client, "web", campaign: "spring_sale");
        var leadB = await CreateLead(client, "ads", campaign: "ads_q2");
        var leadC = await CreateLead(client, "web", campaign: "spring_sale");

        await CreateCompany(client, leadA, "Acme SaaS", "saas");
        await CreateCompany(client, leadB, "Retail Corp", "retail");

        await CreateOpportunity(client, leadA, WonStageId, 1000m, "Won A");
        await CreateOpportunity(client, leadB, NewStageId, 200m, "Open B");
        await CreateOpportunity(client, leadC, WonStageId, 300m, "Won C");

        var response = await client.GetAsync("/api/analytics/advanced/segments?groupBy=day");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SegmentationDto>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.BySource);
        Assert.NotEmpty(payload.ByCampaign);
        Assert.NotEmpty(payload.ByIndustry);

        Assert.Contains(payload.BySource, x => x.Key == "web" && x.TotalLeads == 2 && x.WonLeads == 2);
        Assert.Contains(payload.ByCampaign, x => x.Key == "spring_sale" && x.TotalLeads == 2 && x.WonLeads == 2);
        Assert.Contains(payload.ByIndustry, x => x.Key == "saas" && x.TotalLeads == 1 && x.WonLeads == 1);
        Assert.Contains(payload.ByIndustry, x => x.Key == "retail" && x.TotalLeads == 1 && x.WonLeads == 0);
    }

    private static async Task<Guid> CreateLead(HttpClient client, string source, string? campaign = null)
    {
        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"ao03_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = source,
            Campaign = campaign
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var lead = await response.Content.ReadFromJsonAsync<LeadIntakeDto>();
        Assert.NotNull(lead);
        return lead.Id;
    }

    private static async Task CreateOpportunity(HttpClient client, Guid leadId, Guid stageId, decimal value, string title)
    {
        var response = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = leadId,
            StageId = stageId,
            Title = title,
            Value = value
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<Guid> CreateAssignmentUser(HttpClient client, string fullName, string email)
    {
        var response = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = fullName,
            Email = email,
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AssignmentUserDto>();
        Assert.NotNull(payload);
        return payload.Id;
    }

    private static async Task CreateCompany(HttpClient client, Guid leadId, string name, string industry)
    {
        var response = await client.PostAsJsonAsync("/api/companies", new
        {
            LeadId = leadId,
            Name = name,
            Industry = industry,
            Website = "https://example.com"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static string BuildUniquePhone()
    {
        var n = Math.Abs(Guid.NewGuid().GetHashCode()) % 10_000_000;
        return $"+1555{n:D7}";
    }
}

file sealed class AdvancedOverviewDto
{
    public object Funnel { get; init; } = new();
    public RevenueOverviewDto Revenue { get; init; } = new();
}

file sealed class RevenueOverviewDto
{
    public decimal WonRevenue { get; init; }
}

file sealed class WeeklyAnalyticsReportDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public DateTime WindowStartUtc { get; init; }
    public DateTime WindowEndUtc { get; init; }
    public object DashboardOverview { get; init; } = new();
    public object AdvancedOverview { get; init; } = new();
}

file sealed class LeadIntakeDto
{
    public Guid Id { get; init; }
}

file sealed class AssignmentUserDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

file sealed class ScopeMetricsDto
{
    public TenantScopeMetricDto Tenant { get; init; } = new();
    public List<SellerScopeMetricDto> Sellers { get; init; } = [];
    public List<TeamScopeMetricDto> Teams { get; init; } = [];
}

file sealed class TenantScopeMetricDto
{
    public string TenantId { get; init; } = string.Empty;
    public int TotalLeads { get; init; }
    public int AssignedLeadsCount { get; init; }
    public int WonLeadsCount { get; init; }
}

file sealed class SellerScopeMetricDto
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public int AssignedLeadsCount { get; init; }
    public int WonLeadsCount { get; init; }
}

file sealed class TeamScopeMetricDto
{
    public string TeamKey { get; init; } = string.Empty;
    public int AssignedLeadsCount { get; init; }
    public int WonLeadsCount { get; init; }
}

file sealed class PeriodOverPeriodComparisonDto
{
    public AdvancedOverviewDto Current { get; init; } = new();
    public AdvancedOverviewDto Previous { get; init; } = new();
    public PeriodDeltaDto Delta { get; init; } = new();
}

file sealed class PeriodDeltaDto
{
    public decimal WonRevenueDelta { get; init; }
}

file sealed class SegmentationDto
{
    public List<SegmentMetricDto> BySource { get; init; } = [];
    public List<SegmentMetricDto> ByCampaign { get; init; } = [];
    public List<SegmentMetricDto> ByIndustry { get; init; } = [];
}

file sealed class SegmentMetricDto
{
    public string Key { get; init; } = string.Empty;
    public int TotalLeads { get; init; }
    public int WonLeads { get; init; }
}
