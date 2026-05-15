namespace Api.Domain.Proposals;

public class ProposalTemplate
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string HtmlBody { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public bool IsCurrent { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ProposalTemplate() { }

    public ProposalTemplate(string name, string displayName, string htmlBody, int version, bool isCurrent)
    {
        Id = Guid.NewGuid();
        Name = name;
        DisplayName = displayName;
        HtmlBody = htmlBody;
        Version = version;
        IsCurrent = isCurrent;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkCurrent()
    {
        IsCurrent = true;
    }

    public void ClearCurrent()
    {
        IsCurrent = false;
    }
}
