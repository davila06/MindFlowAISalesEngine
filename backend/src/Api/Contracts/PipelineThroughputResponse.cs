namespace Api.Contracts;

public class PipelineThroughputResponse
{
    public DateTime? StartDateUtc { get; init; }
    public DateTime? EndDateUtc { get; init; }
    public IReadOnlyList<PipelineStageThroughputItemResponse> Items { get; init; } = [];
}

public class PipelineStageThroughputItemResponse
{
    public Guid StageId { get; init; }
    public string StageName { get; init; } = string.Empty;
    public int EnteredCount { get; init; }
    public int ExitedCount { get; init; }
    public int NetFlow => EnteredCount - ExitedCount;
}
