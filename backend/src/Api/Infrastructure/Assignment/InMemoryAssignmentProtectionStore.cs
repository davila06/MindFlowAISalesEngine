using System.Collections.Concurrent;
using Api.Application.Assignment;

namespace Api.Infrastructure.Assignment;

public sealed class InMemoryAssignmentProtectionStore : IAssignmentProtectionStore
{
    private readonly ConcurrentDictionary<Guid, bool> _manualProtectionByLead = new();

    public bool IsManualProtected(Guid leadId)
    {
        return _manualProtectionByLead.TryGetValue(leadId, out var enabled) && enabled;
    }

    public void SetManualProtection(Guid leadId, bool enabled)
    {
        if (!enabled)
        {
            _manualProtectionByLead.TryRemove(leadId, out _);
            return;
        }

        _manualProtectionByLead[leadId] = true;
    }
}
