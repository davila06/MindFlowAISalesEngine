namespace Api.Contracts.Analytics;

public class RevenueKpiResponse
{
    public decimal WonRevenue { get; init; }
    public decimal PipelineRevenue { get; init; }
    public decimal AverageDealSize { get; init; }
    public string Currency { get; init; } = "USD";
}
