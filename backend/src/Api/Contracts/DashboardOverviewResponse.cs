namespace Api.Contracts;

public class DashboardOverviewResponse
{
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public int TotalLeads { get; init; }
    public int TotalOpportunities { get; init; }
    public int WonOpportunities { get; init; }
    public decimal ConversionRate { get; init; }
    public decimal PipelineValue { get; init; }
    public List<LeadsPerDayPointResponse> LeadsPerDay { get; init; } = [];
}
