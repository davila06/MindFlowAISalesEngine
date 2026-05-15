namespace Api.Contracts;

public class LeadIntakeFailedRecordResponse
{
    public string FailedRequestId { get; init; } = string.Empty;
    public LeadIntakeRequest Request { get; init; } = new();
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime FailedAtUtc { get; init; }
}
