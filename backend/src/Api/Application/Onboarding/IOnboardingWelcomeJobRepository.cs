using Api.Domain.Onboarding;

namespace Api.Application.Onboarding;

public interface IOnboardingWelcomeJobRepository
{
    Task AddAsync(OnboardingWelcomeJob job, CancellationToken cancellationToken);
    Task<OnboardingWelcomeJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);
    Task<OnboardingWelcomeJob?> GetLatestByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingWelcomeJob>> GetScheduledDueAsync(DateTime utcNow, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingWelcomeJob>> GetDeadLetterAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingWelcomeJob>> GetPoisonQueueAsync(CancellationToken cancellationToken);
    Task<int> CountPoisonedAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}