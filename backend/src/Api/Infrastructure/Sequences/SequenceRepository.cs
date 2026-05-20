using Api.Application.Sequences;
using Api.Domain.Sequences;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Sequences;

public class SequenceRepository : ISequenceRepository
{
    private readonly LeadsDbContext _db;
    public SequenceRepository(LeadsDbContext db) => _db = db;

    public async Task<Sequence?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Sequences
            .Include(s => s.Steps)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Sequence>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.Sequences
            .Include(s => s.Steps)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Sequence sequence, CancellationToken cancellationToken)
    {
        await _db.Sequences.AddAsync(sequence, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Sequence sequence, CancellationToken cancellationToken)
    {
        _db.Sequences.Update(sequence);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var sequence = await _db.Sequences.FindAsync([id], cancellationToken);
        if (sequence is not null)
        {
            _db.Sequences.Remove(sequence);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
