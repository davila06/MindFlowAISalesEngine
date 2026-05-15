namespace Api.Application.Leads;

public interface ILeadCreatedEventPublisher
{
    Task PublishAsync(LeadCreatedEvent leadCreatedEvent, CancellationToken cancellationToken);
}
