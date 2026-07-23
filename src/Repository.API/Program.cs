
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Repository.Application.Services;
using Repository.Domain.Repositories.Interfaces;
using Repository.Infrastructure;
using Repository.Infrastructure.Middleware;

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

        // Registrazione repository: EfRepository implementa sia IRepository che IReadRepository
        builder.Services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        builder.Services.AddScoped(typeof(IReadRepository<,>), typeof(EfRepository<,>));

        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        builder.Services.AddScoped<PersonService>();

        var app = builder.Build();
        app.UseHttpsRedirection();

        // Usare il middleware prima dei controller
        app.UseMiddleware<QueryMappingMiddleware>();

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