using Api.Domain.Sequences;

namespace Api.Application.Sequences;

public interface ISequenceService
{
    Task<Sequence> CreateAsync(string name, string? description, IEnumerable<SequenceStepInput> steps, CancellationToken cancellationToken);
    Task<Sequence> UpdateAsync(Guid id, string name, string? description, bool isActive, IEnumerable<SequenceStepInput> steps, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<SequenceEnrollment> EnrollLeadAsync(Guid leadId, Guid sequenceId, CancellationToken cancellationToken);
    Task UnenrollLeadAsync(Guid enrollmentId, CancellationToken cancellationToken);
}

public record SequenceStepInput(int Order, string ActionType, string ActionValue, int DelayDays);
