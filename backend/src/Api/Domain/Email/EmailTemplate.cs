namespace Api.Domain.Email;

public class EmailTemplate
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string BodyHtml { get; private set; } = string.Empty;
    public string RequiredVariablesSerialized { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsCurrent { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private EmailTemplate() { }

    public EmailTemplate(string name, string subject, string bodyHtml)
        : this(name, 1, subject, bodyHtml, Array.Empty<string>())
    {
    }

    public EmailTemplate(string name, int version, string subject, string bodyHtml, IEnumerable<string>? requiredVariables)
    {
        Id = Guid.NewGuid();
        Name = name;
        Version = version;
        Subject = subject;
        BodyHtml = bodyHtml;
        RequiredVariablesSerialized = SerializeVariables(requiredVariables);
        IsActive = true;
        IsCurrent = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public IReadOnlyList<string> GetRequiredVariables()
    {
        return RequiredVariablesSerialized
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    public void MarkAsHistorical()
    {
        IsCurrent = false;
    }

    public void MarkAsCurrent()
    {
        IsCurrent = true;
    }

    public static string SerializeVariables(IEnumerable<string>? requiredVariables)
    {
        return string.Join(
            '|',
            (requiredVariables ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    }
}
