using Microsoft.EntityFrameworkCore;
using Sdmp.Monolith.Infrastructure;

namespace Sdmp.Monolith.Messaging;

/// <summary>
/// EF-backed Outbox. <see cref="EnqueueAsync"/> only adds the row to the shared <see cref="SdmpDbContext"/>;
/// it is committed by the same <c>SaveChanges</c> that persists the business change, giving the
/// transactional all-or-nothing guarantee the Outbox pattern exists to provide.
/// </summary>
public sealed class EfOutbox : IOutbox
{
    private readonly SdmpDbContext _db;

    public EfOutbox(SdmpDbContext db) => _db = db;

    public ValueTask EnqueueAsync(string type, string payload, CancellationToken ct = default)
    {
        // Tracked, not saved here — committed with the business change's SaveChanges.
        _db.Set<OutboxMessage>().Add(new OutboxMessage { Type = type, Payload = payload });
        return ValueTask.CompletedTask;
    }

    public async ValueTask<IReadOnlyList<OutboxMessage>> ListPendingAsync(int max, CancellationToken ct = default)
        => await _db.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(max)
            .ToListAsync(ct);

    public async ValueTask MarkProcessedAsync(Guid id, CancellationToken ct = default)
    {
        if (await _db.Set<OutboxMessage>().FindAsync([id], ct) is { } message)
        {
            message.ProcessedAt = DateTimeOffset.UtcNow;
            message.Attempts++;
            await _db.SaveChangesAsync(ct);
        }
    }
}
