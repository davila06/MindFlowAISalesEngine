namespace Api.Domain.Observability;

public sealed class ObservabilityMetricRecord
{
    public string Id { get; private set; } = string.Empty;
    public string EndpointName { get; private set; } = string.Empty;
    public long RequestCount { get; private set; }
    public long SuccessCount { get; private set; }
    public long ErrorCount { get; private set; }
    public decimal AverageLatencyMs { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }

    private ObservabilityMetricRecord() { }

    public ObservabilityMetricRecord(
        string endpointName,
        long requestCount,
        long successCount,
        long errorCount,
        decimal averageLatencyMs,
        DateTime recordedAtUtc)
    {
        Id = Guid.NewGuid().ToString();
        EndpointName = endpointName;
        RequestCount = requestCount;
        SuccessCount = successCount;
        ErrorCount = errorCount;
        AverageLatencyMs = averageLatencyMs;
        RecordedAtUtc = recordedAtUtc;
    }
}
