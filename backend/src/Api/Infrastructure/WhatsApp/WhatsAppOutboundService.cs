using Api.Application.WhatsApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Infrastructure.WhatsApp;

/// <summary>
/// Sends messages via Meta WhatsApp Business Cloud API (Graph API v18).
/// Configure WHATSAPP_PHONE_NUMBER_ID and WHATSAPP_ACCESS_TOKEN in appsettings / env.
/// </summary>
public class WhatsAppOutboundService : IWhatsAppOutboundService
{
    private readonly HttpClient _httpClient;
    private readonly string? _phoneNumberId;
    private readonly ILogger<WhatsAppOutboundService> _logger;

    public WhatsAppOutboundService(HttpClient httpClient, IConfiguration configuration, ILogger<WhatsAppOutboundService> logger)
    {
        _httpClient = httpClient;
        _phoneNumberId = configuration["WhatsApp:PhoneNumberId"];
        var token = configuration["WhatsApp:AccessToken"];
        if (!string.IsNullOrWhiteSpace(token))
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        _httpClient.BaseAddress = new Uri("https://graph.facebook.com/v18.0/");
        _logger = logger;
    }

    public async Task<string?> SendTextAsync(string toPhone, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_phoneNumberId))
        {
            _logger.LogWarning("WhatsApp PhoneNumberId not configured. Skipping outbound send.");
            return null;
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to = toPhone,
            type = "text",
            text = new { body }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_phoneNumberId}/messages", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("WhatsApp API returned {Status} for {Phone}.", response.StatusCode, toPhone);
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return doc.RootElement
                .GetProperty("messages")[0]
                .GetProperty("id")
                .GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp text to {Phone}.", toPhone);
            return null;
        }
    }

    public async Task<string?> SendTemplateAsync(string toPhone, string templateName, IEnumerable<string> parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_phoneNumberId))
        {
            _logger.LogWarning("WhatsApp PhoneNumberId not configured. Skipping template send.");
            return null;
        }

        var components = new[]
        {
            new
            {
                type = "body",
                parameters = parameters.Select(p => new { type = "text", text = p }).ToArray()
            }
        };

        var payload = new
        {
            messaging_product = "whatsapp",
            to = toPhone,
            type = "template",
            template = new { name = templateName, language = new { code = "en_US" }, components }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_phoneNumberId}/messages", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("WhatsApp API returned {Status} for template {Template} to {Phone}.",
                    response.StatusCode, templateName, toPhone);
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return doc.RootElement
                .GetProperty("messages")[0]
                .GetProperty("id")
                .GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp template '{Template}' to {Phone}.", templateName, toPhone);
            return null;
        }
    }
}
