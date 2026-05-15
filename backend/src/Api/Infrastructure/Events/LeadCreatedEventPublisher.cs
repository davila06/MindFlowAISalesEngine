using Api.Application.Leads;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Events;

public class LeadCreatedEventPublisher : ILeadCreatedEventPublisher
{
    private readonly ILogger<LeadCreatedEventPublisher> _logger;

    public LeadCreatedEventPublisher(ILogger<LeadCreatedEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(LeadCreatedEvent leadCreatedEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Domain event published: lead.created (LeadId: {LeadId}, Source: {Source})",
            leadCreatedEvent.LeadId,
            leadCreatedEvent.Source);

        return Task.CompletedTask;
    }
}
