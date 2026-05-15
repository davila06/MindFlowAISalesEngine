namespace Api.Contracts;

public sealed class RuleDriftSummaryResponse
{
    public int TotalRules { get; init; }
    public int DraftRules { get; init; }
    public int RejectedRules { get; init; }
    public int NonProductionActiveRules { get; init; }
    public IReadOnlyList<RuleDriftEnvironmentResponse> ByEnvironment { get; init; } = [];
}

public sealed class RuleDriftEnvironmentResponse
{
    public string Environment { get; init; } = string.Empty;
    public int Count { get; init; }
}