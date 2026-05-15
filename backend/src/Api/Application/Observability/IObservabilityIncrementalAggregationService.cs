namespace Api.Application.Observability;

public interface IObservabilityIncrementalAggregationService
{
    Task<ObservabilityIncrementalAggregationResult> RunAsync(
        int windowMinutes,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ObservabilityAggregateBatchResult>> QueryBatchesAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        string? endpointName,
        int windowMinutes,
        int top,
        CancellationToken cancellationToken = default);
}

public sealed class ObservabilityIncrementalAggregationResult
{
    public int ProcessedRecords { get; init; }
    public int UpsertedBatches { get; init; }
    public DateTime? LastProcessedRecordedAtUtc { get; init; }
}

public sealed class ObservabilityAggregateBatchResult
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
