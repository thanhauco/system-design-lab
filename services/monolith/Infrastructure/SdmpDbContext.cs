using Microsoft.EntityFrameworkCore;
using Sdmp.Monolith.Domain;
using Sdmp.Monolith.Messaging;

namespace Sdmp.Monolith.Infrastructure;

/// <summary>
/// EF Core context for the monolith. Order lines are stored as JSON on the order row — appropriate
/// because lines are only ever read/written as part of their owning order aggregate (no independent
/// queries against lines). This keeps the aggregate boundary intact and avoids a join.
/// </summary>
public sealed class SdmpDbContext : DbContext
{
    public SdmpDbContext(DbContextOptions<SdmpDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.DisplayName).IsRequired();
        });

        b.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(p => p.Id);
            e.Property(p => p.Sku).IsRequired();
            e.HasIndex(p => p.Sku).IsUnique();
            e.Property(p => p.Price).HasColumnType("numeric(18,2)");
        });

        b.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(o => o.Id);
            e.Property(o => o.Status).HasConversion<string>();
            // Persist the line collection as JSONB; it travels with the aggregate root.
            e.OwnsMany(o => o.Lines, nav => nav.ToJson());
        });

        b.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox");
            e.HasKey(m => m.Id);
            e.Property(m => m.Type).IsRequired();
            // Index the pending set so the processor's poll stays cheap as the table grows.
            e.HasIndex(m => m.ProcessedAt);
        });
    }
}
