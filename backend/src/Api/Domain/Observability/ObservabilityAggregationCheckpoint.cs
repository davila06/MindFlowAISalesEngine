namespace Api.Domain.Observability;

public sealed class ObservabilityAggregationCheckpoint
{
    public string Key { get; private set; } = string.Empty;
    public DateTime? LastProcessedRecordedAtUtc { get; private set; }
    public string? LastProcessedRecordId { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private ObservabilityAggregationCheckpoint() { }

    public ObservabilityAggregationCheckpoint(string key)
    {
        Key = key;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Advance(DateTime recordedAtUtc, string recordId)
    {
        LastProcessedRecordedAtUtc = recordedAtUtc;
        LastProcessedRecordId = recordId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
