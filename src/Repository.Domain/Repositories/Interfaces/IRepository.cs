using System.Linq.Expressions;
using Repository.Domain.Queries;

namespace Repository.Domain.Repositories.Interfaces;

// Repository generico per aggregate root (write + read minima)
public interface IRepository<TEntity, TKey>
    where TEntity : class, new()
    where TKey : IEquatable<TKey>
{
    // Create
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // Read basic
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<TEntity>> GetRangeAsync(int skip, int take, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    // Update
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // Patch
    Task<TEntity?> PatchAsync(TKey id, Func<TEntity, Task> patchAction, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> PatchRangeAsync(IEnumerable<TKey> ids, Func<TEntity, Task> patchAction, CancellationToken ct = default);

    // Delete
    Task DeleteAsync(TKey id, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<TKey> ids, CancellationToken ct = default);

    // Advanced: paged find (tutti i repository devono implementarlo)
    Task<PagedResult<TEntity>> FindPagedAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Expression<Func<TEntity, object>>[]? includes = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    // Overload convenience for sort strings
    Task<PagedResult<TEntity>> FindPagedAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter,
        string? sortBy, string? sortDir,
        Expression<Func<TEntity, object>>[]? includes = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);
}