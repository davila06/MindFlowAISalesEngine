namespace Api.Domain.Contacts;

public class Contact
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public string? FullName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Contact()
    {
    }

    public Contact(Guid leadId, string? fullName, string? email, string? phone)
    {
        Id = Guid.NewGuid();
        LeadId = leadId;
        FullName = fullName;
        Email = email;
        Phone = phone;
        IsDeleted = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string? fullName, string? email, string? phone)
    {
        FullName = fullName;
        Email = email;
        Phone = phone;
    }

    public void MarkDeleted()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
    }
}