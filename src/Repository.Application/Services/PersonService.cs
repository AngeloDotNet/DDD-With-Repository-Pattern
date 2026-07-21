using Repository.Domain.Entities;
using Repository.Domain.Repositories.Interfaces;

namespace Repository.Application.Services;

// Servizio di alto livello che incapsula uso del repository e UnitOfWork (logica applicativa)
public class PersonService(IRepository<Person, Guid> repo, IUnitOfWork uow)
{
    public async Task<Person> CreateAsync(Person p, CancellationToken ct = default)
    {
        await repo.AddAsync(p, ct).ConfigureAwait(false);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return p;
    }

    public async Task<IEnumerable<Person>> CreateManyAsync(IEnumerable<Person> people, CancellationToken ct = default)
    {
        await repo.AddRangeAsync(people, ct).ConfigureAwait(false);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return people;
    }

    public Task<Person?> GetByIdAsync(Guid id, CancellationToken ct = default) => repo.GetByIdAsync(id, ct);

    public Task<IEnumerable<Person>> GetAllAsync(CancellationToken ct = default) => repo.GetAllAsync(ct);

    public Task<IEnumerable<Person>> GetPageAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var skip = (pageNumber - 1) * pageSize;
        return repo.GetRangeAsync(skip, pageSize, ct);
    }

    public async Task UpdateAsync(Person p, CancellationToken ct = default)
    {
        await repo.UpdateAsync(p, ct).ConfigureAwait(false);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await repo.DeleteAsync(id, ct).ConfigureAwait(false);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // Patch usa un'azione che agisce sull'aggregato per rispettare DDD (cambiare attraverso metodi di dominio)
    public async Task<Person?> PatchAsync(Guid id, Func<Person, Task> patchAction, CancellationToken ct = default)
    {
        var p = await repo.PatchAsync(id, patchAction, ct).ConfigureAwait(false);

        if (p != null)
        {
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return p;
    }

    // Patch range (bulk)
    public async Task<IEnumerable<Person>> PatchManyAsync(IEnumerable<Guid> ids, Func<Person, Task> patchAction, CancellationToken ct = default)
    {
        var updated = await repo.PatchRangeAsync(ids, patchAction, ct).ConfigureAwait(false);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return updated;
    }
}