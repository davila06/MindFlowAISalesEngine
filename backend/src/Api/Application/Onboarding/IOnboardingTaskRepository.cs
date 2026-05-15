using Api.Domain.Onboarding;

namespace Api.Application.Onboarding;

public interface IOnboardingTaskRepository
{
    Task AddRangeAsync(IEnumerable<OnboardingTask> tasks, CancellationToken cancellationToken);
    Task<OnboardingTask?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingTask>> ListByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingTask>> ListAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
