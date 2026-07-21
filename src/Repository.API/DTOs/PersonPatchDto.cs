namespace Repository.API.DTOs;

public sealed class PersonPatchDto
{
    // Campi opzionali: se null -> non modificare
    public string? FirstName { get; init; }
    public string? LastName { get; init; }

    // Setteggio diretto dell'età (sovrascrive) oppure incremento
    public int? Age { get; init; }
    public int? AgeIncrement { get; init; }

    // Applica le modifiche all'aggregate usando i metodi del dominio
    public void ApplyTo(Repository.Domain.Entities.Person person)
    {
        // Aggiorna nome solo se entrambi i campi sono forniti
        if (FirstName is not null && LastName is not null)
        {
            person.UpdateName(FirstName, LastName);
        }

        // Se viene fornita Age la usiamo (sovrascrittura)
        if (Age.HasValue)
        {
            person.SetAge(Age.Value);
        }
        // Altrimenti supportiamo anche un incremento dell'età
        else if (AgeIncrement.HasValue)
        {
            person.SetAge(person.Age + AgeIncrement.Value);
        }
    }
}