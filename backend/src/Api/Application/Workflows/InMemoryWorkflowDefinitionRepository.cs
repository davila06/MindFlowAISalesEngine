using Api.Domain.Workflows;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Application.Workflows;

public class InMemoryWorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly ConcurrentDictionary<Guid, WorkflowDefinition> _store = new();

    public Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_store.TryGetValue(id, out var wf) ? wf : null);

    public Task<IReadOnlyList<WorkflowDefinition>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<WorkflowDefinition>)_store.Values.ToList());

    public Task AddAsync(WorkflowDefinition workflow, CancellationToken cancellationToken)
    {
        _store[workflow.Id] = workflow;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WorkflowDefinition workflow, CancellationToken cancellationToken)
    {
        _store[workflow.Id] = workflow;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
