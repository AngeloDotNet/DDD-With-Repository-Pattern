using Repository.Domain.Entities.Interfaces;

namespace Repository.Domain.Entities;

// Base entity con Id generico e controllo di uguaglianza
public abstract class Entity<TKey>(TKey id) : IEntity<TKey> where TKey : IEquatable<TKey>
{
    public TKey Id { get; protected set; } = id;

    // Per DDD puoi inserire domain events qui
    private readonly List<object> domainEvents = [];
    public IReadOnlyCollection<object> DomainEvents => domainEvents.AsReadOnly();
    protected void AddDomainEvent(object @event) => domainEvents.Add(@event);
    protected void ClearDomainEvents() => domainEvents.Clear();
}