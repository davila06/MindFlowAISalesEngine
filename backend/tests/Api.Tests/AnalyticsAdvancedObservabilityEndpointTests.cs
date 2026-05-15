using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.AnalyticsAdvanced;

namespace Api.Tests;

public class AnalyticsAdvancedObservabilityEndpointTests
{
    private static HttpClient BuildClient() => new DashboardTestFactory().CreateClient();

    [Fact]
    public async Task MetricsEndpoint_ReturnsOkWithPayload()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/analytics/advanced/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AnalyticsMetricsPayloadDto>();
        Assert.NotNull(payload);
        Assert.NotNull(payload.Endpoints);
    }

    [Fact]
    public async Task MetricsEndpoint_AfterOverviewRequest_TracksOverviewCounters()
    {
        using var client = BuildClient();

        var overviewResponse = await client.GetAsync("/api/analytics/advanced/overview?groupBy=day");
        Assert.Equal(HttpStatusCode.OK, overviewResponse.StatusCode);

        var metricsResponse = await client.GetAsync("/api/analytics/advanced/metrics");
        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);

        var payload = await metricsResponse.Content.ReadFromJsonAsync<AnalyticsMetricsPayloadDto>();
        Assert.NotNull(payload);

        var overviewMetric = payload.Endpoints.FirstOrDefault(x => x.Endpoint == "overview");
        Assert.NotNull(overviewMetric);
        Assert.True(overviewMetric.RequestCount >= 1);
        Assert.True(overviewMetric.SuccessCount >= 1);
    }

    [Fact]
    public async Task MetricsEndpoint_AfterValidationError_TracksErrorCounter()
    {
        using var client = BuildClient();

        var invalidResponse = await client.GetAsync("/api/analytics/advanced/funnel?groupBy=quarter");
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        var metricsResponse = await client.GetAsync("/api/analytics/advanced/metrics");
        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);

        var payload = await metricsResponse.Content.ReadFromJsonAsync<AnalyticsMetricsPayloadDto>();
        Assert.NotNull(payload);

        var funnelMetric = payload.Endpoints.FirstOrDefault(x => x.Endpoint == "funnel");
        Assert.NotNull(funnelMetric);
        Assert.True(funnelMetric.ErrorCount >= 1);
    }

    [Fact]
    public async Task MetricsHistoryIncrementalAggregation_CreatesBatches_AndListsThem()
    {
        using var client = BuildClient();

        await client.GetAsync("/api/analytics/advanced/overview?groupBy=day");
        await client.GetAsync("/api/analytics/advanced/funnel?groupBy=day");
        await client.PostAsync("/api/analytics/advanced/metrics/history/snapshot", null);

        await client.GetAsync("/api/analytics/advanced/overview?groupBy=week");
        await client.PostAsync("/api/analytics/advanced/metrics/history/snapshot", null);

        var aggregateResponse = await client.PostAsync("/api/analytics/advanced/metrics/history/aggregate-incremental?windowMinutes=60&batchSize=100", null);
        Assert.Equal(HttpStatusCode.OK, aggregateResponse.StatusCode);

        var aggregate = await aggregateResponse.Content.ReadFromJsonAsync<ObservabilityIncrementalAggregationDto>();
        Assert.NotNull(aggregate);
        Assert.True(aggregate.ProcessedRecords >= 1);

        var listResponse = await client.GetAsync("/api/analytics/advanced/metrics/history/aggregates?windowMinutes=60");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var list = await listResponse.Content.ReadFromJsonAsync<ObservabilityAggregateBatchListDto>();
        Assert.NotNull(list);
        Assert.NotEmpty(list.Items);
        Assert.Contains(list.Items, x => x.EndpointName == "overview");
    }

    [Fact]
    public void ObservabilityCardinalityControl_NormalizesDynamicEndpoints_AndCapsSeries()
    {
        var service = new InMemoryAnalyticsObservabilityService(maxDistinctEndpoints: 2);

        service.TrackSuccess("/api/leads/123e4567-e89b-12d3-a456-426614174000?expand=contacts", 20);
        service.TrackSuccess("/api/leads/223e4567-e89b-12d3-a456-426614174111", 10);
        service.TrackSuccess("/api/pipeline/opportunities/987654", 30);
        service.TrackError("/api/rules/abc-123-long-dynamic-key", 40);

        var snapshot = service.GetSnapshot();

        Assert.Contains(snapshot.Endpoints, x => x.Endpoint == "/api/leads/{id}" && x.RequestCount == 2);
        Assert.Contains(snapshot.Endpoints, x => x.Endpoint == "__overflow__" && x.RequestCount >= 1);
        Assert.True(snapshot.Cardinality.DroppedDistinctEndpointCount >= 1);
        Assert.Equal(2, snapshot.Cardinality.MaxDistinctEndpoints);
    }
}

file sealed class AnalyticsMetricsPayloadDto
{
    public List<AnalyticsEndpointMetricDto> Endpoints { get; init; } = [];
    public AnalyticsCardinalityDto Cardinality { get; init; } = new();
}

file sealed class AnalyticsEndpointMetricDto
{
    public string Endpoint { get; init; } = string.Empty;
    public long RequestCount { get; init; }
    public long SuccessCount { get; init; }
    public long ErrorCount { get; init; }
    public decimal AverageLatencyMs { get; init; }
}

file sealed class AnalyticsCardinalityDto
{
    public int DistinctEndpoints { get; init; }
    public int MaxDistinctEndpoints { get; init; }
    public int DroppedDistinctEndpointCount { get; init; }
}

file sealed class ObservabilityIncrementalAggregationDto
{
    public int ProcessedRecords { get; init; }
    public int UpsertedBatches { get; init; }
    public DateTime? LastProcessedRecordedAtUtc { get; init; }
}

file sealed class ObservabilityAggregateBatchListDto
{
    public List<ObservabilityAggregateBatchDto> Items { get; init; } = [];
}

file sealed class ObservabilityAggregateBatchDto
{
    public string EndpointName { get; init; } = string.Empty;
    public DateTime WindowStartUtc { get; init; }
    public DateTime WindowEndUtc { get; init; }
    public long IncrementalRequestCount { get; init; }
    public long IncrementalSuccessCount { get; init; }
    public long IncrementalErrorCount { get; init; }
}
