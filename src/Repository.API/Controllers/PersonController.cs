using Microsoft.AspNetCore.Mvc;
using Repository.API.DTOs;
using Repository.Application.Services;
using Repository.Domain.Entities;

namespace Repository.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonController(PersonService service) : ControllerBase
{
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

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] PersonCreateDto dto, CancellationToken ct)
    {
        var person = new Person(Guid.NewGuid(), dto.FirstName, dto.LastName, dto.Age);
        await service.CreateAsync(person, ct);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = person.Id }, person);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> ReplaceAsync(Guid id, [FromBody] PersonUpdateDto dto, CancellationToken ct)
    {
        var existing = await service.GetByIdAsync(id, ct);

        if (existing == null)
        {
            return NotFound();
        }

        // Usare metodi di dominio per rispettare invarianti
        existing.UpdateName(dto.FirstName, dto.LastName);
        existing.SetAge(dto.Age);

        await service.UpdateAsync(existing, ct);
        return NoContent();
    }

    // PATCH usando un DTO tipato e metodi di dominio (più fluido e sicuro rispetto a JsonPatchDocument)
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> PatchAsync(Guid id, [FromBody] PersonPatchDto dto, CancellationToken ct)
    {
        if (dto == null)
        {
            return BadRequest();
        }

        // Regola: aggiornare il nome richiede entrambi FirstName e LastName per rispetto delle invarianti
        if ((dto.FirstName != null) ^ (dto.LastName != null))
        {
            return BadRequest("To update name, provide both FirstName and LastName.");
        }

        var patched = await service.PatchAsync(id, async person =>
        {
            // Applichiamo le modifiche tramite i metodi di dominio esposti dall'aggregate
            dto.ApplyTo(person);
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
            // Esempio: incrementiamo l'età di 1 per tutti gli id passati
            person.SetAge(person.Age + dto.AgeIncrement);
            await Task.CompletedTask;
        }, ct);

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}