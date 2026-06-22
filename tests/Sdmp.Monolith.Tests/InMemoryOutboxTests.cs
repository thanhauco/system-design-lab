using Sdmp.Monolith.Messaging;
using Xunit;

namespace Sdmp.Monolith.Tests;

public class InMemoryOutboxTests
{
    [Fact]
    public async Task Enqueued_message_appears_as_pending()
    {
        var outbox = new InMemoryOutbox();
        await outbox.EnqueueAsync("OrderCreated", "{\"orderId\":\"x\"}");

        var pending = await outbox.ListPendingAsync(10);

        Assert.Single(pending);
        Assert.Equal("OrderCreated", pending[0].Type);
        Assert.Null(pending[0].ProcessedAt);
    }

    [Fact]
    public async Task Marked_message_is_no_longer_pending()
    {
        var outbox = new InMemoryOutbox();
        await outbox.EnqueueAsync("OrderCreated", "{}");
        var pending = await outbox.ListPendingAsync(10);

        await outbox.MarkProcessedAsync(pending[0].Id);

        Assert.Empty(await outbox.ListPendingAsync(10));
    }

    [Fact]
    public async Task Pending_respects_max_batch_size()
    {
        var outbox = new InMemoryOutbox();
        await outbox.EnqueueAsync("A", "1");
        await outbox.EnqueueAsync("B", "2");
        await outbox.EnqueueAsync("C", "3");

        var pending = await outbox.ListPendingAsync(2);

        Assert.Equal(2, pending.Count);
        Assert.All(pending, m => Assert.Contains(m.Type, new[] { "A", "B", "C" }));
    }
}
