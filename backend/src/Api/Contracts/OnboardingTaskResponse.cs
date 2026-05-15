namespace Api.Contracts;

public class OnboardingTaskResponse
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<string> DependencyKeys { get; init; } = [];
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? DueAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
}
