using Api.Application.RulesEngine;
using Api.Application.Security;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/rules")]
public class RulesController : ControllerBase
{
    private readonly IRuleService _ruleService;
    private readonly IAdminAuditService _adminAuditService;
    private readonly IRuleEventListener _ruleEventListener;

    public RulesController(IRuleService ruleService, IAdminAuditService adminAuditService, IRuleEventListener ruleEventListener)
    {
        _ruleService = ruleService;
        _adminAuditService = adminAuditService;
        _ruleEventListener = ruleEventListener;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RuleCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _ruleService.CreateAsync(request, cancellationToken);
            await _adminAuditService.RecordAsync("rule_created", "rules", $"RuleId={response.Id}; Name={response.Name}", cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (RuleValidationException ex)
        {
            return BadRequest(new ValidationProblemDetails(ex.Errors)
            {
                Title = "Rule validation failed.",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var response = await _ruleService.GetAllAsync(cancellationToken);

        if (page is > 0 && pageSize is > 0)
        {
            response = response
                .Skip((page.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();
        }

        return Ok(response);
    }

    [HttpGet("drift-summary")]
    public async Task<IActionResult> GetDriftSummary(CancellationToken cancellationToken)
    {
        var response = await _ruleService.GetDriftSummaryAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _ruleService.GetByIdAsync(id, cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        await _adminAuditService.RecordAsync("rule_updated", "rules", $"RuleId={id}", cancellationToken);

        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RuleUpdateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _ruleService.UpdateAsync(id, request, cancellationToken);
            if (response is null)
            {
                return NotFound();
            }

            return Ok(response);
        }
        catch (RuleValidationException ex)
        {
            return BadRequest(new ValidationProblemDetails(ex.Errors)
            {
                Title = "Rule validation failed.",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _ruleService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        await _adminAuditService.RecordAsync("rule_deleted", "rules", $"RuleId={id}", cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _ruleService.ActivateAsync(id, cancellationToken);
            if (!updated)
            {
                return NotFound();
            }

            await _adminAuditService.RecordAsync("rule_activated", "rules", $"RuleId={id}", cancellationToken);

            return Ok();
        }
        catch (RuleValidationException ex)
        {
            return BadRequest(new ValidationProblemDetails(ex.Errors)
            {
                Title = "Rule validation failed.",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var updated = await _ruleService.DeactivateAsync(id, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        await _adminAuditService.RecordAsync("rule_deactivated", "rules", $"RuleId={id}", cancellationToken);

        return Ok();
    }

    [HttpPost("{id:guid}/promote")]
    public async Task<IActionResult> Promote(Guid id, [FromBody] RulePromotionRequest request, CancellationToken cancellationToken)
    {
        var response = await _ruleService.PromoteAsync(id, request, cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        await _adminAuditService.RecordAsync(
            "rule_promoted",
            "rules",
            $"RuleId={id}; Environment={response.Environment}; Version={response.Version}",
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/dry-run")]
    public async Task<IActionResult> DryRun(Guid id, CancellationToken cancellationToken)
    {
        var response = await _ruleService.DryRunAsync(id, cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpGet("{id:guid}/metrics")]
    public async Task<IActionResult> Metrics(Guid id, CancellationToken cancellationToken)
    {
        var response = await _ruleService.GetMetricsAsync(id, cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost("{id:guid}/rollback")]
    public async Task<IActionResult> Rollback(Guid id, [FromBody] RuleRollbackRequest request, CancellationToken cancellationToken)
    {
        var response = await _ruleService.RollbackAsync(id, request.TargetVersion, cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        await _adminAuditService.RecordAsync("rule_rollback", "rules", $"RuleId={id}; Version={response.Version}", cancellationToken);
        return Ok(response);
    }

    [HttpGet("templates")]
    public async Task<IActionResult> Templates(CancellationToken cancellationToken)
    {
        var response = await _ruleService.GetTemplatesAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("test-fixture")]
    public async Task<IActionResult> TestFixture([FromBody] RuleFixtureTestRequest request, CancellationToken cancellationToken)
    {
        var response = await _ruleService.TestFixtureAsync(request, cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost("events/dispatch")]
    public async Task<IActionResult> DispatchEvent([FromBody] RuleTriggerEventRequest request, CancellationToken cancellationToken)
    {
        var trigger = request.Trigger.Trim().ToLowerInvariant();
        switch (trigger)
        {
            case "lead.created" when request.LeadId.HasValue:
                await _ruleEventListener.OnLeadCreatedAsync(request.LeadId.Value, cancellationToken);
                break;
            case "lead.responded" when request.LeadId.HasValue:
                await _ruleEventListener.OnLeadRespondedAsync(request.LeadId.Value, cancellationToken);
                break;
            case "proposal.sent" when request.LeadId.HasValue && request.ProposalId.HasValue:
                await _ruleEventListener.OnProposalSentAsync(request.LeadId.Value, request.ProposalId.Value, cancellationToken);
                break;
            case "stage_changed" when request.OpportunityId.HasValue && !string.IsNullOrWhiteSpace(request.FromStage) && !string.IsNullOrWhiteSpace(request.ToStage):
            case "pipeline.stage.changed" when request.OpportunityId.HasValue && !string.IsNullOrWhiteSpace(request.FromStage) && !string.IsNullOrWhiteSpace(request.ToStage):
                await _ruleEventListener.OnOpportunityStageChangedAsync(request.OpportunityId.Value, request.FromStage!, request.ToStage!, cancellationToken);
                break;
            default:
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid event payload for trigger dispatch.",
                    Status = StatusCodes.Status400BadRequest
                });
        }

        return Ok();
    }
}
