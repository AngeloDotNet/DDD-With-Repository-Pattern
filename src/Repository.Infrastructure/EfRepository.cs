using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Repository.Domain.Entities.Interfaces;
using Repository.Domain.Repositories.Interfaces;

namespace Repository.Infrastructure;

// Implementazione EF Core del repository generico.
// Nota: questa implementazione NON chiama SaveChanges; il commit è responsabilità del UnitOfWork.
public class EfRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    private readonly AppDbContext context;
    private readonly DbSet<TEntity> dbSet;

    public EfRepository(AppDbContext context)
    {
        this.context = context;
        dbSet = this.context.Set<TEntity>();
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await dbSet.AddAsync(entity, ct).ConfigureAwait(false);
        return entity;
    }

    public async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        await dbSet.AddRangeAsync(entities, ct).ConfigureAwait(false);
        return entities;
    }

    public async Task DeleteAsync(TKey id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct).ConfigureAwait(false);
        if (entity != null)
        {
            dbSet.Remove(entity);
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<TKey> ids, CancellationToken ct = default)
    {
        foreach (var id in ids)
        {
            var entity = await GetByIdAsync(id, ct).ConfigureAwait(false);

            if (entity != null)
            {
                dbSet.Remove(entity);
            }
        }
    }

    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
    {
        return await dbSet.Where(predicate).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await dbSet.ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TEntity>> GetRangeAsync(int skip, int take, CancellationToken ct = default)
    {
        return await dbSet.Skip(skip).Take(take).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        // FindAsync accepts object[] + CancellationToken overload pattern
        var found = await dbSet.FindAsync([id], ct).ConfigureAwait(false);
        return found;
    }

    public Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        dbSet.UpdateRange(entities);
        return Task.CompletedTask;
    }

    public async Task<TEntity?> PatchAsync(TKey id, Func<TEntity, Task> patchAction, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct).ConfigureAwait(false);
        if (entity == null)
        {
            return null;
        }

        await patchAction(entity).ConfigureAwait(false);
        dbSet.Update(entity);
        return entity;
    }

    public async Task<IEnumerable<TEntity>> PatchRangeAsync(IEnumerable<TKey> ids, Func<TEntity, Task> patchAction, CancellationToken ct = default)
    {
        var updated = new List<TEntity>();
        foreach (var id in ids)
        {
            var e = await GetByIdAsync(id, ct).ConfigureAwait(false);

            if (e != null)
            {
                await patchAction(e).ConfigureAwait(false);
                dbSet.Update(e);
                updated.Add(e);
            }
        }

        return updated;
    }
}