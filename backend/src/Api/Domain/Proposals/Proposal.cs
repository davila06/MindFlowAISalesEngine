namespace Api.Domain.Proposals;

public class Proposal
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string? RecipientName { get; private set; }
    public string? RecipientEmail { get; private set; }
    public string TemplateName { get; private set; } = string.Empty;
    public int TemplateVersion { get; private set; }
    public string PdfFileName { get; private set; } = string.Empty;
    public byte[] PdfContent { get; private set; } = [];
    public string TrackingToken { get; private set; } = string.Empty;
    public int ViewCount { get; private set; }
    public DateTime? LastViewedAtUtc { get; private set; }
    public string Status { get; private set; } = ProposalStatus.Draft;
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime? SignedAtUtc { get; private set; }
    public string? SignedByName { get; private set; }
    public string? SignedByEmail { get; private set; }
    public Guid? RenewedFromProposalId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }

    private Proposal() { }

    public Proposal(
        Guid leadId,
        string title,
        decimal amount,
        string currency,
        string? recipientName,
        string? recipientEmail,
        string templateName,
        int templateVersion,
        string pdfFileName,
        byte[] pdfContent,
        Guid? renewedFromProposalId = null)
    {
        Id = Guid.NewGuid();
        LeadId = leadId;
        Title = title;
        Amount = amount;
        Currency = currency;
        RecipientName = recipientName;
        RecipientEmail = recipientEmail;
        TemplateName = templateName;
        TemplateVersion = templateVersion;
        PdfFileName = pdfFileName;
        PdfContent = pdfContent;
        TrackingToken = Guid.NewGuid().ToString("N");
        Status = ProposalStatus.Draft;
        RenewedFromProposalId = renewedFromProposalId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkSent(int expiryDays = 14)
    {
        Status = ProposalStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = DateTime.UtcNow.AddDays(expiryDays);
    }

    public void TrackView()
    {
        ViewCount++;
        LastViewedAtUtc = DateTime.UtcNow;
        if (Status == ProposalStatus.Sent)
        {
            Status = ProposalStatus.Viewed;
        }
    }

    public void Sign(string signerName, string signerEmail)
    {
        SignedByName = signerName;
        SignedByEmail = signerEmail;
        SignedAtUtc = DateTime.UtcNow;
        Status = ProposalStatus.Signed;
    }

    public void Expire()
    {
        Status = ProposalStatus.Expired;
        ExpiresAtUtc = DateTime.UtcNow;
    }

    public void MarkRenewed()
    {
        Status = ProposalStatus.Renewed;
    }

    public void RescheduleReminderWindow(int additionalDays)
    {
        ExpiresAtUtc = (ExpiresAtUtc ?? DateTime.UtcNow).AddDays(additionalDays);
    }
}
