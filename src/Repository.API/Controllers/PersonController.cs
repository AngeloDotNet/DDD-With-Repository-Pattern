using Microsoft.AspNetCore.Mvc;
using Repository.API.DTOs;
using Repository.Application.Services;
using Repository.Domain.Entities;
using Repository.Domain.Repositories.Interfaces;
using Repository.Infrastructure.Middleware;
using Repository.Infrastructure.Models;

namespace Repository.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonController(PersonService service, IReadRepository<Person, Guid> reader) : ControllerBase
{
    // Whitelist per proprietà che possono essere usate nel sorting (case-insensitive)
    private static readonly string[] allowedSortProperties = ["FirstName", "LastName", "Age"];

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
    // Ora prende i parametri mappati dal middleware QueryMappingMiddleware.
    [HttpGet("search")]
    public async Task<IActionResult> SearchAsync(CancellationToken ct = default)
    {
        // Recupero i parametri mappati dal middleware
        var qp = HttpContext.Items[QueryMappingMiddleware.HttpContextKey] as QueryParameters ?? new QueryParameters();

        Func<IQueryable<Person>, IQueryable<Person>>? filter = q =>
        {
            if (!string.IsNullOrWhiteSpace(qp.FirstName))
            {
                q = q.Where(p => p.FirstName.Contains(qp.FirstName));
            }

            if (!string.IsNullOrWhiteSpace(qp.LastName))
            {
                q = q.Where(p => p.LastName.Contains(qp.LastName));
            }

            if (qp.MinAge.HasValue)
            {
                q = q.Where(p => p.Age >= qp.MinAge.Value);
            }

            if (qp.MaxAge.HasValue)
            {
                q = q.Where(p => p.Age <= qp.MaxAge.Value);
            }

            return q;
        };

        // Validazione whitelist per sortBy
        if (!string.IsNullOrWhiteSpace(qp.SortBy))
        {
            var fields = qp.SortBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var invalid = fields.FirstOrDefault(f => !allowedSortProperties.Any(a => a.Equals(f, StringComparison.OrdinalIgnoreCase)));

            if (invalid != null)
            {
                return BadRequest($"Sorting by '{invalid}' is not allowed.");
            }
        }

        // Uso overload string-based di FindPagedAsync
        var paged = await reader.FindPagedAsync(filter, qp.SortBy, qp.SortDir, null, qp.Page, qp.Size, ct);
        return Ok(paged);
    }
}