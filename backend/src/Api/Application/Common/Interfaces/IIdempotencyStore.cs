namespace Api.Application.Common.Interfaces;

public interface IIdempotencyStore
{
    bool TryGet<T>(string scope, string key, out T? value);
    void Set<T>(string scope, string key, T value);
}
