using Api.Domain.Sequences;

namespace Api.Application.Sequences;

public class SequenceService : ISequenceService
{
    private readonly ISequenceRepository _sequenceRepository;
    private readonly ISequenceEnrollmentRepository _enrollmentRepository;

    public SequenceService(
        ISequenceRepository sequenceRepository,
        ISequenceEnrollmentRepository enrollmentRepository)
    {
        _sequenceRepository = sequenceRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<Sequence> CreateAsync(string name, string? description, IEnumerable<SequenceStepInput> steps, CancellationToken cancellationToken)
    {
        var sequence = Sequence.Create(name, description);
        foreach (var s in steps.OrderBy(x => x.Order))
            sequence.AddStep(s.Order, s.ActionType, s.ActionValue, s.DelayDays);

        await _sequenceRepository.AddAsync(sequence, cancellationToken);
        return sequence;
    }

    public async Task<Sequence> UpdateAsync(Guid id, string name, string? description, bool isActive, IEnumerable<SequenceStepInput> steps, CancellationToken cancellationToken)
    {
        var sequence = await _sequenceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sequence {id} not found.");

        sequence.Update(name, description, isActive);

        var newSteps = steps.OrderBy(x => x.Order)
            .Select(s => SequenceStep.Create(sequence.Id, s.Order, s.ActionType, s.ActionValue, s.DelayDays))
            .ToList();
        sequence.SetSteps(newSteps);

        await _sequenceRepository.UpdateAsync(sequence, cancellationToken);
        return sequence;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _sequenceRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<SequenceEnrollment> EnrollLeadAsync(Guid leadId, Guid sequenceId, CancellationToken cancellationToken)
    {
        // Prevent duplicate active enrollment
        var existing = await _enrollmentRepository.GetActiveByLeadAndSequenceAsync(leadId, sequenceId, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"Lead {leadId} is already actively enrolled in sequence {sequenceId}.");

        var sequence = await _sequenceRepository.GetByIdAsync(sequenceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Sequence {sequenceId} not found.");

        if (!sequence.IsActive)
            throw new InvalidOperationException("Cannot enroll into an inactive sequence.");

        var enrollment = SequenceEnrollment.Create(leadId, sequenceId);
        await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
        return enrollment;
    }

    public async Task UnenrollLeadAsync(Guid enrollmentId, CancellationToken cancellationToken)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Enrollment {enrollmentId} not found.");

        enrollment.Exit(SequenceEnrollment.ExitReasons.Manual);
        await _enrollmentRepository.UpdateAsync(enrollment, cancellationToken);
    }
}
