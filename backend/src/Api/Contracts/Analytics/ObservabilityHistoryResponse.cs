namespace Api.Contracts.Analytics;

public sealed class ObservabilityHistoryResponse
{
    public IReadOnlyList<ObservabilityMetricRecordResponse> Records { get; init; } = [];
}

public sealed class ObservabilityMetricRecordResponse
{
    public string Id { get; init; } = string.Empty;
    public string EndpointName { get; init; } = string.Empty;
    public long RequestCount { get; init; }
    public long SuccessCount { get; init; }
    public long ErrorCount { get; init; }
    public decimal AverageLatencyMs { get; init; }
    public DateTime RecordedAtUtc { get; init; }
}

public sealed class ObservabilityIncrementalAggregationResponse
{
    public int ProcessedRecords { get; init; }
    public int UpsertedBatches { get; init; }
    public DateTime? LastProcessedRecordedAtUtc { get; init; }
}

public sealed class ObservabilityAggregateBatchListResponse
{
    public IReadOnlyList<ObservabilityAggregateBatchResponse> Items { get; init; } = [];
}

public sealed class ObservabilityAggregateBatchResponse
{
    public string EndpointName { get; init; } = string.Empty;
    public DateTime WindowStartUtc { get; init; }
    public DateTime WindowEndUtc { get; init; }
    public long IncrementalRequestCount { get; init; }
    public long IncrementalSuccessCount { get; init; }
    public long IncrementalErrorCount { get; init; }
    public decimal AverageLatencyMs { get; init; }
    public long SampleCount { get; init; }
    public DateTime LastSourceRecordedAtUtc { get; init; }
}
