namespace Api.Application.Email;

public interface IEmailService
{
    Task SendLeadWelcomeAsync(Guid leadId, string? toEmail, CancellationToken cancellationToken);
    Task SendLeadFollowUpAsync(Guid leadId, string? toEmail, CancellationToken cancellationToken);
    Task<bool> SendProposalAsync(
        Guid leadId,
        string? toEmail,
        string recipientName,
        string proposalTitle,
        decimal amount,
        string currency,
        string trackingUrl,
        byte[] pdfBytes,
        string pdfFileName,
        CancellationToken cancellationToken);
    Task<bool> SendProposalReminderAsync(
        Guid leadId,
        string? toEmail,
        string recipientName,
        string proposalTitle,
        string trackingUrl,
        CancellationToken cancellationToken);
    Task<bool> SendCustomerWelcomeAsync(
        Guid leadId,
        string? toEmail,
        string trackingUrl,
        CancellationToken cancellationToken);
    Task<bool> SendAnalyticsDegradationAlertAsync(
        string toEmail,
        string endpointName,
        string metricName,
        decimal observedValue,
        decimal thresholdValue,
        DateTime triggeredAtUtc,
        CancellationToken cancellationToken);
}
