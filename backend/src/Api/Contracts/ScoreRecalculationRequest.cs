namespace Api.Contracts;

public sealed class ScoreRecalculationRequest
{
    public DateTime? StartDateUtc { get; init; }
    public DateTime? EndDateUtc { get; init; }
}
