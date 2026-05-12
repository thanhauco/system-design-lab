namespace Sdmp.Monolith.Reliability;

/// <summary>A stored HTTP response, replayed when an idempotency key is seen again.</summary>
public readonly record struct IdempotentResponse(
    int StatusCode, string ContentType, byte[] Body);

/// <summary>
/// Storage for idempotency keys. The in-memory implementation is fine for a single instance; the
/// Redis implementation makes the guarantee hold across a horizontally scaled fleet — the same
/// reason a payment platform stores idempotency keys centrally rather than per-process.
/// </summary>
public interface IIdempotencyStore
{
    ValueTask<IdempotentResponse?> TryGetAsync(string key, CancellationToken ct = default);
    ValueTask SaveAsync(string key, IdempotentResponse response, TimeSpan ttl, CancellationToken ct = default);
}
