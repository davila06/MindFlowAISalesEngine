using Api.Domain.Leads;

namespace Api.Application.Leads;

/// <summary>
/// Records activity events for a lead. Called by intake, pipeline, email and scoring services
/// to auto-populate the activity timeline without coupling domain to persistence directly.
/// </summary>
public interface ILeadActivityService
{
    Task RecordAsync(
        Guid leadId,
        string activityType,
        string? title = null,
        string? description = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        string actor = "system",
        CancellationToken cancellationToken = default);
}
