namespace ProductService.Api.Idempotency;

/// <summary>
/// Tiny idempotency cache. Production would back this with Redis or a
/// dedicated table; the in-memory implementation is fine for dev/demo.
///
/// Used by the Commit endpoint: if a network retry resubmits the same
/// Idempotency-Key, we return the cached response instead of double-committing.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>Returns the cached response body for this key, or null if never seen.</summary>
    Task<string?> TryGetAsync(string key, CancellationToken ct = default);

    /// <summary>Caches a response body under this key. Overwrites if the key already exists.</summary>
    Task SetAsync(string key, string responseBody, TimeSpan ttl, CancellationToken ct = default);
}
