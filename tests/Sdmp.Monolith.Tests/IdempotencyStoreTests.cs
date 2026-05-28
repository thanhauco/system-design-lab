using Sdmp.Monolith.Reliability;
using Xunit;

namespace Sdmp.Monolith.Tests;

public class IdempotencyStoreTests
{
    [Fact]
    public async Task Save_then_TryGet_returns_the_stored_response()
    {
        var store = new InMemoryIdempotencyStore();
        var response = new IdempotentResponse(201, "application/json", "body"u8.ToArray());

        await store.SaveAsync("key-1", response, TimeSpan.FromMinutes(1));

        var fetched = await store.TryGetAsync("key-1");
        Assert.NotNull(fetched);
        Assert.Equal(201, fetched!.Value.StatusCode);
        Assert.Equal("application/json", fetched.Value.ContentType);
        Assert.Equal("body"u8.ToArray(), fetched.Value.Body);
    }

    [Fact]
    public async Task TryGet_returns_null_for_unknown_key()
    {
        var store = new InMemoryIdempotencyStore();
        Assert.Null(await store.TryGetAsync("missing"));
    }

    [Fact]
    public async Task Expired_entry_is_evicted()
    {
        var store = new InMemoryIdempotencyStore();
        var response = new IdempotentResponse(200, "text/plain", "x"u8.ToArray());

        // Negative TTL => already expired.
        await store.SaveAsync("key-exp", response, TimeSpan.FromMilliseconds(-1));

        Assert.Null(await store.TryGetAsync("key-exp"));
    }
}
