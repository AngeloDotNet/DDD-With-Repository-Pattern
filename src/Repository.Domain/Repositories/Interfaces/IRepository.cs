using System.Linq.Expressions;

namespace Repository.Domain.Repositories.Interfaces;

// Repository generico per aggregate root
public interface IRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    // Create
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // Read
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<TEntity>> GetRangeAsync(int skip, int take, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    // Update
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // Patch: applica un'azione sul singolo aggregate caricato e salva
    Task<TEntity?> PatchAsync(TKey id, Func<TEntity, Task> patchAction, CancellationToken ct = default);

    // Patch range: applica patchAction a tutti gli elementi selezionati (es. per bulk)
    Task<IEnumerable<TEntity>> PatchRangeAsync(IEnumerable<TKey> ids, Func<TEntity, Task> patchAction, CancellationToken ct = default);

    // Delete
    Task DeleteAsync(TKey id, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<TKey> ids, CancellationToken ct = default);
}