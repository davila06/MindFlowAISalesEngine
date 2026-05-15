namespace Api.Contracts;

public class CompanyUpdateRequest
{
    public string? Name { get; init; }
    public string? Industry { get; init; }
    public string? Website { get; init; }
}