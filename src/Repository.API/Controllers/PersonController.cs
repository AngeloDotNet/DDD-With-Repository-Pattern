using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Repository.API.DTOs;
using Repository.Application.Services;
using Repository.Domain.Entities;
using Repository.Domain.Repositories.Interfaces;
using Repository.Infrastructure;

namespace Repository.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonController(PersonService service, IRepository<Person, Guid> repo) : ControllerBase
{
    private readonly EfRepository<Person, Guid> repoConcrete = repo as EfRepository<Person, Guid> ?? throw new ArgumentException("Expected EfRepository<Person,Guid> for advanced queries"); // per FindPagedAsync helper (esempio)

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PersonCreateDto dto, CancellationToken ct)
    {
        var person = new Person(Guid.NewGuid(), dto.FirstName, dto.LastName, dto.Age);
        await service.CreateAsync(person, ct);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = person.Id }, person);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var p = await service.GetByIdAsync(id, ct);

        if (p == null)
        {
            return NotFound();
        }

        return Ok(p);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken ct)
    {
        var all = await service.GetAllAsync(ct);
        return Ok(all);
    }

    [HttpGet("page")]
    public async Task<IActionResult> GetPageAsync([FromQuery] int page = 1, [FromQuery] int size = 10, CancellationToken ct = default)
    {
        var pageData = await service.GetPageAsync(page, size, ct);
        return Ok(pageData);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> ReplaceAsync(Guid id, [FromBody] PersonUpdateDto dto, CancellationToken ct)
    {
        var existing = await service.GetByIdAsync(id, ct);

        if (existing == null)
        {
            return NotFound();
        }

        existing.UpdateName(dto.FirstName, dto.LastName);
        existing.SetAge(dto.Age);

        await service.UpdateAsync(existing, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> PatchAsync(Guid id, [FromBody] PersonPatchDto patchDoc, CancellationToken ct)
    {
        if (patchDoc == null)
        {
            return BadRequest();
        }

        var patched = await service.PatchAsync(id, async person =>
        {
            patchDoc.ApplyTo(person);
            await Task.CompletedTask;
        }, ct);

        if (patched == null)
        {
            return NotFound();
        }

        return Ok(patched);
    }

    [HttpPatch("bulk")]
    public async Task<IActionResult> PatchBulkAsync([FromBody] BulkPatchDto dto, CancellationToken ct)
    {
        var updated = await service.PatchManyAsync(dto.Ids, async person =>
        {
            person.SetAge(person.Age + dto.AgeIncrement);
            await Task.CompletedTask;
        }, ct);

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    // Nuovo endpoint: ricerca/paginazione/ordinamento dinamico via query string
    // Esempio: GET /api/person?firstName=mar&minAge=20&sortBy=lastName&sortDir=asc&page=1&size=10
    [HttpGet("search")]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string? firstName,
        [FromQuery] string? lastName,
        [FromQuery] int? minAge,
        [FromQuery] int? maxAge,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        CancellationToken ct = default)
    {
        // Costruisco il filtro dinamico come lambda
        Func<IQueryable<Person>, IQueryable<Person>>? filter = null;
        filter = q =>
        {
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                q = q.Where(p => p.FirstName.Contains(firstName));
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                q = q.Where(p => p.LastName.Contains(lastName));
            }

            if (minAge.HasValue)
            {
                q = q.Where(p => p.Age >= minAge.Value);
            }

            if (maxAge.HasValue)
            {
                q = q.Where(p => p.Age <= maxAge.Value);
            }

            return q;
        };

        // Costruisco l'ordering dinamico (supporta alcuni campi noti). Puoi estendere con reflection se vuoi.
        Func<IQueryable<Person>, IOrderedQueryable<Person>>? orderBy = null;

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            var dir = (sortDir ?? "asc").ToLowerInvariant();

            switch (sortBy.ToLowerInvariant())
            {
                case "firstname":
                    orderBy = q => dir == "asc" ? q.OrderBy(p => p.FirstName) : q.OrderByDescending(p => p.FirstName);
                    break;
                case "lastname":
                    orderBy = q => dir == "asc" ? q.OrderBy(p => p.LastName) : q.OrderByDescending(p => p.LastName);
                    break;
                case "age":
                    orderBy = q => dir == "asc" ? q.OrderBy(p => p.Age) : q.OrderByDescending(p => p.Age);
                    break;
                default:
                    // fallback: nessun ordering se campo non noto
                    orderBy = null;
                    break;
            }
        }

        // Includes: in questo esempio Person non ha navigations; se ne avessi potresti costruire includes expressions.
        Expression<Func<Person, object>>[]? includes = null;

        // Uso il metodo helper di EfRepository
        var paged = await repoConcrete.FindPagedAsync(filter, orderBy, includes, page, size, ct);
        return Ok(paged);
    }
}