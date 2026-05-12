using Microsoft.Extensions.Caching.Memory;

namespace ProductService.Api.Idempotency;

public sealed class InMemoryIdempotencyStore(IMemoryCache cache) : IIdempotencyStore
{
    public Task<string?> TryGetAsync(string key, CancellationToken ct = default)
    {
        cache.TryGetValue<string>(Prefixed(key), out var cached);
        return Task.FromResult(cached);
    }

    public Task SetAsync(string key, string responseBody, TimeSpan ttl, CancellationToken ct = default)
    {
        cache.Set(Prefixed(key), responseBody, ttl);
        return Task.CompletedTask;
    }

    private static string Prefixed(string key) => $"idempotency:{key}";
}
