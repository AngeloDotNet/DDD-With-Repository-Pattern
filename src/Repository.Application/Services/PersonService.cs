using Repository.Domain.Entities;
using Repository.Domain.Repositories.Interfaces;

namespace Repository.Application.Services;

// Servizio di alto livello che incapsula uso del repository e logica applicativa
public class PersonService(IRepository<Person, Guid> repo)
{
    public Task<Person> CreateAsync(Person p, CancellationToken ct = default)
        => repo.AddAsync(p, ct);

    public Task<IEnumerable<Person>> CreateManyAsync(IEnumerable<Person> people, CancellationToken ct = default)
        => repo.AddRangeAsync(people, ct);

    public Task<Person?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => repo.GetByIdAsync(id, ct);

    public Task<IEnumerable<Person>> GetAllAsync(CancellationToken ct = default)
        => repo.GetAllAsync(ct);

    public Task<IEnumerable<Person>> GetPageAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var skip = (pageNumber - 1) * pageSize;
        return repo.GetRangeAsync(skip, pageSize, ct);
    }

    public Task UpdateAsync(Person p, CancellationToken ct = default)
        => repo.UpdateAsync(p, ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => repo.DeleteAsync(id, ct);

    // Patch usa un'azione che agisce sull'aggregato per rispettare DDD (cambiare attraverso metodi di dominio)
    public Task<Person?> PatchAsync(Guid id, Func<Person, Task> patchAction, CancellationToken ct = default)
        => repo.PatchAsync(id, patchAction, ct);

    // Patch range (bulk)
    public Task<IEnumerable<Person>> PatchManyAsync(IEnumerable<Guid> ids, Func<Person, Task> patchAction, CancellationToken ct = default)
        => repo.PatchRangeAsync(ids, patchAction, ct);
}