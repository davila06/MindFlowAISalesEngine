using Api.Domain.Leads;

namespace Api.Application.Leads;

public class LeadActivityService : ILeadActivityService
{
    private readonly ILeadActivityRepository _repository;

    public LeadActivityService(ILeadActivityRepository repository) => _repository = repository;

    public Task RecordAsync(
        Guid leadId,
        string activityType,
        string? title = null,
        string? description = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        string actor = "system",
        CancellationToken cancellationToken = default)
    {
        var activity = LeadActivity.Create(leadId, activityType, title, description,
            relatedEntityId, relatedEntityType, actor);
        return _repository.AddAsync(activity, cancellationToken);
    }
}
