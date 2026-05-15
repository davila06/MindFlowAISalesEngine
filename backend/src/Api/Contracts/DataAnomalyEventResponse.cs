namespace Api.Contracts;

public sealed class DataAnomalyEventResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Actor { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}