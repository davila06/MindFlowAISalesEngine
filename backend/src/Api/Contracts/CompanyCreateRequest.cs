namespace Api.Contracts;

public class CompanyCreateRequest
{
    public Guid LeadId { get; init; }
    public string? Name { get; init; }
    public string? Industry { get; init; }
    public string? Website { get; init; }
}