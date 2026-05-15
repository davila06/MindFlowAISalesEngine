using Api.Application.Contacts;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly ILogger<ContactsController> _logger;

    public ContactsController(IContactService contactService, ILogger<ContactsController> logger)
    {
        _contactService = contactService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _contactService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (ContactValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>(ex.Errors))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Contact validation failed."
            });
        }
        catch (ContactConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Contact duplicate detected.",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during contact creation.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Unexpected error during contact creation.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? leadId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        var items = await _contactService.ListAsync(leadId, search, cancellationToken);
        var totalCount = items.Count;
        var paged = items
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        return Ok(new PagedResponse<ContactResponse>
        {
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
            Items = paged
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _contactService.GetByIdAsync(id, cancellationToken);
            return Ok(response);
        }
        catch (ContactNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Contact not found.",
                Detail = ex.Message
            });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ContactUpdateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _contactService.UpdateAsync(id, request, cancellationToken);
            return Ok(response);
        }
        catch (ContactValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>(ex.Errors))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Contact validation failed."
            });
        }
        catch (ContactConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Contact duplicate detected.",
                Detail = ex.Message
            });
        }
        catch (ContactNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Contact not found.",
                Detail = ex.Message
            });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _contactService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (ContactNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Contact not found.",
                Detail = ex.Message
            });
        }
    }
}