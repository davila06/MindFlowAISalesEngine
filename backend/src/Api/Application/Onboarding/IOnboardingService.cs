using Api.Contracts;

namespace Api.Application.Onboarding;

public interface IOnboardingService
{
    Task EnsureForWonOpportunityAsync(Guid leadId, CancellationToken cancellationToken);
    Task ExecuteDueWelcomeJobsAsync(CancellationToken cancellationToken);
    Task ForceWelcomeJobDueAsync(Guid customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingWelcomeJobResponse>> GetWelcomeDeadLetterAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingWelcomeJobResponse>> GetWelcomePoisonQueueAsync(CancellationToken cancellationToken);
    Task RequeueWelcomeJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomerResponse>> ListCustomersAsync(CancellationToken cancellationToken);
    Task<CustomerResponse?> GetCustomerByLeadIdAsync(Guid leadId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingTaskResponse>> GetTasksByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);
    Task<OnboardingTaskResponse?> CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken);
    Task<OnboardingOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken);
    Task EvaluateLifecycleAsync(CancellationToken cancellationToken);
    Task TrackAsync(string trackingToken, CancellationToken cancellationToken);
}
