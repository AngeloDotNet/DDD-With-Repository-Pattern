using Repository.Domain.Repositories.Interfaces;

namespace Repository.Infrastructure;

// UnitOfWork semplice basato su EF Core DbContext
public class EfUnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return context.SaveChangesAsync(ct);
    }
}