using Api.Application.Assignment;
using Api.Domain.Assignment;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Assignment;

public class LeadAssignmentRepository : ILeadAssignmentRepository
{
    private readonly LeadsDbContext _context;

    public LeadAssignmentRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LeadAssignment assignment, CancellationToken cancellationToken)
    {
        _context.LeadAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<LeadAssignment?> GetLatestAsync(CancellationToken cancellationToken)
    {
        return await _context.LeadAssignments
            .OrderByDescending(x => x.AssignedAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<LeadAssignment?> GetLatestByLeadIdAsync(Guid leadId, CancellationToken cancellationToken)
    {
        return await _context.LeadAssignments
            .Where(x => x.LeadId == leadId)
            .OrderByDescending(x => x.AssignedAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeadAssignment>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.LeadAssignments
            .OrderByDescending(x => x.AssignedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
