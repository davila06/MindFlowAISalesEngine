using Api.Application.Scoring;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/scoring")]
public class ScoringController : ControllerBase
{
    private readonly ILeadScoringService _scoringService;

    public ScoringController(ILeadScoringService scoringService)
    {
        _scoringService = scoringService;
    }

    [HttpGet("rules")]
    public IActionResult GetRules()
    {
        return Ok(_scoringService.GetRules());
    }

    [HttpGet("formula")]
    public async Task<IActionResult> GetFormula(CancellationToken cancellationToken)
    {
        var formula = await _scoringService.GetCurrentFormulaAsync(cancellationToken);
        return Ok(formula);
    }

    [HttpGet("formula/versions")]
    public async Task<IActionResult> GetFormulaVersions(CancellationToken cancellationToken)
    {
        var versions = await _scoringService.GetFormulaVersionsAsync(cancellationToken);
        return Ok(versions);
    }

    [HttpGet("formula/proposals")]
    public async Task<IActionResult> GetFormulaProposals(CancellationToken cancellationToken)
    {
        var proposals = await _scoringService.GetFormulaProposalsAsync(cancellationToken);
        return Ok(proposals);
    }

    [HttpPost("formula/proposals")]
    public async Task<IActionResult> CreateFormulaProposal([FromBody] ScoringFormulaProposalRequest request, CancellationToken cancellationToken)
    {
        var proposal = await _scoringService.CreateFormulaProposalAsync(request, cancellationToken);
        return Ok(proposal);
    }

    [HttpPost("formula/proposals/{proposalId:guid}/approve")]
    public async Task<IActionResult> ApproveFormulaProposal(Guid proposalId, [FromQuery] string approvedBy = "system", CancellationToken cancellationToken = default)
    {
        var proposal = await _scoringService.ApproveFormulaProposalAsync(proposalId, approvedBy, cancellationToken);
        if (proposal is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Scoring formula proposal not found.",
                Detail = $"Proposal '{proposalId}' does not exist."
            });
        }

        return Ok(proposal);
    }

    [HttpGet("priority-thresholds")]
    public async Task<IActionResult> GetPriorityThresholds(CancellationToken cancellationToken)
    {
        var response = await _scoringService.GetPriorityThresholdsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPut("priority-thresholds")]
    public async Task<IActionResult> UpdatePriorityThresholds([FromBody] ScoringPriorityThresholdsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _scoringService.UpdatePriorityThresholdsAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid threshold configuration.",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("drift")]
    public async Task<IActionResult> GetScoreDrift([FromQuery] ScoringDriftQueryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _scoringService.GetScoreDriftAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid drift query configuration.",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("leads/{leadId:guid}")]
    public async Task<IActionResult> GetLeadScore(Guid leadId, CancellationToken cancellationToken)
    {
        var score = await _scoringService.GetLeadScoreAsync(leadId, cancellationToken);
        if (score is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Lead not found.",
                Detail = $"Lead '{leadId}' does not exist."
            });
        }

        return Ok(score);
    }

    [HttpGet("leads/{leadId:guid}/explain")]
    public async Task<IActionResult> GetLeadExplainability(Guid leadId, CancellationToken cancellationToken)
    {
        var explainability = await _scoringService.GetLeadExplainabilityAsync(leadId, cancellationToken);
        if (explainability is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Lead not found.",
                Detail = $"Lead '{leadId}' does not exist."
            });
        }

        return Ok(explainability);
    }

    [HttpPost("simulator")]
    public async Task<IActionResult> Simulate([FromBody] ScoringSimulationRequest request, CancellationToken cancellationToken)
    {
        var response = await _scoringService.SimulateAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("conversion-loop")]
    public async Task<IActionResult> GetConversionLoop(CancellationToken cancellationToken)
    {
        var response = await _scoringService.GetConversionLoopAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("recalculate")]
    public async Task<IActionResult> RecalculateScores([FromBody] ScoreRecalculationRequest request, CancellationToken cancellationToken)
    {
        var result = await _scoringService.RecalculateScoresAsync(request.StartDateUtc, request.EndDateUtc, cancellationToken);
        return Ok(result);
    }
}
