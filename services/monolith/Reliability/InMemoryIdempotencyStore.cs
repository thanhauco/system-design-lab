using System.Collections.Concurrent;

namespace Sdmp.Monolith.Reliability;

/// <summary>
/// Single-instance idempotency store backed by an in-memory dictionary with per-entry TTL. Suitable
/// for local development and the default zero-dependency configuration. Expired entries are evicted
/// lazily on read.
/// </summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, Entry> _store = new();

    public ValueTask<IdempotentResponse?> TryGetAsync(string key, CancellationToken ct = default)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTimeOffset.UtcNow)
                return ValueTask.FromResult<IdempotentResponse?>(entry.Response);

            _store.TryRemove(key, out _); // lazy eviction
        }

        return ValueTask.FromResult<IdempotentResponse?>(null);
    }

    public ValueTask SaveAsync(string key, IdempotentResponse response, TimeSpan ttl, CancellationToken ct = default)
    {
        _store[key] = new Entry(response, DateTimeOffset.UtcNow.Add(ttl));
        return ValueTask.CompletedTask;
    }

    private readonly record struct Entry(IdempotentResponse Response, DateTimeOffset ExpiresAt);
}
