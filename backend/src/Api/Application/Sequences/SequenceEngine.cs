using Api.Application.Email;
using Api.Application.Leads;
using Api.Domain.Sequences;
using Microsoft.Extensions.Logging;

namespace Api.Application.Sequences;

public class SequenceEngine : ISequenceEngine
{
    private readonly ISequenceEnrollmentRepository _enrollmentRepository;
    private readonly ISequenceRepository _sequenceRepository;
    private readonly ILeadActivityService _activityService;
    private readonly ILogger<SequenceEngine> _logger;

    public SequenceEngine(
        ISequenceEnrollmentRepository enrollmentRepository,
        ISequenceRepository sequenceRepository,
        ILeadActivityService activityService,
        ILogger<SequenceEngine> logger)
    {
        _enrollmentRepository = enrollmentRepository;
        _sequenceRepository = sequenceRepository;
        _activityService = activityService;
        _logger = logger;
    }

    public async Task RunDueBatchAsync(CancellationToken cancellationToken)
    {
        const int BatchSize = 100;
        var dueEnrollments = await _enrollmentRepository.GetDueEnrollmentsAsync(BatchSize, cancellationToken);

        foreach (var enrollment in dueEnrollments)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await ProcessEnrollmentAsync(enrollment, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process sequence enrollment {EnrollmentId}.", enrollment.Id);
            }
        }
    }

    private async Task ProcessEnrollmentAsync(SequenceEnrollment enrollment, CancellationToken cancellationToken)
    {
        var sequence = await _sequenceRepository.GetByIdAsync(enrollment.SequenceId, cancellationToken);
        if (sequence is null || !sequence.IsActive)
        {
            enrollment.Exit(SequenceEnrollment.ExitReasons.Manual);
            await _enrollmentRepository.UpdateAsync(enrollment, cancellationToken);
            return;
        }

        var step = sequence.Steps.FirstOrDefault(s => s.Order == enrollment.NextStepOrder);
        if (step is null)
        {
            enrollment.Complete();
            await _enrollmentRepository.UpdateAsync(enrollment, cancellationToken);
            await _activityService.RecordAsync(
                enrollment.LeadId,
                Domain.Leads.LeadActivity.ActivityTypes.SequenceStepSent,
                $"Sequence '{sequence.Name}' completed",
                null,
                enrollment.Id,
                "SequenceEnrollment",
                cancellationToken: cancellationToken);
            return;
        }

        // Execute the step action
        await ExecuteStepAsync(enrollment.LeadId, sequence.Name, step, cancellationToken);

        // Determine next step
        var nextStep = sequence.Steps.OrderBy(s => s.Order).FirstOrDefault(s => s.Order > step.Order);
        if (nextStep is null)
        {
            enrollment.Complete();
        }
        else
        {
            enrollment.AdvanceToNextStep(nextStep.Order, nextStep.DelayDays);
        }

        await _enrollmentRepository.UpdateAsync(enrollment, cancellationToken);
    }

    private async Task ExecuteStepAsync(Guid leadId, string sequenceName, SequenceStep step, CancellationToken cancellationToken)
    {
        switch (step.ActionType)
        {
            case Sequence.StepActionTypes.SendEmail:
                await _activityService.RecordAsync(
                    leadId,
                    Domain.Leads.LeadActivity.ActivityTypes.SequenceStepSent,
                    $"Sequence email: {step.ActionValue}",
                    $"Sequence '{sequenceName}', step {step.Order}",
                    step.Id,
                    "SequenceStep",
                    cancellationToken: cancellationToken);
                _logger.LogInformation(
                    "Sequence step {StepId} (send_email:{Template}) executed for lead {LeadId}.",
                    step.Id, step.ActionValue, leadId);
                break;

            case Sequence.StepActionTypes.AddNote:
                await _activityService.RecordAsync(
                    leadId,
                    Domain.Leads.LeadActivity.ActivityTypes.NoteAdded,
                    $"Sequence note (step {step.Order})",
                    step.ActionValue,
                    step.Id,
                    "SequenceStep",
                    cancellationToken: cancellationToken);
                break;

            default:
                _logger.LogWarning("Unknown sequence step action type '{ActionType}'.", step.ActionType);
                break;
        }
    }
}
