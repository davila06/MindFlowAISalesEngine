namespace Api.Contracts;

public class PipelineStageResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Order { get; init; }
    public string? Color { get; init; }
    public int WipLimit { get; init; }
    public int OpportunityCount { get; init; }
    public bool IsOverWipLimit { get; init; }
}