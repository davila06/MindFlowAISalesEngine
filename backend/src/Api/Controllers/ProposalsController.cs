using Api.Application.Proposals;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/proposals")]
public class ProposalsController : ControllerBase
{
    private readonly IProposalService _proposalService;

    public ProposalsController(IProposalService proposalService)
    {
        _proposalService = proposalService;
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateProposalTemplateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await _proposalService.CreateTemplateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("templates")]
    public async Task<IActionResult> ListTemplates(CancellationToken cancellationToken)
    {
        var response = await _proposalService.ListTemplatesAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProposalRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _proposalService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { proposalId = response.Id }, response);
        }
        catch (ProposalValidationException ex)
        {
            return BadRequest(new ValidationProblemDetails(ex.Errors)
            {
                Title = "Proposal validation failed.",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var response = await _proposalService.ListAsync(cancellationToken);

        if (page is > 0 && pageSize is > 0)
        {
            response = response
                .Skip((page.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();
        }

        return Ok(response);
    }

    [HttpGet("{proposalId:guid}")]
    public async Task<IActionResult> GetById(Guid proposalId, CancellationToken cancellationToken)
    {
        var response = await _proposalService.GetByIdAsync(proposalId, cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpGet("{proposalId:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid proposalId, CancellationToken cancellationToken)
    {
        var result = await _proposalService.GetPdfAsync(proposalId, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return File(result.Value.PdfBytes, "application/pdf", result.Value.FileName);
    }

    [HttpPost("{proposalId:guid}/sign")]
    public async Task<IActionResult> Sign(Guid proposalId, [FromBody] ProposalSignRequest request, CancellationToken cancellationToken)
    {
        var response = await _proposalService.SignAsync(proposalId, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{proposalId:guid}/expire")]
    public async Task<IActionResult> Expire(Guid proposalId, CancellationToken cancellationToken)
    {
        var response = await _proposalService.ExpireAsync(proposalId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{proposalId:guid}/renew")]
    public async Task<IActionResult> Renew(Guid proposalId, [FromBody] ProposalRenewRequest request, CancellationToken cancellationToken)
    {
        var response = await _proposalService.RenewAsync(proposalId, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis(CancellationToken cancellationToken)
    {
        var response = await _proposalService.GetKpisAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("{proposalId:guid}/reminders/force-due")]
    public async Task<IActionResult> ForceReminderDue(Guid proposalId, CancellationToken cancellationToken)
    {
        await _proposalService.ForceReminderDueAsync(proposalId, cancellationToken);
        return Ok();
    }

    [HttpGet("reminders/dead-letter")]
    public async Task<IActionResult> GetReminderDeadLetter(CancellationToken cancellationToken)
    {
        var response = await _proposalService.GetReminderDeadLetterAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("reminders/poison-queue")]
    public async Task<IActionResult> GetReminderPoisonQueue(CancellationToken cancellationToken)
    {
        var response = await _proposalService.GetReminderPoisonQueueAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("{proposalId:guid}/reminders/requeue")]
    public async Task<IActionResult> RequeueReminder(Guid proposalId, CancellationToken cancellationToken)
    {
        await _proposalService.RequeueReminderAsync(proposalId, cancellationToken);
        return Ok();
    }

    [HttpPost("reminders/execute-due")]
    public async Task<IActionResult> ExecuteDueReminders(CancellationToken cancellationToken)
    {
        await _proposalService.ExecuteDueRemindersAsync(cancellationToken);
        return Ok();
    }

    [HttpGet("track/{trackingToken}")]
    public async Task<IActionResult> Track(string trackingToken, CancellationToken cancellationToken)
    {
        await _proposalService.TrackAsync(trackingToken, cancellationToken);
        return Content("tracked", "text/plain");
    }
}
