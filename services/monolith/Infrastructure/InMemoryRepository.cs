using System.Collections.Concurrent;
using Sdmp.Monolith.Domain;

namespace Sdmp.Monolith.Infrastructure;

/// <summary>
/// Thread-safe in-memory repository. This is the default backing store so the platform runs with
/// zero external dependencies for learning. Swapping to a Postgres-backed implementation is a single
/// DI registration change because both satisfy <see cref="IRepository{T}"/>.
/// </summary>
public sealed class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly ConcurrentDictionary<Guid, T> _store = new();

    public ValueTask<T?> GetAsync(Guid id, CancellationToken ct = default)
        => ValueTask.FromResult(_store.TryGetValue(id, out var entity) ? entity : null);

    public ValueTask<IReadOnlyList<T>> ListAsync(CancellationToken ct = default)
        => ValueTask.FromResult<IReadOnlyList<T>>(_store.Values.ToList());

    public ValueTask<T> AddAsync(T entity, CancellationToken ct = default)
    {
        if (!_store.TryAdd(entity.Id, entity))
            throw new InvalidOperationException($"Entity {entity.Id} already exists.");
        return ValueTask.FromResult(entity);
    }

    public ValueTask<bool> UpdateAsync(T entity, CancellationToken ct = default)
    {
        // Replace only if present; callers treat false as "not found".
        if (!_store.ContainsKey(entity.Id)) return ValueTask.FromResult(false);
        _store[entity.Id] = entity;
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        => ValueTask.FromResult(_store.TryRemove(id, out _));
}
