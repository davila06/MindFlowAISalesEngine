using Api.Application.FollowUp;
using Api.Application.Common.Security;
using Api.Contracts;
using Api.Domain.FollowUp;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/followup")]
public class FollowUpController : ControllerBase
{
    private readonly IFollowUpService _followUpService;
    private readonly IFollowUpJobRepository _repository;
    private readonly IFollowUpPolicyRepository _policyRepository;
    private readonly ILogger<FollowUpController> _logger;

    public FollowUpController(
        IFollowUpService followUpService,
        IFollowUpJobRepository repository,
        IFollowUpPolicyRepository policyRepository,
        ILogger<FollowUpController> logger)
    {
        _followUpService = followUpService;
        _repository = repository;
        _policyRepository = policyRepository;
        _logger = logger;
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetAllJobs(CancellationToken cancellationToken)
    {
        var jobs = await _repository.GetAllAsync(cancellationToken);
        return Ok(jobs.Select(MapToResponse));
    }

    [HttpGet("leads/{leadId:guid}/jobs")]
    public async Task<IActionResult> GetJobsByLead(Guid leadId, CancellationToken cancellationToken)
    {
        var jobs = await _repository.GetByLeadIdAsync(leadId, cancellationToken);
        return Ok(jobs.Select(MapToResponse));
    }

    [HttpGet("dead-letter")]
    public async Task<IActionResult> GetDeadLetter(CancellationToken cancellationToken)
    {
        var jobs = await _repository.GetDeadLetterAsync(cancellationToken);
        return Ok(jobs.Select(MapToResponse));
    }

    [HttpGet("poison-queue")]
    public async Task<IActionResult> GetPoisonQueue(CancellationToken cancellationToken)
    {
        var jobs = await _repository.GetPoisonQueueAsync(cancellationToken);
        return Ok(jobs.Select(MapToResponse));
    }

    [HttpPost("leads/{leadId:guid}/cancel")]
    public async Task<IActionResult> CancelByLead(
        Guid leadId,
        [FromBody] CancelFollowUpRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _followUpService.CancelByLeadAsync(leadId, request.Reason, cancellationToken);

        _logger.LogInformation(
            "Cancelled follow-up jobs for lead {LeadId}. Reason: {Reason}",
            leadId, request.Reason);

        return Ok();
    }

    [HttpPost("jobs/{jobId:guid}/cancel")]
    public async Task<IActionResult> CancelJob(
        Guid jobId,
        [FromBody] CancelFollowUpRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _followUpService.CancelAsync(jobId, request.Reason, cancellationToken);
        return Ok();
    }

    [HttpPost("jobs/{jobId:guid}/requeue")]
    public async Task<IActionResult> RequeueJob(Guid jobId, CancellationToken cancellationToken)
    {
        await _followUpService.RequeueAsync(jobId, cancellationToken);
        return Ok();
    }

    [HttpGet("policy")]
    public async Task<IActionResult> GetPolicy(CancellationToken cancellationToken)
    {
        var policy = await _policyRepository.GetAsync(cancellationToken);
        if (policy is null)
        {
            return Ok(new FollowUpPolicyResponse());
        }

        return Ok(MapPolicy(policy));
    }

    [HttpPut("policy")]
    public async Task<IActionResult> UpsertPolicy(
        [FromBody] FollowUpPolicyRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var policy = new FollowUpPolicySettings(
            request.QuietHoursEnabled,
            request.QuietHoursStartHourUtc,
            request.QuietHoursEndHourUtc,
            request.Rules.Select(x => new FollowUpPolicyRule
            {
                StageName = x.StageName,
                MinimumScore = x.MinimumScore,
                DelayHours = x.DelayHours
            }));

        await _policyRepository.UpsertAsync(policy, cancellationToken);
        var updated = await _policyRepository.GetAsync(cancellationToken);
        return Ok(MapPolicy(updated!));
    }

    private static FollowUpJobResponse MapToResponse(FollowUpJob j) => new()
    {
        Id              = j.Id,
        LeadId          = j.LeadId,
        ToEmail         = PiiMasking.MaskEmail(j.ToEmail),
        Status          = j.Status,
        AttemptNumber   = j.AttemptNumber,
        ScheduledAtUtc  = j.ScheduledAtUtc,
        DueAtUtc        = j.DueAtUtc,
        ExecutedAtUtc   = j.ExecutedAtUtc,
        CancelledAtUtc  = j.CancelledAtUtc,
        CancelReason    = j.CancelReason,
        ErrorMessage    = j.ErrorMessage
    };

    private static FollowUpPolicyResponse MapPolicy(FollowUpPolicySettings policy) => new()
    {
        QuietHoursEnabled = policy.QuietHoursEnabled,
        QuietHoursStartHourUtc = policy.QuietHoursStartHourUtc,
        QuietHoursEndHourUtc = policy.QuietHoursEndHourUtc,
        Rules = policy.GetRules().Select(x => new FollowUpPolicyRuleItem
        {
            StageName = x.StageName,
            MinimumScore = x.MinimumScore,
            DelayHours = x.DelayHours
        }).ToList()
    };
}
