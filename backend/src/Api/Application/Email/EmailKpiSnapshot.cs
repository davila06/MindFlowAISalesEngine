namespace Api.Application.Email;

public sealed class EmailKpiSnapshot
{
    public int TotalCount { get; init; }
    public int SentCount { get; init; }
    public int FailedCount { get; init; }
    public int QueuedCount { get; init; }
    public int BouncedCount { get; init; }
    public IReadOnlyList<EmailChannelKpiSnapshot> ByChannel { get; init; } = Array.Empty<EmailChannelKpiSnapshot>();
    public decimal ErrorRatePercent { get; init; }
}

public sealed class EmailChannelKpiSnapshot
{
    public string Channel { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public int SentCount { get; init; }
    public int FailedCount { get; init; }
    public int QueuedCount { get; init; }
    public int BouncedCount { get; init; }
    public decimal ErrorRatePercent { get; init; }
}