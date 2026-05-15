namespace Api.Contracts;

public class OpportunityResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public Guid StageId { get; init; }
    public Guid? OwnerUserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string LeadSource { get; init; } = string.Empty;
    public int LeadScore { get; init; }
    public string RiskLabel { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public string VersionToken { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}