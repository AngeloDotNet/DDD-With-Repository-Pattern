using Microsoft.EntityFrameworkCore;
using Repository.Domain.Entities;

namespace Repository.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public virtual DbSet<Person> People { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurazione semplice per Person
        modelBuilder.Entity<Person>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
            b.Property(p => p.LastName).IsRequired().HasMaxLength(100);
            b.Property(p => p.Age).IsRequired();
        });
    }
}