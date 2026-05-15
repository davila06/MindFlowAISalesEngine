namespace Api.Contracts;

public class ContactResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}