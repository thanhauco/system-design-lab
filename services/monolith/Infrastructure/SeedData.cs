using Sdmp.Monolith.Domain;

namespace Sdmp.Monolith.Infrastructure;

/// <summary>Seeds the in-memory stores with realistic demo data on startup.</summary>
public static class SeedData
{
    public static async Task SeedAsync(
        IRepository<User> users,
        IRepository<Product> products)
    {
        if ((await users.ListAsync()).Count > 0) return;

        var now = DateTimeOffset.UtcNow;

        await users.AddAsync(new User(Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "ada@sdmp.dev", "Ada Lovelace", now));
        await users.AddAsync(new User(Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "alan@sdmp.dev", "Alan Turing", now));

        await products.AddAsync(new Product(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
            "SKU-CPU-01", "Edge Inference GPU", 2499.00m, 25));
        await products.AddAsync(new Product(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
            "SKU-NET-02", "10GbE Smart NIC", 399.00m, 120));
        await products.AddAsync(new Product(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"),
            "SKU-MEM-03", "256GB DDR5 Module", 899.00m, 60));
    }
}
