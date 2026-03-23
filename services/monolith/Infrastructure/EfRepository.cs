using Microsoft.EntityFrameworkCore;
using Sdmp.Monolith.Domain;

namespace Sdmp.Monolith.Infrastructure;

/// <summary>
/// EF Core implementation of <see cref="IRepository{T}"/>. Satisfies the same contract as
/// <see cref="InMemoryRepository{T}"/>, so the domain and endpoints are unchanged when the platform
/// is configured to use Postgres — the only difference is one DI registration.
/// </summary>
public sealed class EfRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly SdmpDbContext _db;
    private readonly DbSet<T> _set;

    public EfRepository(SdmpDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public async ValueTask<T?> GetAsync(Guid id, CancellationToken ct = default)
        => await _set.FindAsync([id], ct);

    public async ValueTask<IReadOnlyList<T>> ListAsync(CancellationToken ct = default)
        => await _set.AsNoTracking().ToListAsync(ct);

    public async ValueTask<T> AddAsync(T entity, CancellationToken ct = default)
    {
        _set.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async ValueTask<bool> UpdateAsync(T entity, CancellationToken ct = default)
    {
        if (await _set.FindAsync([entity.Id], ct) is not { } existing) return false;
        _db.Entry(existing).CurrentValues.SetValues(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async ValueTask<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (await _set.FindAsync([id], ct) is not { } existing) return false;
        _set.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
