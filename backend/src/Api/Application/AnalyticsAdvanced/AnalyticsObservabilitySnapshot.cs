namespace Api.Application.AnalyticsAdvanced;

public sealed class AnalyticsObservabilitySnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public IReadOnlyList<AnalyticsEndpointMetricSnapshot> Endpoints { get; init; } = [];
    public AnalyticsObservabilityCardinalitySnapshot Cardinality { get; init; } = new();
}

public sealed class AnalyticsObservabilityCardinalitySnapshot
{
    public int DistinctEndpoints { get; init; }
    public int MaxDistinctEndpoints { get; init; }
    public int DroppedDistinctEndpointCount { get; init; }
}

public sealed class AnalyticsEndpointMetricSnapshot
{
    public string Endpoint { get; init; } = string.Empty;
    public long RequestCount { get; init; }
    public long SuccessCount { get; init; }
    public long ErrorCount { get; init; }
    public decimal AverageLatencyMs { get; init; }
}
