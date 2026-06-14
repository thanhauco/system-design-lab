using Sdmp.Monolith.Messaging;

namespace Sdmp.Monolith.Messaging;

/// <summary>
/// Registers the Outbox. It follows the persistence provider: with Postgres the EF-backed outbox
/// gives transactional guarantees (committed with the business change); in-memory otherwise. The
/// background processor runs in both modes.
/// </summary>
public static class OutboxExtensions
{
    public static WebApplicationBuilder AddOutbox(this WebApplicationBuilder builder)
    {
        var provider = builder.Configuration["Persistence:Provider"] ?? "InMemory";

        if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            // Scoped so it shares the request's DbContext (and therefore its transaction).
            builder.Services.AddScoped<IOutbox, EfOutbox>();
        }
        else
        {
            builder.Services.AddSingleton<IOutbox, InMemoryOutbox>();
        }

        builder.Services.AddHostedService<OutboxProcessor>();
        return builder;
    }
}
