using Microsoft.EntityFrameworkCore;
using Sdmp.Monolith.Domain;

namespace Sdmp.Monolith.Infrastructure;

/// <summary>
/// Registers the persistence layer. The provider is chosen by configuration so the same code runs
/// with zero external dependencies (in-memory) for learning, or against Postgres for the full
/// platform — set <c>Persistence:Provider=Postgres</c> (or env <c>Persistence__Provider=Postgres</c>).
/// </summary>
public static class PersistenceExtensions
{
    public static WebApplicationBuilder AddPersistence(this WebApplicationBuilder builder)
    {
        var provider = builder.Configuration["Persistence:Provider"] ?? "InMemory";

        if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = builder.Configuration.GetConnectionString("Postgres")
                ?? "Host=localhost;Port=5432;Database=sdmp;Username=sdmp;Password=sdmp";

            builder.Services.AddDbContext<SdmpDbContext>(o => o.UseNpgsql(connectionString));
            builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

            // Readiness now depends on the database being reachable.
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<SdmpDbContext>("postgres", tags: ["ready"]);
        }
        else
        {
            // Singletons so in-memory data survives across requests.
            builder.Services.AddSingleton<IRepository<User>, InMemoryRepository<User>>();
            builder.Services.AddSingleton<IRepository<Product>, InMemoryRepository<Product>>();
            builder.Services.AddSingleton<IRepository<Order>, InMemoryRepository<Order>>();
        }

        return builder;
    }

    /// <summary>Applies schema (for Postgres) and seeds demo data, regardless of provider.</summary>
    public static async Task InitializePersistenceAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        if (sp.GetService<SdmpDbContext>() is { } db)
        {
            // EnsureCreated is fine for a learning platform; migrations arrive with the relational
            // modeling lab in data/.
            await db.Database.EnsureCreatedAsync();
        }

        await SeedData.SeedAsync(
            sp.GetRequiredService<IRepository<User>>(),
            sp.GetRequiredService<IRepository<Product>>());
    }
}
