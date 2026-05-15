namespace Api.Contracts;

public class LeadAssignmentResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public Guid UserId { get; init; }
    public string Strategy { get; init; } = string.Empty;
    public string? RuleKey { get; init; }
    public DateTime AssignedAtUtc { get; init; }
}
