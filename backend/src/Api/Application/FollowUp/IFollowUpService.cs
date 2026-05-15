namespace Api.Application.FollowUp;

public interface IFollowUpService
{
    /// <summary>Schedule a follow-up 48 h from now for the given lead.</summary>
    Task ScheduleAsync(Guid leadId, string? toEmail, CancellationToken cancellationToken);

    /// <summary>Cancel all pending follow-up jobs for a lead (e.g. lead responded).</summary>
    Task CancelByLeadAsync(Guid leadId, string reason, CancellationToken cancellationToken);

    /// <summary>Cancel a specific follow-up job by id.</summary>
    Task CancelAsync(Guid jobId, string reason, CancellationToken cancellationToken);

    /// <summary>Requeue a failed follow-up job as a new scheduled attempt.</summary>
    Task RequeueAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>Execute all due follow-up jobs (called by the background processor).</summary>
    Task ExecuteDueJobsAsync(CancellationToken cancellationToken);
}
