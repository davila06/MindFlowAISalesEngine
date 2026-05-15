namespace Api.Contracts.Analytics;

public class AnalyticsBacklogItemResponse
{
    public string Endpoint { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Priority { get; init; }
    public string EstimatedSprint { get; init; } = string.Empty;
}
