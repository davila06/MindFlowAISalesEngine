using System.Net;
using System.Net.Http.Json;

namespace Api.Tests;

public class ObservabilityDashboardFrontendEndpointTests
{
    private static HttpClient BuildClient() => new DashboardTestFactory().CreateClient();

    [Fact]
    public async Task LegacyObservabilityHtml_IsNotServed()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/observability.html");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LegacyDashboardAndAnalyticsHtml_AreNotServed()
    {
        using var client = BuildClient();

        var analyticsResponse = await client.GetAsync("/analytics-advanced.html");
        var dashboardResponse = await client.GetAsync("/dashboard.html");

        Assert.Equal(HttpStatusCode.NotFound, analyticsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, dashboardResponse.StatusCode);
    }

    [Fact]
    public async Task AlertThresholds_WithIsActiveFilter_ReturnsOnlyActive()
    {
        using var client = BuildClient();

        var createActive = await client.PostAsJsonAsync("/api/analytics/advanced/alert-thresholds", new
        {
            EndpointName = "overview",
            MaxErrorRatePercent = 10,
            MaxAverageLatencyMs = 500,
            NotificationEmail = "ops@novamind.local",
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, createActive.StatusCode);

        var createInactive = await client.PostAsJsonAsync("/api/analytics/advanced/alert-thresholds", new
        {
            EndpointName = "funnel",
            MaxErrorRatePercent = 10,
            MaxAverageLatencyMs = 500,
            NotificationEmail = "ops@novamind.local",
            IsActive = false
        });
        Assert.Equal(HttpStatusCode.Created, createInactive.StatusCode);

        var response = await client.GetAsync("/api/analytics/advanced/alert-thresholds?isActive=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AlertThresholdListDto>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Items);
        Assert.All(payload.Items, x => Assert.True(x.IsActive));
    }
}

file sealed class AlertThresholdListDto
{
    public List<AlertThresholdDto> Items { get; init; } = [];
}

file sealed class AlertThresholdDto
{
    public string Id { get; init; } = string.Empty;
    public string EndpointName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
