namespace Api.Domain.Observability;

public sealed class ObservabilityEndpointAggregationState
{
    public string EndpointName { get; private set; } = string.Empty;
    public long LastRequestCount { get; private set; }
    public long LastSuccessCount { get; private set; }
    public long LastErrorCount { get; private set; }
    public DateTime LastRecordedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private ObservabilityEndpointAggregationState() { }

    public ObservabilityEndpointAggregationState(string endpointName)
    {
        EndpointName = endpointName;
        LastRecordedAtUtc = DateTime.MinValue;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Advance(long requestCount, long successCount, long errorCount, DateTime recordedAtUtc)
    {
        LastRequestCount = requestCount;
        LastSuccessCount = successCount;
        LastErrorCount = errorCount;
        LastRecordedAtUtc = recordedAtUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
