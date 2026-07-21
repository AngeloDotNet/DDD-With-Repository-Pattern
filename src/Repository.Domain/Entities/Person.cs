using Repository.Domain.Entities.Interfaces;

namespace Repository.Domain.Entities;

// Esempio di aggregate root con alcune regole di dominio
public class Person : Entity<Guid>, IEntity<Guid>, IAggregateRoot
{
    public Person() : base(Guid.Empty)
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Age = 0;
    } // Add this public parameterless constructor

    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public int Age { get; private set; }

    // Costruttore factories sono preferibili in DDD
    public Person(Guid id, string firstName, string lastName, int age) : base(id)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("firstName");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("lastName");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(age);

        FirstName = firstName;
        LastName = lastName;
        Age = age;
    }

    // Esempio di metodo di dominio per patch o update
    public void UpdateName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("firstName");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("lastName");
        }

        FirstName = firstName;
        LastName = lastName;
        // AddDomainEvent(new NameChangedEvent(...));
    }

    public void SetAge(int age)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(age);

        Age = age;
    }
}