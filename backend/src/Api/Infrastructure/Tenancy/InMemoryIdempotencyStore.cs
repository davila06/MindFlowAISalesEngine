using System.Collections.Concurrent;
using Api.Application.Common.Interfaces;

namespace Api.Infrastructure.Tenancy;

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, object> _store = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGet<T>(string scope, string key, out T? value)
    {
        var composite = BuildKey(scope, key);
        if (_store.TryGetValue(composite, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public void Set<T>(string scope, string key, T value)
    {
        var composite = BuildKey(scope, key);
        _store[composite] = value!;
    }

    private static string BuildKey(string scope, string key) => $"{scope}:{key}";
}
