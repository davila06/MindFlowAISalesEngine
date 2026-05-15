using Api.Application.Onboarding;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/onboarding")]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [HttpGet("customers")]
    public async Task<IActionResult> ListCustomers(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var response = await _onboardingService.ListCustomersAsync(cancellationToken);

        if (page is > 0 && pageSize is > 0)
        {
            response = response
                .Skip((page.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();
        }

        return Ok(response);
    }

    [HttpGet("customers/by-lead/{leadId:guid}")]
    public async Task<IActionResult> GetCustomerByLeadId(Guid leadId, CancellationToken cancellationToken)
    {
        var response = await _onboardingService.GetCustomerByLeadIdAsync(leadId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("customers/{customerId:guid}/tasks")]
    public async Task<IActionResult> GetTasksByCustomerId(Guid customerId, CancellationToken cancellationToken)
    {
        var response = await _onboardingService.GetTasksByCustomerIdAsync(customerId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("tasks/{taskId:guid}/complete")]
    public async Task<IActionResult> CompleteTask(Guid taskId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _onboardingService.CompleteTaskAsync(taskId, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (OnboardingValidationException ex)
        {
            return BadRequest(new ValidationProblemDetails(ex.Errors)
            {
                Title = "Onboarding validation failed.",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var response = await _onboardingService.GetOverviewAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("lifecycle/evaluate")]
    public async Task<IActionResult> EvaluateLifecycle(CancellationToken cancellationToken)
    {
        await _onboardingService.EvaluateLifecycleAsync(cancellationToken);
        return Ok();
    }

    [HttpGet("welcome-jobs/dead-letter")]
    public async Task<IActionResult> GetWelcomeDeadLetter(CancellationToken cancellationToken)
    {
        var response = await _onboardingService.GetWelcomeDeadLetterAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("welcome-jobs/poison-queue")]
    public async Task<IActionResult> GetWelcomePoisonQueue(CancellationToken cancellationToken)
    {
        var response = await _onboardingService.GetWelcomePoisonQueueAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("welcome-jobs/{jobId:guid}/requeue")]
    public async Task<IActionResult> RequeueWelcomeJob(Guid jobId, CancellationToken cancellationToken)
    {
        await _onboardingService.RequeueWelcomeJobAsync(jobId, cancellationToken);
        return Ok();
    }

    [HttpPost("welcome-jobs/customers/{customerId:guid}/force-due")]
    public async Task<IActionResult> ForceWelcomeJobDue(Guid customerId, CancellationToken cancellationToken)
    {
        await _onboardingService.ForceWelcomeJobDueAsync(customerId, cancellationToken);
        return Ok();
    }

    [HttpPost("welcome-jobs/execute-due")]
    public async Task<IActionResult> ExecuteDueWelcomeJobs(CancellationToken cancellationToken)
    {
        await _onboardingService.ExecuteDueWelcomeJobsAsync(cancellationToken);
        return Ok();
    }

    [HttpGet("track/{trackingToken}")]
    public async Task<IActionResult> Track(string trackingToken, CancellationToken cancellationToken)
    {
        await _onboardingService.TrackAsync(trackingToken, cancellationToken);
        return Content("tracked", "text/plain");
    }
}
