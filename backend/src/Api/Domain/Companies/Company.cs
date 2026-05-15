namespace Api.Domain.Companies;

public class Company
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public string Name { get; private set; }
    public string Industry { get; private set; }
    public string? Website { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Company()
    {
        Name = string.Empty;
        Industry = "unknown";
    }

    public Company(Guid leadId, string name, string? website, string? industry = null)
    {
        Id = Guid.NewGuid();
        LeadId = leadId;
        Name = name;
        Industry = string.IsNullOrWhiteSpace(industry) ? "unknown" : industry;
        Website = website;
        IsDeleted = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string name, string? website, string? industry = null)
    {
        Name = name;
        Website = website;
        Industry = string.IsNullOrWhiteSpace(industry) ? "unknown" : industry;
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