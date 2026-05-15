namespace Api.Domain.Observability;

public sealed class ObservabilityAggregateBatch
{
    public Guid Id { get; private set; }
    public string EndpointName { get; private set; } = string.Empty;
    public DateTime WindowStartUtc { get; private set; }
    public DateTime WindowEndUtc { get; private set; }
    public long IncrementalRequestCount { get; private set; }
    public long IncrementalSuccessCount { get; private set; }
    public long IncrementalErrorCount { get; private set; }
    public decimal TotalLatencyMs { get; private set; }
    public long SampleCount { get; private set; }
    public DateTime LastSourceRecordedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private ObservabilityAggregateBatch() { }

    public ObservabilityAggregateBatch(
        string endpointName,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        DateTime lastSourceRecordedAtUtc)
    {
        Id = Guid.NewGuid();
        EndpointName = endpointName;
        WindowStartUtc = windowStartUtc;
        WindowEndUtc = windowEndUtc;
        LastSourceRecordedAtUtc = lastSourceRecordedAtUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Accumulate(long requestDelta, long successDelta, long errorDelta, decimal averageLatencyMs, DateTime lastSourceRecordedAtUtc)
    {
        IncrementalRequestCount += Math.Max(requestDelta, 0);
        IncrementalSuccessCount += Math.Max(successDelta, 0);
        IncrementalErrorCount += Math.Max(errorDelta, 0);
        TotalLatencyMs += Math.Max(averageLatencyMs, 0);
        SampleCount += 1;

        if (lastSourceRecordedAtUtc > LastSourceRecordedAtUtc)
        {
            LastSourceRecordedAtUtc = lastSourceRecordedAtUtc;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }
}
