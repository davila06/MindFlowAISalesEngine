using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Application.WhatsApp;
using Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Api.Controllers;

[ApiController]
[Route("api/whatsapp")]
public class WhatsAppController : ControllerBase
{
    private readonly WhatsAppService _service;
    private readonly IWhatsAppRepository _repository;
    private readonly IConfiguration _configuration;

    public WhatsAppController(
        WhatsAppService service,
        IWhatsAppRepository repository,
        IConfiguration configuration)
    {
        _service = service;
        _repository = repository;
        _configuration = configuration;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Webhook — Meta webhook verification challenge
    // ─────────────────────────────────────────────────────────────────

    /// <summary>GET: Meta webhook verification challenge (must be publicly accessible).</summary>
    [HttpGet("webhook")]
    [AllowAnonymous]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string verifyToken,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        var expectedToken = _configuration["WhatsApp:VerifyToken"];
        if (mode == "subscribe" && verifyToken == expectedToken)
            return Ok(challenge);

        return Forbid();
    }

    /// <summary>POST: Receive inbound messages from Meta webhook (must be publicly accessible).</summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveWebhook(CancellationToken cancellationToken)
    {
        // 1. HMAC-SHA256 signature verification
        if (!await VerifyWebhookSignatureAsync())
            return Unauthorized(new { message = "Invalid webhook signature." });

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
            return Ok("no_content");

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (!root.TryGetProperty("entry", out var entries))
                return Ok("no_entry");

            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("changes", out var changes)) continue;

                foreach (var change in changes.EnumerateArray())
                {
                    if (!change.TryGetProperty("value", out var value)) continue;
                    if (!value.TryGetProperty("messages", out var messages)) continue;

                    foreach (var msg in messages.EnumerateArray())
                    {
                        var externalId = msg.GetProperty("id").GetString() ?? string.Empty;
                        var from = msg.GetProperty("from").GetString() ?? string.Empty;
                        var text = msg.TryGetProperty("text", out var textEl)
                            ? textEl.GetProperty("body").GetString() ?? string.Empty
                            : string.Empty;

                        await _service.HandleInboundAsync(externalId, from, text, cancellationToken);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Return 200 so Meta doesn't retry malformed payloads
            return Ok("parse_error");
        }

        return Ok("ok");
    }

    // ─────────────────────────────────────────────────────────────────
    //  Outbound
    // ─────────────────────────────────────────────────────────────────

    /// <summary>POST send a WhatsApp text message.</summary>
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] WhatsAppSendRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ToPhone))
            return BadRequest(new { message = "ToPhone is required." });
        if (string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { message = "Body is required." });

        var message = await _service.SendTextAsync(request.ToPhone, request.Body, request.LeadId, cancellationToken);
        if (message is null)
            return BadRequest(new { message = "Message could not be sent. Contact may not be opted-in or WhatsApp is not configured." });

        return Ok(MapMessage(message));
    }

    /// <summary>POST opt-in a phone number.</summary>
    [HttpPost("opt-in")]
    public async Task<IActionResult> OptIn([FromBody] WhatsAppOptInRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { message = "Phone is required." });

        await _service.OptInAsync(request.Phone, cancellationToken);
        return Ok();
    }

    /// <summary>POST opt-out a phone number.</summary>
    [HttpPost("opt-out")]
    public async Task<IActionResult> OptOut([FromBody] WhatsAppOptInRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { message = "Phone is required." });

        await _service.OptOutAsync(request.Phone, cancellationToken);
        return Ok();
    }

    /// <summary>GET conversation history for a phone number.</summary>
    [HttpGet("conversations/{phone}")]
    public async Task<IActionResult> GetConversation(
        string phone,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        var messages = await _repository.GetMessagesAsync(phone, page, pageSize, cancellationToken);
        return Ok(messages.Select(MapMessage));
    }

    // ─────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────

    private async Task<bool> VerifyWebhookSignatureAsync()
    {
        var appSecret = _configuration["WhatsApp:AppSecret"];
        if (string.IsNullOrWhiteSpace(appSecret))
            return true; // Not configured → skip in dev

        if (!Request.Headers.TryGetValue("X-Hub-Signature-256", out var sigHeader))
            return false;

        var sig = sigHeader.ToString();
        if (!sig.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            return false;

        Request.EnableBuffering();
        Request.Body.Position = 0;
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        Request.Body.Position = 0;
        var rawBytes = ms.ToArray();

        var secretBytes = Encoding.UTF8.GetBytes(appSecret);
        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(rawBytes);
        var expected = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(sig));
    }

    private static WhatsAppMessageResponse MapMessage(Domain.WhatsApp.WhatsAppMessage m) =>
        new(m.Id, m.ExternalMessageId, m.ContactPhone, m.Direction, m.Body, m.TemplateName, m.Status, m.LeadId, m.SentAtUtc);
}
