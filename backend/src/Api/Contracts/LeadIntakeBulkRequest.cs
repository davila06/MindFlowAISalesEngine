namespace Api.Contracts;

public class LeadIntakeBulkRequest
{
    public IReadOnlyList<LeadIntakeRequest> Items { get; init; } = [];
}

public class LeadIntakeBulkResponse
{
    public IReadOnlyList<LeadIntakeBulkSuccessItem> Accepted { get; init; } = [];
    public IReadOnlyList<LeadIntakeBulkFailureItem> Rejected { get; init; } = [];
}

public class LeadIntakeBulkSuccessItem
{
    public int Index { get; init; }
    public LeadIntakeResponse Lead { get; init; } = new();
}

public class LeadIntakeBulkFailureItem
{
    public int Index { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string FailedRequestId { get; init; } = string.Empty;
}
