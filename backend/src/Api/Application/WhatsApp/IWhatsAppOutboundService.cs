namespace Api.Application.WhatsApp;

/// <summary>Abstraction over the Meta Graph API for sending WhatsApp messages.</summary>
public interface IWhatsAppOutboundService
{
    /// <summary>
    /// Send a text message to a phone number.
    /// Returns the Meta-assigned message ID or null on failure.
    /// </summary>
    Task<string?> SendTextAsync(string toPhone, string body, CancellationToken cancellationToken);

    /// <summary>
    /// Send a template message.
    /// Returns the Meta-assigned message ID or null on failure.
    /// </summary>
    Task<string?> SendTemplateAsync(string toPhone, string templateName, IEnumerable<string> parameters, CancellationToken cancellationToken);
}
