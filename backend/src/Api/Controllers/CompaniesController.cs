using Api.Application.Companies;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(ICompanyService companyService, ILogger<CompaniesController> logger)
    {
        _companyService = companyService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CompanyCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _companyService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (CompanyValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>(ex.Errors))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Company validation failed."
            });
        }
        catch (CompanyConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Company duplicate detected.",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during company creation.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Unexpected error during company creation.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? leadId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        var items = await _companyService.ListAsync(leadId, search, cancellationToken);
        var totalCount = items.Count;
        var paged = items
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        return Ok(new PagedResponse<CompanyResponse>
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
            var response = await _companyService.GetByIdAsync(id, cancellationToken);
            return Ok(response);
        }
        catch (CompanyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Company not found.",
                Detail = ex.Message
            });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CompanyUpdateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _companyService.UpdateAsync(id, request, cancellationToken);
            return Ok(response);
        }
        catch (CompanyValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>(ex.Errors))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Company validation failed."
            });
        }
        catch (CompanyConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Company duplicate detected.",
                Detail = ex.Message
            });
        }
        catch (CompanyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Company not found.",
                Detail = ex.Message
            });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _companyService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (CompanyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Company not found.",
                Detail = ex.Message
            });
        }
    }
}