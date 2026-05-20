using System.Buffers.Binary;
using System.Text;
using StackExchange.Redis;

namespace Sdmp.Monolith.Reliability;

/// <summary>
/// Distributed idempotency store backed by Redis. The stored response is encoded into a single
/// string value and given a native Redis TTL, so eviction is handled by Redis and the guarantee
/// holds across every instance of the service.
///
/// Encoding: [4-byte status][4-byte contentType length][contentType bytes][body bytes].
/// </summary>
public sealed class RedisIdempotencyStore : IIdempotencyStore
{
    private const string KeyPrefix = "sdmp:idem:";
    private readonly IConnectionMultiplexer _redis;

    public RedisIdempotencyStore(IConnectionMultiplexer redis) => _redis = redis;

    public async ValueTask<IdempotentResponse?> TryGetAsync(string key, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(KeyPrefix + key);
        if (value.IsNullOrEmpty) return null;

        var bytes = (byte[])value!;
        var status = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0, 4));
        var ctLen = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4, 4));
        var contentType = Encoding.UTF8.GetString(bytes, 8, ctLen);
        var body = bytes[(8 + ctLen)..];

        return new IdempotentResponse(status, contentType, body);
    }

    public async ValueTask SaveAsync(string key, IdempotentResponse response, TimeSpan ttl, CancellationToken ct = default)
    {
        var ctBytes = Encoding.UTF8.GetBytes(response.ContentType);
        var buffer = new byte[8 + ctBytes.Length + response.Body.Length];

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(0, 4), response.StatusCode);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4, 4), ctBytes.Length);
        ctBytes.CopyTo(buffer.AsSpan(8));
        response.Body.CopyTo(buffer.AsSpan(8 + ctBytes.Length));

        var db = _redis.GetDatabase();
        await db.StringSetAsync(KeyPrefix + key, buffer, ttl);
    }
}
