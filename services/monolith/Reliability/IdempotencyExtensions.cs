using StackExchange.Redis;

namespace Sdmp.Monolith.Reliability;

/// <summary>
/// Registers the idempotency store. Defaults to in-memory; set <c>Idempotency:Provider=Redis</c>
/// (env <c>Idempotency__Provider=Redis</c>) with a <c>ConnectionStrings:Redis</c> value to share the
/// store across instances.
/// </summary>
public static class IdempotencyExtensions
{
    public static WebApplicationBuilder AddIdempotency(this WebApplicationBuilder builder)
    {
        var provider = builder.Configuration["Idempotency:Provider"] ?? "InMemory";

        if (provider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = builder.Configuration.GetConnectionString("Redis")
                ?? "localhost:6379";

            // One multiplexer per process (it is a thread-safe, multiplexed connection).
            builder.Services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(connectionString));
            builder.Services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
        }
        else
        {
            builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
        }

        return builder;
    }
}
