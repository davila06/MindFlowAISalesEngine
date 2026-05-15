using Api.Domain.Pipeline;

namespace Api.Application.Common.Interfaces;

public interface IPipelineStageRepository
{
    Task<IReadOnlyList<PipelineStage>> ListAsync(CancellationToken cancellationToken);
    Task<PipelineStage?> GetByIdAsync(Guid stageId, CancellationToken cancellationToken);
    Task SeedDefaultsIfEmptyAsync(CancellationToken cancellationToken);
}