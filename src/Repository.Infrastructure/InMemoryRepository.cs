using System.Collections.Concurrent;
using System.Linq.Expressions;
using Repository.Domain.Repositories.Interfaces;

namespace Repository.Infrastructure;

// Implementazione IN-MEMORY adatta per test o demo. Sostituibile con EF Core.
public class InMemoryRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    private readonly ConcurrentDictionary<TKey, TEntity> store = new();

    public Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        // Assumiamo che l'entità esponga una proprietà "Id" (riflessione minima)
        var key = GetKey(entity);
        if (key == null)
        {
            throw new InvalidOperationException("Entity has no Id");
        }

        if (!store.TryAdd(key, entity))
        {
            throw new InvalidOperationException("Entity with same id already exists");
        }

        return Task.FromResult(entity);
    }

    public Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        foreach (var e in entities)
        {
            var key = GetKey(e) ?? throw new InvalidOperationException("Entity has no Id");
            store.TryAdd(key, e);
        }

        return Task.FromResult(entities);
    }

    public Task DeleteAsync(TKey id, CancellationToken ct = default)
    {
        store.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<TKey> ids, CancellationToken ct = default)
    {
        foreach (var id in ids)
        {
            store.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
    {
        var compiled = predicate.Compile();
        var result = store.Values.Where(compiled).ToList();
        return Task.FromResult<IEnumerable<TEntity>>(result);
    }

    public Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<TEntity>>(store.Values.ToList());
    }

    public Task<IEnumerable<TEntity>> GetRangeAsync(int skip, int take, CancellationToken ct = default)
    {
        var result = store.Values.Skip(skip).Take(take).ToList();
        return Task.FromResult<IEnumerable<TEntity>>(result);
    }

    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        store.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        var key = GetKey(entity) ?? throw new InvalidOperationException("Entity has no Id");
        store.AddOrUpdate(key, entity, (k, old) => entity);
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        foreach (var e in entities)
        {
            var key = GetKey(e) ?? throw new InvalidOperationException("Entity has no Id");
            store.AddOrUpdate(key, e, (k, old) => e);
        }

        return Task.CompletedTask;
    }

    public async Task<TEntity?> PatchAsync(TKey id, Func<TEntity, Task> patchAction, CancellationToken ct = default)
    {
        if (!store.TryGetValue(id, out var entity))
        {
            return null;
        }

        // Applichiamo il patchAction in un blocco logico:
        await patchAction(entity).ConfigureAwait(false);

        // Scriviamo il valore aggiornato
        store.AddOrUpdate(id, entity, (k, old) => entity);
        return entity;
    }

    public async Task<IEnumerable<TEntity>> PatchRangeAsync(IEnumerable<TKey> ids, Func<TEntity, Task> patchAction, CancellationToken ct = default)
    {
        var updated = new List<TEntity>();
        foreach (var id in ids)
        {
            if (store.TryGetValue(id, out var entity))
            {
                await patchAction(entity).ConfigureAwait(false);
                store.AddOrUpdate(id, entity, (k, old) => entity);
                updated.Add(entity);
            }
        }

        return updated;
    }

    // Utility: ottiene il valore della proprietà Id (usa riflessione per semplicità)
    private static TKey? GetKey(TEntity entity)
    {
        var prop = typeof(TEntity).GetProperty("Id");
        if (prop == null)
        {
            return default;
        }

        return (TKey?)prop.GetValue(entity);
    }
}