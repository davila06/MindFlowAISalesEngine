namespace Api.Contracts;

public class PipelineBoardResponse
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public bool HasMore { get; init; }
    public List<PipelineStageResponse> Stages { get; init; } = [];
    public List<OpportunityResponse> Opportunities { get; init; } = [];
}