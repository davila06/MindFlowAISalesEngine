namespace Api.Contracts;

public class ContactUpdateRequest
{
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}