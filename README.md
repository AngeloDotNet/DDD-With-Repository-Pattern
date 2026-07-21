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