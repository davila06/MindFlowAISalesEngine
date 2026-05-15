using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Api.Application.Email;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private const int MaxAttempts = 3;
    private const int RetryDelayMilliseconds = 500;
    private const int MaxAttachmentBytes = 5 * 1024 * 1024;

    private readonly ILogger<SmtpEmailSender> _logger;
    private static readonly HashSet<string> AllowedAttachmentContentTypes =
    [
        MediaTypeNames.Application.Pdf,
        MediaTypeNames.Application.Octet
    ];

    public SmtpEmailSender(ILogger<SmtpEmailSender> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(
        string host, int port, string username, string password, bool enableSsl,
        string fromEmail, string fromName,
        string toEmail, string subject, string bodyHtml,
        byte[]? attachmentBytes,
        string? attachmentFileName,
        string? attachmentContentType,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 10_000
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = bodyHtml,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                if (attachmentBytes is not null && attachmentBytes.Length > 0)
                {
                    if (attachmentBytes.Length > MaxAttachmentBytes)
                    {
                        throw new InvalidOperationException("Attachment exceeds the maximum allowed size.");
                    }

                    var stream = new MemoryStream(attachmentBytes, writable: false);
                    var contentType = string.IsNullOrWhiteSpace(attachmentContentType)
                        ? MediaTypeNames.Application.Octet
                        : attachmentContentType;

                    if (!AllowedAttachmentContentTypes.Contains(contentType))
                    {
                        throw new InvalidOperationException("Attachment content type is not allowed.");
                    }

                    var attachment = new Attachment(stream, attachmentFileName ?? "attachment.bin", contentType);
                    message.Attachments.Add(attachment);
                }

                _logger.LogDebug(
                    "Sending SMTP email to {ToEmail} via {Host}:{Port}. Attempt {Attempt}.",
                    toEmail,
                    host,
                    port,
                    attempt);

                await client.SendMailAsync(message, cancellationToken);
                return;
            }
            catch (SmtpException) when (attempt < MaxAttempts)
            {
                await Task.Delay(RetryDelayMilliseconds * attempt, cancellationToken);
            }
            catch (TimeoutException) when (attempt < MaxAttempts)
            {
                await Task.Delay(RetryDelayMilliseconds * attempt, cancellationToken);
            }
        }
    }
}
