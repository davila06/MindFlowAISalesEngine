namespace Api.Contracts;

public class PipelineStageSlaAlertResponse
{
    public int TotalOpportunitiesEvaluated { get; init; }
    public int TotalBreaches { get; init; }
    public IReadOnlyList<PipelineStageSlaAlertItemResponse> Items { get; init; } = [];
}

public class PipelineStageSlaAlertItemResponse
{
    public Guid OpportunityId { get; init; }
    public Guid LeadId { get; init; }
    public Guid StageId { get; init; }
    public string StageName { get; init; } = string.Empty;
    public int MinutesInStage { get; init; }
    public int SlaMinutes { get; init; }
    public bool IsBreached { get; init; }
    public int ExceededByMinutes { get; init; }
    public string Severity { get; init; } = string.Empty;
}
