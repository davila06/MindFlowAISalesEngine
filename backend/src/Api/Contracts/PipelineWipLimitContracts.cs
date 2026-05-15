using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class PipelineWipLimitResponse
{
    public Guid StageId { get; init; }
    public string StageName { get; init; } = string.Empty;
    public int Limit { get; init; }
}

public class PipelineWipLimitUpdateRequest
{
    [Range(0, 100000)]
    public int Limit { get; init; }
}
