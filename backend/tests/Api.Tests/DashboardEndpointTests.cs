using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public sealed class DashboardTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"dashboard_test_{Guid.NewGuid():N}.db";

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
            try
            {
                File.Delete(_dbPath);
            }
            catch (IOException)
            {
                // Ignore transient SQLite file locks during test teardown.
            }
        }
    }
}

public class DashboardEndpointTests
{
    private static readonly Guid WonStageId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid NewStageId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static HttpClient BuildClient() => new DashboardTestFactory().CreateClient();

    [Fact]
    public async Task GetOverview_WithNoData_ReturnsZeroMetrics()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/dashboard/overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var overview = await response.Content.ReadFromJsonAsync<DashboardOverviewDto>();
        Assert.NotNull(overview);
        Assert.Equal(0, overview.TotalLeads);
        Assert.Equal(0m, overview.ConversionRate);
        Assert.Equal(0m, overview.PipelineValue);
        Assert.NotEmpty(overview.LeadsPerDay);
    }

    [Fact]
    public async Task GetOverview_WithLeadAndOpportunities_ReturnsExpectedMetrics()
    {
        using var client = BuildClient();

        var lead1Id = await CreateLead(client, "referral");
        var lead2Id = await CreateLead(client, "web");

        await CreateOpportunity(client, lead1Id, WonStageId, 1000m, "Deal Won");
        await CreateOpportunity(client, lead2Id, NewStageId, 2000m, "Deal Open");

        var response = await client.GetAsync("/api/dashboard/overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var overview = await response.Content.ReadFromJsonAsync<DashboardOverviewDto>();
        Assert.NotNull(overview);
        Assert.Equal(2, overview.TotalLeads);
        Assert.Equal(3000m, overview.PipelineValue);
        Assert.Equal(50m, overview.ConversionRate);
    }

    [Fact]
    public async Task GetOverview_LeadsPerDay_IncludesTodayCount()
    {
        using var client = BuildClient();

        await CreateLead(client, "ads");
        await CreateLead(client, "unknown");

        var response = await client.GetAsync("/api/dashboard/overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var overview = await response.Content.ReadFromJsonAsync<DashboardOverviewDto>();
        Assert.NotNull(overview);

        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        var todayBucket = overview.LeadsPerDay.FirstOrDefault(x => x.Date == today);
        Assert.NotNull(todayBucket);
        Assert.True(todayBucket.Count >= 2);
    }

    [Fact]
    public async Task GetOverviewCsv_ReturnsCsvFile()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/dashboard/overview/csv?days=7");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Metric,Value", csv);
        Assert.Contains("LeadsPerDayDate,LeadsPerDayCount", csv);
        Assert.Contains("TotalLeads", csv);
    }

    [Fact]
    public async Task LegacyDashboardHtml_IsNotServed()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/dashboard.html");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDataQuality_WithDuplicateLeadCapture_TracksAnomalyEvents()
    {
        using var client = BuildClient();

        var duplicateEmail = $"dup_{Guid.NewGuid():N}@mindflow.test";

        var firstResponse = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = duplicateEmail,
            Phone = BuildUniquePhone(),
            Source = "quality-test"
        });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = duplicateEmail,
            Phone = BuildUniquePhone(),
            Source = "quality-test"
        });
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        var response = await client.GetAsync("/api/dashboard/data-quality");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var overview = await response.Content.ReadFromJsonAsync<DataQualityOverviewDto>();
        Assert.NotNull(overview);
        Assert.True(overview.DataAnomalyEvents >= 1);
    }

    [Fact]
    public async Task GetDataQualityAnomalies_FiltersByWindowAndType()
    {
        using var client = BuildClient();

        var duplicateEmail = $"history_{Guid.NewGuid():N}@mindflow.test";
        await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = duplicateEmail,
            Phone = BuildUniquePhone(),
            Source = "anomaly-history"
        });

        await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = duplicateEmail,
            Phone = BuildUniquePhone(),
            Source = "anomaly-history"
        });

        var startUtc = DateTime.UtcNow.AddMinutes(-5).ToString("O");
        var endUtc = DateTime.UtcNow.AddMinutes(5).ToString("O");
        var response = await client.GetAsync($"/api/dashboard/data-quality/anomalies?eventType=duplicate_candidate&startUtc={Uri.EscapeDataString(startUtc)}&endUtc={Uri.EscapeDataString(endUtc)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var anomalies = await response.Content.ReadFromJsonAsync<List<DataAnomalyEventDto>>();
        Assert.NotNull(anomalies);
        Assert.NotEmpty(anomalies);
        Assert.All(anomalies, x => Assert.Contains("duplicate_candidate", x.EventType));
    }

    private static async Task<Guid> CreateLead(HttpClient client, string source)
    {
        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"dash_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = source
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

    private static string BuildUniquePhone()
    {
        var n = Math.Abs(Guid.NewGuid().GetHashCode()) % 10_000_000;
        return $"+1555{n:D7}";
    }
}

file sealed class DashboardOverviewDto
{
    public int TotalLeads { get; init; }
    public decimal ConversionRate { get; init; }
    public decimal PipelineValue { get; init; }
    public List<LeadPerDayDto> LeadsPerDay { get; init; } = [];
}

file sealed class LeadPerDayDto
{
    public string Date { get; init; } = string.Empty;
    public int Count { get; init; }
}

file sealed class LeadIntakeDto
{
    public Guid Id { get; init; }
}

file sealed class DataQualityOverviewDto
{
    public int TotalLeads { get; init; }
    public int LeadsWithEmail { get; init; }
    public int LeadsWithPhone { get; init; }
    public int LeadsWithBothContacts { get; init; }
    public int DuplicateEmailCandidates { get; init; }
    public int DuplicatePhoneCandidates { get; init; }
    public decimal ContactCompletenessPercent { get; init; }
    public int DataAnomalyEvents { get; init; }
}

file sealed class DataAnomalyEventDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Actor { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
