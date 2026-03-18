namespace Repository.API.DTOs;

// DTO semplice per demo
public record BulkPatchDto(IEnumerable<Guid> Ids, int AgeIncrement);