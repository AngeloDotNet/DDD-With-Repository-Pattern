using System.Linq.Expressions;
using Repository.Infrastructure.Models;

namespace Repository.Domain.Repositories.Interfaces;

// Interfaccia separata dedicata alle query/lettura avanzata
public interface IReadRepository<TEntity, TKey>
    where TEntity : class, new()
    where TKey : IEquatable<TKey>
{
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);

    Task<PagedResult<TEntity>> FindPagedAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Expression<Func<TEntity, object>>[]? includes = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);
}