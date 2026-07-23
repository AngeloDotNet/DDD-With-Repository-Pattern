using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Repository.Domain.Entities.Interfaces;
using Repository.Domain.Repositories.Interfaces;
using Repository.Infrastructure.Extensions;
using Repository.Infrastructure.Models;

namespace Repository.Infrastructure;

// Implementazione EF Core del repository generico.
// Implementa sia IRepository (write+basicread) che IReadRepository (query avanzate).
public class EfRepository<TEntity, TKey> : IRepository<TEntity, TKey>, IReadRepository<TEntity, TKey>
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
            var e = await GetByIdAsync(id, ct).ConfigureAwait(false);

            if (e != null)
            {
                dbSet.Remove(e);
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

    // FindPagedAsync implementato usando QueryExtensions
    public async Task<PagedResult<TEntity>> FindPagedAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Expression<Func<TEntity, object>>[]? includes = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = dbSet.AsQueryable();

        if (includes != null && includes.Length > 0)
        {
            query = query.IncludeProperties(includes);
        }

        if (filter != null)
        {
            query = filter(query);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        return await query.ToPagedResultAsync(page, pageSize, ct).ConfigureAwait(false);
    }
}