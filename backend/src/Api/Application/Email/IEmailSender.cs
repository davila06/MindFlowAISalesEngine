namespace Api.Application.Email;

public interface IEmailSender
{
    Task SendAsync(
        string host, int port, string username, string password, bool enableSsl,
        string fromEmail, string fromName,
        string toEmail, string subject, string bodyHtml,
    byte[]? attachmentBytes,
    string? attachmentFileName,
    string? attachmentContentType,
        CancellationToken cancellationToken);
}
