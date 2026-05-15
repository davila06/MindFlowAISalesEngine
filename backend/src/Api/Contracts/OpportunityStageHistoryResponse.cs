namespace Api.Contracts;

public class OpportunityStageHistoryResponse
{
    public Guid Id { get; init; }
    public Guid OpportunityId { get; init; }
    public Guid FromStageId { get; init; }
    public Guid ToStageId { get; init; }
    public string FromStageName { get; init; } = string.Empty;
    public string ToStageName { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public string Actor { get; init; } = string.Empty;
    public bool IsAutomated { get; init; }
    public DateTime ChangedAtUtc { get; init; }
}