namespace Repository.Domain.Entities.Interfaces;

// Interfaccia che espone la chiave Id in forma generica
public interface IEntity<TKey> where TKey : IEquatable<TKey>
{
    TKey Id { get; }
}