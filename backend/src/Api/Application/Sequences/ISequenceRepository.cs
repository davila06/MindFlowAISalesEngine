using Api.Domain.Sequences;

namespace Api.Application.Sequences;

public interface ISequenceRepository
{
    Task<Sequence?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Sequence>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Sequence sequence, CancellationToken cancellationToken);
    Task UpdateAsync(Sequence sequence, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
