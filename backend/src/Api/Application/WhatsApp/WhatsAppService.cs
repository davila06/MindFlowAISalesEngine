using Api.Application.Leads;
using Api.Application.WhatsApp;
using Api.Domain.WhatsApp;
using Microsoft.Extensions.Logging;

namespace Api.Application.WhatsApp;

public class WhatsAppService
{
    private readonly IWhatsAppRepository _repository;
    private readonly IWhatsAppOutboundService _outbound;
    private readonly ILeadActivityService _activityService;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        IWhatsAppRepository repository,
        IWhatsAppOutboundService outbound,
        ILeadActivityService activityService,
        ILogger<WhatsAppService> logger)
    {
        _repository = repository;
        _outbound = outbound;
        _activityService = activityService;
        _logger = logger;
    }

    public async Task<WhatsAppMessage?> SendTextAsync(string toPhone, string body, Guid? leadId, CancellationToken cancellationToken)
    {
        var contact = await EnsureContactAsync(toPhone, null, cancellationToken);

        if (!contact.OptedIn)
        {
            _logger.LogWarning("Blocked outbound WA to {Phone}: contact has not opted in (GDPR/LGPD).", toPhone);
            return null;
        }

        var extId = await _outbound.SendTextAsync(toPhone, body, cancellationToken);
        var message = WhatsAppMessage.CreateOutbound(toPhone, body, null, leadId);
        if (extId is not null) message.UpdateStatus(WhatsAppMessage.Statuses.Sent, extId);
        else message.UpdateStatus(WhatsAppMessage.Statuses.Failed);

        await _repository.AddMessageAsync(message, cancellationToken);

        if (leadId.HasValue)
            await _activityService.RecordAsync(
                leadId.Value,
                Domain.Leads.LeadActivity.ActivityTypes.WhatsAppSent,
                $"WhatsApp sent: {body[..Math.Min(80, body.Length)]}",
                body,
                message.Id,
                "WhatsAppMessage",
                cancellationToken: cancellationToken);

        return message;
    }

    public async Task HandleInboundAsync(string externalId, string fromPhone, string body, CancellationToken cancellationToken)
    {
        // Idempotency
        if (await _repository.MessageExistsAsync(externalId, cancellationToken))
            return;

        var contact = await EnsureContactAsync(fromPhone, null, cancellationToken);

        // Opt-in keyword detection
        if (body.Trim().ToUpperInvariant() is "JOIN" or "OPT IN" or "OPTIN" or "YES" or "SUBSCRIBE")
        {
            contact.OptIn();
            await _repository.UpdateContactAsync(contact, cancellationToken);
        }
        else if (body.Trim().ToUpperInvariant() is "STOP" or "OPT OUT" or "OPTOUT" or "UNSUBSCRIBE")
        {
            contact.OptOut();
            await _repository.UpdateContactAsync(contact, cancellationToken);
        }

        var message = WhatsAppMessage.CreateInbound(externalId, fromPhone, body, contact.LeadId);
        await _repository.AddMessageAsync(message, cancellationToken);

        if (contact.LeadId.HasValue)
            await _activityService.RecordAsync(
                contact.LeadId.Value,
                Domain.Leads.LeadActivity.ActivityTypes.WhatsAppReceived,
                $"WhatsApp received: {body[..Math.Min(80, body.Length)]}",
                body,
                message.Id,
                "WhatsAppMessage",
                cancellationToken: cancellationToken);
    }

    public async Task OptInAsync(string phone, CancellationToken cancellationToken)
    {
        var contact = await EnsureContactAsync(phone, null, cancellationToken);
        contact.OptIn();
        await _repository.UpdateContactAsync(contact, cancellationToken);
    }

    public async Task OptOutAsync(string phone, CancellationToken cancellationToken)
    {
        var contact = await EnsureContactAsync(phone, null, cancellationToken);
        contact.OptOut();
        await _repository.UpdateContactAsync(contact, cancellationToken);
    }

    private async Task<WhatsAppContact> EnsureContactAsync(string phone, string? displayName, CancellationToken cancellationToken)
    {
        var contact = await _repository.GetContactByPhoneAsync(phone, cancellationToken);
        if (contact is null)
        {
            contact = WhatsAppContact.Create(phone, displayName);
            await _repository.AddContactAsync(contact, cancellationToken);
        }
        return contact;
    }
}
