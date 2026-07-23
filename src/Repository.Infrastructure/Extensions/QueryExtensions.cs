using System.Linq.Expressions;
using System.Reflection;
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
    /// Overload non tipizzato per applicare più ordinamenti tramite selectors già espressi (Expression{Func{T,object}}).
    /// Retrocompatibilità (potrebbe introdurre conversioni).
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
    /// Overload tipizzato per evitare conversioni.
    /// Tutti i selectors devono avere lo stesso tipo TKey.
    /// </summary>
    public static IQueryable<T> OrderByFields<T, TKey>(this IQueryable<T> source, params (Expression<Func<T, TKey>> selector, bool ascending)[] orderings)
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
    /// Applica ordinamento dinamico a partire dai nomi delle proprietà (supporta nested tramite dot notation).
    /// Esegue OrderBy/ThenBy dinamicamente costruendo expression generiche (nessuno switch necessario).
    /// </summary>
    public static IQueryable<T> OrderByPropertyNames<T>(this IQueryable<T> source, params (string propertyName, bool ascending)[] orderings)
    {
        if (orderings == null || orderings.Length == 0)
        {
            return source;
        }

        var param = Expression.Parameter(typeof(T), "x");
        var currentExpr = source.Expression;

        var provider = source.Provider;
        var first = true;

        foreach (var ord in orderings)
        {
            // support nested properties: "Address.City.Name"
            Expression? propertyExp = param;
            foreach (var member in ord.propertyName.Split('.'))
            {
                propertyExp = Expression.PropertyOrField(propertyExp!, member);
            }

            var propType = propertyExp!.Type;

            // costruisco lambda: Func<T, propType>
            var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), propType);
            var lambda = Expression.Lambda(delegateType, propertyExp, param);

            string methodName;
            if (first)
            {
                methodName = ord.ascending ? "OrderBy" : "OrderByDescending";
            }
            else
            {
                methodName = ord.ascending ? "ThenBy" : "ThenByDescending";
            }

            // recupero MethodInfo generico dalla reflection
            var methods = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == methodName && m.GetParameters().Length == 2).ToArray();
            var method = methods.First();
            var genericMethod = method.MakeGenericMethod(typeof(T), propType);

            currentExpr = Expression.Call(genericMethod, currentExpr!, Expression.Quote(lambda));
            first = false;
        }

        return provider.CreateQuery<T>(currentExpr!);
    }

    /// <summary>
    /// Applica paginazione: costruisce la query con Skip/Take.
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
    /// Esegue la query e ritorna un PagedResult (dal Domain) con count totale e items.
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