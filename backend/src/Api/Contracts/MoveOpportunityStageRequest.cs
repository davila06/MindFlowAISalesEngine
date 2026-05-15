namespace Api.Contracts;

public class MoveOpportunityStageRequest
{
    public Guid TargetStageId { get; init; }
    public string? Reason { get; init; }
    public string? Actor { get; init; }
    public string? ExpectedVersionToken { get; init; }
}