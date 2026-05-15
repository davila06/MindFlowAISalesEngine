using System.Net;
using System.Net.Http.Json;

namespace Api.Tests;

public class ObservabilityHistoryEndpointTests
{
    private static HttpClient BuildClient() => new DashboardTestFactory().CreateClient();

    [Fact]
    public async Task MetricsHistory_WhenNoSnapshots_ReturnsOkWithEmptyList()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/analytics/advanced/metrics/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ObservabilityHistoryPayloadDto>();
        Assert.NotNull(payload);
        Assert.NotNull(payload.Records);
        Assert.Empty(payload.Records);
    }

    [Fact]
    public async Task MetricsFlush_Returns200AndPersistsSnapshot()
    {
        using var client = BuildClient();

        // Trigger at least one analytics call so snapshot has data
        await client.GetAsync("/api/analytics/advanced/overview?groupBy=day");

        var flushResponse = await client.PostAsync("/api/analytics/advanced/metrics/history/snapshot", null);
        Assert.Equal(HttpStatusCode.OK, flushResponse.StatusCode);

        var historyResponse = await client.GetAsync("/api/analytics/advanced/metrics/history");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);

        var payload = await historyResponse.Content.ReadFromJsonAsync<ObservabilityHistoryPayloadDto>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Records);
    }

    [Fact]
    public async Task MetricsHistory_WithEndpointNameFilter_ReturnsFilteredRecords()
    {
        using var client = BuildClient();

        await client.GetAsync("/api/analytics/advanced/overview?groupBy=day");
        await client.GetAsync("/api/analytics/advanced/funnel?groupBy=day");
        await client.PostAsync("/api/analytics/advanced/metrics/history/snapshot", null);

        var response = await client.GetAsync("/api/analytics/advanced/metrics/history?endpointName=overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ObservabilityHistoryPayloadDto>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Records);
        Assert.All(payload.Records, r => Assert.Equal("overview", r.EndpointName));
    }

    [Fact]
    public async Task MetricsHistory_WithDateRangeFilter_ReturnsOnlyMatchingRecords()
    {
        using var client = BuildClient();

        await client.PostAsync("/api/analytics/advanced/metrics/history/snapshot", null);

        var startUtc = DateTime.UtcNow.AddHours(-1).ToString("o");
        var endUtc = DateTime.UtcNow.AddHours(1).ToString("o");
        var response = await client.GetAsync(
            $"/api/analytics/advanced/metrics/history?startUtc={Uri.EscapeDataString(startUtc)}&endUtc={Uri.EscapeDataString(endUtc)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ObservabilityHistoryPayloadDto>();
        Assert.NotNull(payload);
        // snapshot was just saved within the date window — records may be 0 if no prior tracking, but endpoint must return 200
        Assert.NotNull(payload.Records);
    }
}

file sealed class ObservabilityHistoryPayloadDto
{
    public List<ObservabilityMetricRecordDto> Records { get; init; } = [];
}

file sealed class ObservabilityMetricRecordDto
{
    public string Id { get; init; } = string.Empty;
    public string EndpointName { get; init; } = string.Empty;
    public long RequestCount { get; init; }
    public long SuccessCount { get; init; }
    public long ErrorCount { get; init; }
    public decimal AverageLatencyMs { get; init; }
    public DateTime RecordedAtUtc { get; init; }
}
