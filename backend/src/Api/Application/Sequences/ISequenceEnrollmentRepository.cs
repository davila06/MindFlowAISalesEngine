using Api.Domain.Sequences;

namespace Api.Application.Sequences;

public interface ISequenceEnrollmentRepository
{
    Task<SequenceEnrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<SequenceEnrollment?> GetActiveByLeadAndSequenceAsync(Guid leadId, Guid sequenceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SequenceEnrollment>> GetActiveByLeadAsync(Guid leadId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SequenceEnrollment>> GetDueEnrollmentsAsync(int batchSize, CancellationToken cancellationToken);
    Task AddAsync(SequenceEnrollment enrollment, CancellationToken cancellationToken);
    Task UpdateAsync(SequenceEnrollment enrollment, CancellationToken cancellationToken);
    /// <summary>Exit ALL active enrollments for a lead matching an exit condition (e.g. stage change).</summary>
    Task ExitAllActiveForLeadAsync(Guid leadId, string exitReason, CancellationToken cancellationToken);
}
