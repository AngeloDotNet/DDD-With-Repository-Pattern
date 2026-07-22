using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Repository.API.DTOs;
using Repository.Application.Services;
using Repository.Domain.Entities;
using Repository.Domain.Repositories.Interfaces;
using Repository.Infrastructure.Extensions;

namespace Repository.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonController(PersonService service, IReadRepository<Person, Guid> reader) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] PersonCreateDto dto, CancellationToken ct)
    {
        var person = new Person(Guid.NewGuid(), dto.FirstName, dto.LastName, dto.Age);
        await service.CreateAsync(person, ct);
        return CreatedAtAction(nameof(GetById), new { id = person.Id }, person);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
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
    public async Task<IActionResult> Patch(Guid id, [FromBody] PersonPatchDto patchDoc, CancellationToken ct)
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    // Endpoint di ricerca/paginazione/ordinamento dinamico via query string.
    // Supporta sorting per nomi proprietà (anche nested con dot notation), senza switch manuale.
    // Esempio: GET /api/person/search?firstName=mar&minAge=20&sortBy=lastName,age&sortDir=asc,desc&page=1&size=10
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? firstName,
        [FromQuery] string? lastName,
        [FromQuery] int? minAge,
        [FromQuery] int? maxAge,
        [FromQuery] string? sortBy,        // comma separated property names
        [FromQuery] string? sortDir,       // comma separated directions: asc/desc
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        CancellationToken ct = default)
    {
        // filtro dinamico come lambda
        Func<IQueryable<Person>, IQueryable<Person>>? filter = q =>
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

        // costruisco ordinamenti dinamici da query string
        Func<IQueryable<Person>, IOrderedQueryable<Person>>? orderBy = null;

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            var fields = sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var dirs = (sortDir ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var orderings = fields
                .Select((f, i) => (propertyName: f, ascending: (i < dirs.Length ? dirs[i] : "asc").Equals("asc", StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            orderBy = q => (IOrderedQueryable<Person>)q.OrderByPropertyNames(orderings);
        }

        Expression<Func<Person, object>>[]? includes = null; // aggiungi include se necessario

        var paged = await reader.FindPagedAsync(filter, orderBy, includes, page, size, ct);
        return Ok(paged);
    }
}