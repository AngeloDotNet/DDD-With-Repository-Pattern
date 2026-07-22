using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Repository.Infrastructure.Models;

namespace Repository.Infrastructure.Extensions;

public static class QueryExtensions
{
    /// <summary>
    /// Applica Include dinamici specificati come espressioni.
    /// Uso: query = query.IncludeProperties(x => x.Navigation1, x => x.Navigation2);
    /// </summary>
    public static IQueryable<T> IncludeProperties<T>(this IQueryable<T> query, params Expression<Func<T, object>>[] includes) where T : class
    {
        if (includes == null || includes.Length == 0)
        {
            return query;
        }

        foreach (var inc in includes)
        {
            query = query.Include(inc);
        }

        return query;
    }

    /// <summary>
    /// Applica un filtro dinamico passato come lambda che prende e restituisce IQueryable.
    /// Uso: query = query.ApplyFilter(q => q.Where(x => x.Age &gt; 18));
    /// </summary>
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, Func<IQueryable<T>, IQueryable<T>>? filter)
    {
        if (filter == null)
        {
            return query;
        }

        return filter(query);
    }

    /// <summary>
    /// Applica ordering dinamico tramite una lambda che riceve IQueryable e ritorna IOrderedQueryable.
    /// Uso: query = query.ApplyOrdering(q => q.OrderBy(x => x.LastName).ThenByDescending(x => x.Age));
    /// </summary>
    public static IQueryable<T> ApplyOrdering<T>(this IQueryable<T> query, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy)
    {
        if (orderBy == null)
        {
            return query;
        }

        return orderBy(query);
    }

    /// <summary>
    /// Helper che applica una serie di ordinamenti specificati come (selector, ascending) in sequenza.
    /// Uso: query = query.OrderByFields((x => x.LastName, true), (x => x.FirstName, true));
    /// </summary>
    public static IQueryable<T> OrderByFields<T>(this IQueryable<T> source, params (Expression<Func<T, object>> selector, bool ascending)[] orderings)
    {
        if (orderings == null || orderings.Length == 0)
        {
            return source;
        }

        IOrderedQueryable<T>? ordered = null;

        for (var i = 0; i < orderings.Length; i++)
        {
            var sel = orderings[i].selector;
            var asc = orderings[i].ascending;

            if (i == 0)
            {
                ordered = asc ? source.OrderBy(sel) : source.OrderByDescending(sel);
            }
            else
            {
                ordered = asc ? ordered!.ThenBy(sel) : ordered!.ThenByDescending(sel);
            }
        }

        return ordered ?? source;
    }

    /// <summary>
    /// Applica paginazione: costruisce la query con Skip/Take.
    /// Uso: var pageQuery = query.ApplyPagination(page, pageSize);
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> source, int page, int pageSize)
    {
        if (page <= 0)
        {
            page = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        var skip = (page - 1) * pageSize;
        return source.Skip(skip).Take(pageSize);
    }

    /// <summary>
    /// Esegue la query e ritorna un PagedResult con count totale e items.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> source, int page, int pageSize, CancellationToken ct = default)
    {
        if (page <= 0)
        {
            page = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        var count = await source.CountAsync(ct).ConfigureAwait(false);
        var items = await source.ApplyPagination(page, pageSize).ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = count,
            Page = page,
            PageSize = pageSize
        };
    }
}