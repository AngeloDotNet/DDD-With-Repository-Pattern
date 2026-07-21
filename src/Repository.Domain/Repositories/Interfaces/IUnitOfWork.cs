namespace Repository.Domain.Repositories.Interfaces;

// Unit of Work: commit changes operazioni fatte dai repository
public interface IUnitOfWork
{
    /// <summary>
    /// Persist changes to the underlying store.
    /// Repositories manipulate the DbContext; UnitOfWork commit the transaction.
    /// </summary>
    /// <returns>Number of state entries written to the underlying database.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}