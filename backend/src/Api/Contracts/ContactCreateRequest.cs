namespace Api.Contracts;

public class ContactCreateRequest
{
    public Guid LeadId { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}