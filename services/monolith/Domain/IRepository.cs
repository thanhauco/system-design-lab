namespace Sdmp.Monolith.Domain;

/// <summary>
/// Persistence abstraction. The domain depends only on this interface; Infrastructure provides the
/// implementation (in-memory today, Postgres later) without the domain knowing. This inversion is
/// what keeps the architecture clean and the later phases easy to extract.
/// </summary>
public interface IRepository<T> where T : class, IEntity
{
    ValueTask<T?> GetAsync(Guid id, CancellationToken ct = default);
    ValueTask<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    ValueTask<T> AddAsync(T entity, CancellationToken ct = default);
    ValueTask<bool> UpdateAsync(T entity, CancellationToken ct = default);
    ValueTask<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
