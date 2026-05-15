using Api.Application.Assignment;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/assignments")]
public class AssignmentsController : ControllerBase
{
    private readonly ILeadAssignmentService _service;

    public AssignmentsController(ILeadAssignmentService service)
    {
        _service = service;
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(
        [FromBody] AssignmentUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _service.CreateUserAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetUsers), new { id = response.Id }, response);
        }
        catch (AssignmentConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Assignment user conflict.",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetUsersAsync(cancellationToken);
        return Ok(ApplyPagination(response, page, pageSize));
    }

    [HttpPut("users/{userId:guid}/availability")]
    public async Task<IActionResult> UpdateUserAvailability(
        Guid userId,
        [FromBody] AssignmentUserAvailabilityUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateUserAvailabilityAsync(userId, request.IsActive, cancellationToken);
        if (updated is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Assignment user not found.",
                Detail = $"Assignment user '{userId}' does not exist."
            });
        }

        return Ok(updated);
    }

    [HttpGet("capacity-load")]
    public async Task<IActionResult> GetCapacityLoad(CancellationToken cancellationToken)
    {
        var response = await _service.GetCapacityLoadAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetAssignmentsAsync(cancellationToken);
        return Ok(ApplyPagination(response, page, pageSize));
    }

    [HttpGet("leads/{leadId:guid}")]
    public async Task<IActionResult> GetLatestByLead(Guid leadId, CancellationToken cancellationToken)
    {
        var response = await _service.GetLatestByLeadAsync(leadId, cancellationToken);
        if (response is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Assignment not found.",
                Detail = $"No assignment found for lead '{leadId}'."
            });
        }

        return Ok(response);
    }

    [HttpPost("leads/{leadId:guid}/manual")]
    public async Task<IActionResult> ManualAssignLead(
        Guid leadId,
        [FromBody] ManualLeadAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var assignment = await _service.AssignLeadManuallyAsync(leadId, request, cancellationToken);
        if (assignment is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Assignment user not found.",
                Detail = $"Assignment user '{request.UserId}' does not exist."
            });
        }

        return Ok(assignment);
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit(
        [FromQuery] Guid? leadId,
        [FromQuery] Guid? userId,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var response = await _service.GetAuditAsync(leadId, userId, take, cancellationToken);
        return Ok(response);
    }

    [HttpGet("fairness")]
    public async Task<IActionResult> GetFairness(CancellationToken cancellationToken)
    {
        var response = await _service.GetFairnessAsync(cancellationToken);
        return Ok(response);
    }

    private static IReadOnlyList<T> ApplyPagination<T>(IReadOnlyList<T> source, int? page, int? pageSize)
    {
        if (page is null || pageSize is null || page <= 0 || pageSize <= 0)
        {
            return source;
        }

        return source
            .Skip((page.Value - 1) * pageSize.Value)
            .Take(pageSize.Value)
            .ToList();
    }
}
