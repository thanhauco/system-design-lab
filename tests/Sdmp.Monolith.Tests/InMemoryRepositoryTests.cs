using Sdmp.Monolith.Domain;
using Sdmp.Monolith.Infrastructure;
using Xunit;

namespace Sdmp.Monolith.Tests;

public class InMemoryRepositoryTests
{
    [Fact]
    public async Task Add_then_Get_returns_the_entity()
    {
        var repo = new InMemoryRepository<User>();
        var user = new User(Guid.NewGuid(), "a@b.com", "A", DateTimeOffset.UtcNow);

        await repo.AddAsync(user);

        var fetched = await repo.GetAsync(user.Id);
        Assert.Equal(user, fetched);
    }

    [Fact]
    public async Task Add_duplicate_id_throws()
    {
        var repo = new InMemoryRepository<User>();
        var user = new User(Guid.NewGuid(), "a@b.com", "A", DateTimeOffset.UtcNow);

        await repo.AddAsync(user);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.AddAsync(user));
    }

    [Fact]
    public async Task Update_returns_false_when_missing()
    {
        var repo = new InMemoryRepository<User>();
        var user = new User(Guid.NewGuid(), "a@b.com", "A", DateTimeOffset.UtcNow);

        Assert.False(await repo.UpdateAsync(user));
    }

    [Fact]
    public async Task Delete_removes_the_entity()
    {
        var repo = new InMemoryRepository<Product>();
        var product = new Product(Guid.NewGuid(), "SKU-1", "P", 10m, 5);
        await repo.AddAsync(product);

        Assert.True(await repo.DeleteAsync(product.Id));
        Assert.Null(await repo.GetAsync(product.Id));
    }
}
