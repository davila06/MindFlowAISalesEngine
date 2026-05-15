using System.Collections.Concurrent;
using Api.Application.Leads;

namespace Api.Infrastructure.Leads;

public sealed class InMemoryLeadIntakeFailureStore : ILeadIntakeFailureStore
{
    private readonly ConcurrentDictionary<string, LeadIntakeFailedRecord> _records = new(StringComparer.OrdinalIgnoreCase);

    public string Add(LeadIntakeFailedRecord record)
    {
        var id = string.IsNullOrWhiteSpace(record.FailedRequestId) ? Guid.NewGuid().ToString("N") : record.FailedRequestId;
        _records[id] = new LeadIntakeFailedRecord
        {
            FailedRequestId = id,
            Request = record.Request,
            Code = record.Code,
            Message = record.Message,
            FailedAtUtc = record.FailedAtUtc
        };
        return id;
    }

    public bool TryGet(string failedRequestId, out LeadIntakeFailedRecord? record)
    {
        var found = _records.TryGetValue(failedRequestId, out var value);
        record = value;
        return found;
    }

    public IReadOnlyList<LeadIntakeFailedRecord> List(int take)
    {
        var boundedTake = Math.Clamp(take, 1, 500);
        return _records.Values
            .OrderByDescending(x => x.FailedAtUtc)
            .Take(boundedTake)
            .ToList();
    }
}
