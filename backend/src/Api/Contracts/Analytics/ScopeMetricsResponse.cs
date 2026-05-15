namespace Api.Contracts.Analytics;

public sealed class ScopeMetricsResponse
{
    public TenantScopeMetricResponse Tenant { get; init; } = new();
    public IReadOnlyList<SellerScopeMetricResponse> Sellers { get; init; } = [];
    public IReadOnlyList<TeamScopeMetricResponse> Teams { get; init; } = [];
}

public sealed class TenantScopeMetricResponse
{
    public string TenantId { get; init; } = string.Empty;
    public int TotalLeads { get; init; }
    public int AssignedLeadsCount { get; init; }
    public int WonLeadsCount { get; init; }
    public decimal ConversionRate { get; init; }
    public decimal PipelineRevenue { get; init; }
    public decimal WonRevenue { get; init; }
}

public sealed class SellerScopeMetricResponse
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int AssignedLeadsCount { get; init; }
    public int WonLeadsCount { get; init; }
    public decimal ConversionRate { get; init; }
    public decimal PipelineRevenue { get; init; }
    public decimal WonRevenue { get; init; }
}

public sealed class TeamScopeMetricResponse
{
    public string TeamKey { get; init; } = string.Empty;
    public int AssignedLeadsCount { get; init; }
    public int WonLeadsCount { get; init; }
    public decimal ConversionRate { get; init; }
    public decimal PipelineRevenue { get; init; }
    public decimal WonRevenue { get; init; }
}