namespace Api.Application.Proposals;

public interface IProposalPdfGenerator
{
    byte[] Generate(
        string title,
        decimal amount,
        string currency,
        string recipientName,
        DateTime createdAtUtc,
        string trackingUrl);
}
