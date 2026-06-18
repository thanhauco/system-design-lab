using Sdmp.Monolith.Domain;

namespace Sdmp.Monolith.Messaging;

/// <summary>
/// A message staged in the Outbox. Writing the message in the same transaction as the business
/// change (e.g. creating an order) guarantees the event is never lost even if publishing fails — the
/// processor retries until <see cref="ProcessedAt"/> is set. This is the Outbox pattern: it closes
/// the gap between "commit the data" and "publish the event" that a naive dual-write leaves open.
/// </summary>
public sealed class OutboxMessage : IEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Type { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
}
