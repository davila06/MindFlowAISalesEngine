namespace Api.Domain.WhatsApp;

/// <summary>A WhatsApp contact (opted-in phone number) linked to a lead.</summary>
public class WhatsAppContact
{
    public Guid Id { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public bool OptedIn { get; private set; }
    public DateTime? OptedInAtUtc { get; private set; }
    public DateTime? OptedOutAtUtc { get; private set; }
    /// <summary>Associated lead (null until linked via intake).</summary>
    public Guid? LeadId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private WhatsAppContact() { }

    public static WhatsAppContact Create(string phoneNumber, string? displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        return new WhatsAppContact
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber,
            DisplayName = displayName,
            OptedIn = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void OptIn()
    {
        OptedIn = true;
        OptedInAtUtc = DateTime.UtcNow;
        OptedOutAtUtc = null;
    }

    public void OptOut()
    {
        OptedIn = false;
        OptedOutAtUtc = DateTime.UtcNow;
    }

    public void LinkToLead(Guid leadId) => LeadId = leadId;
}
