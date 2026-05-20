using Api.Application.Leads;
using Api.Contracts;
using Api.Domain.Leads;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/leads/{leadId:guid}/activities")]
public class LeadActivitiesController : ControllerBase
{
    private readonly ILeadActivityRepository _repository;
    private readonly ILeadActivityService _activityService;

    public LeadActivitiesController(
        ILeadActivityRepository repository,
        ILeadActivityService activityService)
    {
        _repository = repository;
        _activityService = activityService;
    }

    /// <summary>GET paginated activity timeline for a lead.</summary>
    [HttpGet]
    public async Task<IActionResult> GetActivities(
        Guid leadId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var items = await _repository.GetByLeadAsync(leadId, page, pageSize, type, cancellationToken);
        var total = await _repository.CountByLeadAsync(leadId, cancellationToken);

        return Ok(new LeadActivitiesPage
        {
            Items = items.Select(Map).ToArray(),
            Page = page,
            PageSize = pageSize,
            Total = total,
            HasMore = (page * pageSize) < total
        });
    }

    /// <summary>POST a manual note on a lead (shows up in activity timeline).</summary>
    [HttpPost]
    public async Task<IActionResult> AddNote(
        Guid leadId,
        [FromBody] AddLeadNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Note))
            return BadRequest(new { message = "Note text is required." });

        if (request.Note.Length > 2000)
            return BadRequest(new { message = "Note must not exceed 2000 characters." });

        await _activityService.RecordAsync(
            leadId,
            LeadActivity.ActivityTypes.NoteAdded,
            title: "Note added",
            description: request.Note,
            actor: "user",
            cancellationToken: cancellationToken);

        return Ok();
    }

    private static LeadActivityResponse Map(LeadActivity a) => new()
    {
        Id = a.Id,
        LeadId = a.LeadId,
        ActivityType = a.ActivityType,
        Title = a.Title,
        Description = a.Description,
        RelatedEntityId = a.RelatedEntityId,
        RelatedEntityType = a.RelatedEntityType,
        Actor = a.Actor,
        OccurredAtUtc = a.OccurredAtUtc
    };
}
