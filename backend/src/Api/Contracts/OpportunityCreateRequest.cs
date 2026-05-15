namespace Api.Contracts;

public class OpportunityCreateRequest
{
    public Guid LeadId { get; init; }
    public Guid StageId { get; init; }
    public string? Title { get; init; }
    public decimal Value { get; init; }
}