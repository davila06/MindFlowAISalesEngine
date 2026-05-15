namespace Api.Contracts;

public sealed class EmailKpiResponse
{
    public int TotalCount { get; init; }
    public int SentCount { get; init; }
    public int FailedCount { get; init; }
    public int QueuedCount { get; init; }
    public int BouncedCount { get; init; }
    public IReadOnlyList<EmailChannelKpiItemResponse> ByChannel { get; init; } = Array.Empty<EmailChannelKpiItemResponse>();
    public decimal ErrorRatePercent { get; init; }
}

public sealed class EmailChannelKpiItemResponse
{
    public string Channel { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public int SentCount { get; init; }
    public int FailedCount { get; init; }
    public int QueuedCount { get; init; }
    public int BouncedCount { get; init; }
    public decimal ErrorRatePercent { get; init; }
}