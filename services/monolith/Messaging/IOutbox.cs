namespace Sdmp.Monolith.Messaging;

/// <summary>
/// Stages and drains Outbox messages. <see cref="EnqueueAsync"/> registers a message as part of the
/// current unit of work; with the EF-backed implementation it is committed atomically with the
/// business change. The processor uses <see cref="ListPendingAsync"/> and <see cref="MarkProcessedAsync"/>
/// to publish reliably.
/// </summary>
public interface IOutbox
{
    ValueTask EnqueueAsync(string type, string payload, CancellationToken ct = default);
    ValueTask<IReadOnlyList<OutboxMessage>> ListPendingAsync(int max, CancellationToken ct = default);
    ValueTask MarkProcessedAsync(Guid id, CancellationToken ct = default);
}
