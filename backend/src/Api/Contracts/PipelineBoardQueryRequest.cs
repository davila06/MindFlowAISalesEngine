namespace Api.Contracts;

public class PipelineBoardQueryRequest
{
    public Guid? OwnerUserId { get; init; }
    public string? Source { get; init; }
    public int? MinScore { get; init; }
    public int? MaxScore { get; init; }
    public string? RiskLabel { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 100;
}
