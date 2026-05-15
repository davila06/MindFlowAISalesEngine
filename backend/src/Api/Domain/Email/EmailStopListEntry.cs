namespace Api.Domain.Email;

public sealed class EmailStopListEntry
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private EmailStopListEntry() { }

    public EmailStopListEntry(string email, string reason)
    {
        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        Reason = string.IsNullOrWhiteSpace(reason) ? "unsubscribe" : reason.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }
}