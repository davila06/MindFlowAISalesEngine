using Api.Application.Common.Interfaces;
using Api.Domain.Pipeline;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

public class PipelineStageRepository : IPipelineStageRepository
{
    private readonly LeadsDbContext _dbContext;

    public PipelineStageRepository(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PipelineStage>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.PipelineStages
            .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);
    }

    public Task<PipelineStage?> GetByIdAsync(Guid stageId, CancellationToken cancellationToken)
    {
        return _dbContext.PipelineStages.FirstOrDefaultAsync(x => x.Id == stageId, cancellationToken);
    }

    public async Task SeedDefaultsIfEmptyAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.PipelineStages.AnyAsync(cancellationToken))
        {
            return;
        }

        var stages = DefaultPipelineStages.All
            .Select(x => new PipelineStage(Guid.NewGuid(), x.Name, x.Order, x.Color))
            .ToList();

        await _dbContext.PipelineStages.AddRangeAsync(stages, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}