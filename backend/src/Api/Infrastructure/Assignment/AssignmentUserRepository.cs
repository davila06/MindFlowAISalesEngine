using Api.Application.Assignment;
using Api.Domain.Assignment;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Assignment;

public class AssignmentUserRepository : IAssignmentUserRepository
{
    private readonly LeadsDbContext _context;

    public AssignmentUserRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AssignmentUser user, CancellationToken cancellationToken)
    {
        _context.AssignmentUsers.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<AssignmentUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _context.AssignmentUsers.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public async Task<AssignmentUser?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.AssignmentUsers
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<AssignmentUser>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.AssignmentUsers
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AssignmentUser>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await _context.AssignmentUsers
            .Where(x => x.IsActive)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
