using Api.Domain.Assignment;

namespace Api.Application.Assignment;

public interface IAssignmentUserRepository
{
    Task AddAsync(AssignmentUser user, CancellationToken cancellationToken);
    Task<AssignmentUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<AssignmentUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssignmentUser>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AssignmentUser>> GetActiveAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
