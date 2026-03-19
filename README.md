<!--
# DDD-With-Repository-Pattern
An example of applying DDD and repository patterns in .NET
-->

# Esempio Repository Pattern (DDD) - Async e Id generico

Questo repository dimostra:
- Domain-Driven Design (separazione Domain/Application/Infrastructure/API)
- Repository generico con Id generico (TKey)
- Operazioni CRUD estese: Add, AddRange, Get, GetById, GetRange, Update, UpdateRange, Patch, PatchRange, Delete, DeleteRange
- Tutto asincrono e con CancellationToken
- Implementazione in-memory come esempio (puoi sostituirla con EF Core nella Infrastructure)

Come provare:

1. Creare una soluzione .NET 7/8 e includere i progetti Domain, Application, Infrastructure, Api.
2. Registrare dipendenze in Startup/Program:
   - services.AddSingleton<IRepository<Person, Guid>, InMemoryRepository<Person, Guid>>();
   - services.AddScoped<PersonService>();
3. Avviare l'API e testare gli endpoint (POST/GET/PUT/PATCH/DELETE).

Note di progettazione:

- Le patch dovrebbero preferibilmente passare attraverso metodi di dominio (es. person.SetAge(...), person.UpdateName(...)) per mantenere invarianti.
- Per la persistenza reale usare EF Core e implementare transazioni/UnitOfWork se necessario.