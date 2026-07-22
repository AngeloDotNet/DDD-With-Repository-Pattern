# Esempio Repository Pattern (DDD) con EF Core e UnitOfWork (+ xUnit tests)

Cosa contiene:

- Domain layer con IEntity<TKey>, Entity<TKey>, Person aggregate.
- IRepository<TEntity,TKey> (operazioni CRUD estese).
- IUnitOfWork e implementazione EfUnitOfWork.
- EfRepository<TEntity,TKey> che usa AppDbContext (EF Core).
- PersonService che usa repository + UnitOfWork (commit esplicito).
- Test xUnit che coprono tutti i metodi usando provider InMemory di EF Core.

Nota di design:

- I repository manipolano il DbContext ma NON chiamano SaveChanges. Questo permette di aggregare più operazioni e committare tramite IUnitOfWork.
- Per produzione usare provider reale (SqlServer/Postgres) e gestire transazioni complesse se necessario.

Test:

- I test forniti usano `UseInMemoryDatabase` per isolamento e verificano che ogni operazione sia correttamente applicata dopo `SaveChangesAsync`.

Brevi esempi d'uso (all'interno di un service o repository) — in modo conciso:

Include dinamici:

```csharp
var q = ctx.People.IncludeProperties(p => p.Relation1, p => p.Relation2);
```

Filtro dinamico via lambda:

```csharp
var q = ctx.People.ApplyFilter(q => q.Where(p => p.Age > 18));
```

Ordinamento dinamico via lambda:

```csharp
var q = ctx.People.ApplyOrdering(q => q.OrderBy(p => p.LastName).ThenByDescending(p => p.Age));
```

Ordinamento via helper OrderByFields:

```csharp
var q = ctx.People.OrderByFields((p => p.LastName, true), (p => p.FirstName, true));
```

Paginazione con risultato totalizzato:

```csharp
var page = await ctx.People.ApplyFilter(filter).ApplyOrdering(orderBy).ToPagedResultAsync(page, pageSize);
```