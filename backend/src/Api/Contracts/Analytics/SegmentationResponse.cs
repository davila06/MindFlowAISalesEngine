namespace Api.Contracts.Analytics;

public sealed class SegmentationResponse
{
    public IReadOnlyList<SegmentMetricResponse> BySource { get; init; } = [];
    public IReadOnlyList<SegmentMetricResponse> ByCampaign { get; init; } = [];
    public IReadOnlyList<SegmentMetricResponse> ByIndustry { get; init; } = [];
}

public sealed class SegmentMetricResponse
{
    public string Key { get; init; } = string.Empty;
    public int TotalLeads { get; init; }
    public int WonLeads { get; init; }
    public decimal ConversionRate { get; init; }
    public decimal PipelineRevenue { get; init; }
    public decimal WonRevenue { get; init; }
}