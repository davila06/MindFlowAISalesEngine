namespace Api.Contracts;

public class CompanyResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Industry { get; init; } = "unknown";
    public string? Website { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}