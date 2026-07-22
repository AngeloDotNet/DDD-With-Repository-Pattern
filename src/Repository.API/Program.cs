
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
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
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Example API", Version = "v1" });
        });

        //// Configure In-Memory Database and Dependency Injection
        //builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // DbContext: try to use a configured connection string, fallback to InMemory for demo/tests
        var conn = builder.Configuration.GetConnectionString("Default");
        builder.Services.AddDbContext<AppDbContext>(opts =>
        {
            if (!string.IsNullOrWhiteSpace(conn))
            {
                // In production you'd typically use SQL Server / Postgres / etc.
                opts.UseSqlServer(conn);
            }
            else
            {
                opts.UseInMemoryDatabase(Guid.NewGuid().ToString());
            }
        });

        // Repositories and UnitOfWork
        builder.Services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // Application services
        builder.Services.AddScoped<PersonService>();

        var app = builder.Build();
        app.UseHttpsRedirection();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Example API v1"));
        }

        app.UseRouting();
        app.MapControllers();

        // Ensure DB created for in-memory demo
        using (var scope = app.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ctx.Database.EnsureCreated();
        }

        app.Run();
    }
}