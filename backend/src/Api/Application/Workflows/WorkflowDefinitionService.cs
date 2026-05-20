using Api.Domain.Workflows;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Application.Workflows;

public class WorkflowDefinitionService
{
    private readonly IWorkflowDefinitionRepository _repository;

    public WorkflowDefinitionService(IWorkflowDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<WorkflowDefinition>> ListAsync(CancellationToken cancellationToken)
        => _repository.ListAsync(cancellationToken);

    public Task AddAsync(WorkflowDefinition workflow, CancellationToken cancellationToken)
        => _repository.AddAsync(workflow, cancellationToken);

    public Task UpdateAsync(WorkflowDefinition workflow, CancellationToken cancellationToken)
        => _repository.UpdateAsync(workflow, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        => _repository.DeleteAsync(id, cancellationToken);
}
