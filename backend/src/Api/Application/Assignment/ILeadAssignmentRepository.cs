using Api.Domain.Assignment;

namespace Api.Application.Assignment;

public interface ILeadAssignmentRepository
{
    Task AddAsync(LeadAssignment assignment, CancellationToken cancellationToken);
    Task<LeadAssignment?> GetLatestAsync(CancellationToken cancellationToken);
    Task<LeadAssignment?> GetLatestByLeadIdAsync(Guid leadId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeadAssignment>> GetAllAsync(CancellationToken cancellationToken);
}
