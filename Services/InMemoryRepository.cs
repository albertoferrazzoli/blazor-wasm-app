using System.Collections.Concurrent;
using System.Linq.Expressions;
using blazor_wasm_app.Models;

namespace blazor_wasm_app.Services;

public class InMemoryRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
    where TKey : struct
{
    protected readonly ConcurrentDictionary<TKey, TEntity> _store = new();

    public virtual Task<TEntity?> GetByIdAsync(TKey id)
    {
        _store.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public virtual Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<TEntity>>(_store.Values.ToList());
    }

    public virtual Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var compiledPredicate = predicate.Compile();
        var results = _store.Values.Where(compiledPredicate).ToList();
        return Task.FromResult<IEnumerable<TEntity>>(results);
    }

    public virtual Task<TEntity> AddAsync(TEntity entity)
    {
        if (_store.TryAdd(entity.Id, entity))
        {
            return Task.FromResult(entity);
        }
        throw new InvalidOperationException($"Entity with id {entity.Id} already exists");
    }

    public virtual Task UpdateAsync(TEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TKey id)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public virtual Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        if (predicate == null)
            return Task.FromResult(_store.Count);

        var compiledPredicate = predicate.Compile();
        return Task.FromResult(_store.Values.Count(compiledPredicate));
    }

    public virtual Task<bool> ExistsAsync(TKey id)
    {
        return Task.FromResult(_store.ContainsKey(id));
    }
}
