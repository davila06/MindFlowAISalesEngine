namespace Api.Contracts;

public sealed class EmailLogResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string? CorrelationId { get; init; }
    public string? ToEmail { get; init; }
    public string? Subject { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime SentAtUtc { get; init; }
    // Tracking
    public int OpenCount { get; init; }
    public int ClickCount { get; init; }
    public DateTime? FirstOpenedAtUtc { get; init; }
    public DateTime? FirstClickedAtUtc { get; init; }
    public bool IsOpened => OpenCount > 0;
    public bool IsClicked => ClickCount > 0;
}

public sealed class EmailTrackingMetricsResponse
{
    public string TemplateName { get; init; } = string.Empty;
    public int TotalSent { get; init; }
    public int TotalOpened { get; init; }
    public int TotalClicked { get; init; }
    public double OpenRatePercent { get; init; }
    public double ClickRatePercent { get; init; }
    public double ClickToOpenRatePercent { get; init; }
}
