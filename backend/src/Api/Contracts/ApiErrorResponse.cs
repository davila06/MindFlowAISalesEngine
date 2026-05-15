namespace Api.Contracts;

public sealed class ApiErrorResponse
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string TraceId { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; init; }
}
