using Api.Application.Pipeline;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/pipeline")]
public class PipelineController : ControllerBase
{
    private readonly IPipelineService _pipelineService;
    private readonly ILogger<PipelineController> _logger;

    public PipelineController(IPipelineService pipelineService, ILogger<PipelineController> logger)
    {
        _pipelineService = pipelineService;
        _logger = logger;
    }

    [HttpGet("stages")]
    public async Task<IActionResult> GetStages(CancellationToken cancellationToken)
    {
        var response = await _pipelineService.GetStagesAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("board")]
    public async Task<IActionResult> GetBoard([FromQuery] PipelineBoardQueryRequest query, CancellationToken cancellationToken)
    {
        var response = await _pipelineService.GetBoardAsync(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("board/export")]
    public async Task<IActionResult> ExportBoard([FromQuery] PipelineBoardQueryRequest query, CancellationToken cancellationToken)
    {
        var csv = await _pipelineService.ExportBoardCsvAsync(query, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "pipeline-board.csv");
    }

    [HttpGet("stage-sla-alerts")]
    public async Task<IActionResult> GetStageSlaAlerts([FromQuery] int? defaultSlaHours, CancellationToken cancellationToken)
    {
        var response = await _pipelineService.GetStageSlaAlertsAsync(defaultSlaHours, cancellationToken);
        return Ok(response);
    }

    [HttpGet("throughput")]
    public async Task<IActionResult> GetThroughput([FromQuery] DateTime? startDateUtc, [FromQuery] DateTime? endDateUtc, CancellationToken cancellationToken)
    {
        var response = await _pipelineService.GetThroughputAsync(startDateUtc, endDateUtc, cancellationToken);
        return Ok(response);
    }

    [HttpGet("wip-limits")]
    public async Task<IActionResult> GetWipLimits(CancellationToken cancellationToken)
    {
        var response = await _pipelineService.GetWipLimitsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPut("wip-limits/{stageId:guid}")]
    public async Task<IActionResult> UpdateWipLimit(Guid stageId, [FromBody] PipelineWipLimitUpdateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _pipelineService.UpdateWipLimitAsync(stageId, request, cancellationToken);
            return Ok(response);
        }
        catch (PipelineNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Stage not found.",
                Detail = ex.Message
            });
        }
    }

    [HttpPost("opportunities")]
    public async Task<IActionResult> CreateOpportunity([FromBody] OpportunityCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _pipelineService.CreateOpportunityAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetOpportunityHistory), new { opportunityId = response.Id }, response);
        }
        catch (PipelineValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>(ex.Errors))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Pipeline validation failed."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating opportunity.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Unexpected error creating opportunity.");
        }
    }

    [HttpPatch("opportunities/{opportunityId:guid}/stage")]
    public async Task<IActionResult> MoveOpportunityStage(Guid opportunityId, [FromBody] MoveOpportunityStageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _pipelineService.MoveOpportunityStageAsync(opportunityId, request, cancellationToken);
            return Ok(response);
        }
        catch (PipelineValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>(ex.Errors))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Pipeline validation failed."
            });
        }
        catch (PipelineNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Opportunity not found.",
                Detail = ex.Message
            });
        }
        catch (PipelineConcurrencyException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Pipeline concurrency conflict.",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("opportunities/{opportunityId:guid}/history")]
    public async Task<IActionResult> GetOpportunityHistory(Guid opportunityId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _pipelineService.GetOpportunityHistoryAsync(opportunityId, cancellationToken);
            return Ok(response);
        }
        catch (PipelineNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Opportunity not found.",
                Detail = ex.Message
            });
        }
    }
}