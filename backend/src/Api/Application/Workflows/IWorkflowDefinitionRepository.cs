using Api.Domain.Workflows;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Application.Workflows;

public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowDefinition>> ListAsync(CancellationToken cancellationToken);
    Task AddAsync(WorkflowDefinition workflow, CancellationToken cancellationToken);
    Task UpdateAsync(WorkflowDefinition workflow, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
