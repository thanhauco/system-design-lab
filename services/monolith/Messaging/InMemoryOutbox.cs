using System.Collections.Concurrent;

namespace Sdmp.Monolith.Messaging;

/// <summary>
/// Single-instance Outbox backed by an in-memory store. Note: without a shared transaction the
/// enqueue is best-effort relative to the business write — adequate for the zero-dependency learning
/// configuration. The EF-backed implementation provides the true transactional guarantee.
/// </summary>
public sealed class InMemoryOutbox : IOutbox
{
    private readonly ConcurrentDictionary<Guid, OutboxMessage> _messages = new();

    public ValueTask EnqueueAsync(string type, string payload, CancellationToken ct = default)
    {
        var message = new OutboxMessage { Type = type, Payload = payload };
        _messages[message.Id] = message;
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<OutboxMessage>> ListPendingAsync(int max, CancellationToken ct = default)
    {
        var pending = _messages.Values
            .Where(m => m.ProcessedAt is null)
            .OrderBy(m => m.OccurredAt)
            .Take(max)
            .ToList();
        return ValueTask.FromResult<IReadOnlyList<OutboxMessage>>(pending);
    }

    public ValueTask MarkProcessedAsync(Guid id, CancellationToken ct = default)
    {
        if (_messages.TryGetValue(id, out var message))
            message.ProcessedAt = DateTimeOffset.UtcNow;
        return ValueTask.CompletedTask;
    }
}
