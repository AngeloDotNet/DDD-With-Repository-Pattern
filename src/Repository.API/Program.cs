
using Microsoft.EntityFrameworkCore;
using Repository.Application.Services;
using Repository.Domain.Repositories.Interfaces;
using Repository.Infrastructure;

namespace Repository.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Configure In-Memory Database and Dependency Injection
        builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        builder.Services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        builder.Services.AddScoped<PersonService>();

        var app = builder.Build();
        app.UseHttpsRedirection();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();
        app.Run();
    }
}