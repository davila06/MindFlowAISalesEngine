using Api.Application.Sequences;
using Api.Domain.Sequences;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Sequences;

public class SequenceEnrollmentRepository : ISequenceEnrollmentRepository
{
    private readonly LeadsDbContext _db;
    public SequenceEnrollmentRepository(LeadsDbContext db) => _db = db;

    public Task<SequenceEnrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.SequenceEnrollments.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<SequenceEnrollment?> GetActiveByLeadAndSequenceAsync(Guid leadId, Guid sequenceId, CancellationToken cancellationToken) =>
        _db.SequenceEnrollments.FirstOrDefaultAsync(
            e => e.LeadId == leadId && e.SequenceId == sequenceId && e.Status == SequenceEnrollment.Statuses.Active,
            cancellationToken);

    public async Task<IReadOnlyList<SequenceEnrollment>> GetActiveByLeadAsync(Guid leadId, CancellationToken cancellationToken) =>
        await _db.SequenceEnrollments
            .Where(e => e.LeadId == leadId && e.Status == SequenceEnrollment.Statuses.Active)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SequenceEnrollment>> GetDueEnrollmentsAsync(int batchSize, CancellationToken cancellationToken) =>
        await _db.SequenceEnrollments
            .Where(e => e.Status == SequenceEnrollment.Statuses.Active && e.NextStepDueAtUtc <= DateTime.UtcNow)
            .OrderBy(e => e.NextStepDueAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(SequenceEnrollment enrollment, CancellationToken cancellationToken)
    {
        await _db.SequenceEnrollments.AddAsync(enrollment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SequenceEnrollment enrollment, CancellationToken cancellationToken)
    {
        _db.SequenceEnrollments.Update(enrollment);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ExitAllActiveForLeadAsync(Guid leadId, string exitReason, CancellationToken cancellationToken)
    {
        var enrollments = await _db.SequenceEnrollments
            .Where(e => e.LeadId == leadId && e.Status == SequenceEnrollment.Statuses.Active)
            .ToListAsync(cancellationToken);

        foreach (var e in enrollments)
            e.Exit(exitReason);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
