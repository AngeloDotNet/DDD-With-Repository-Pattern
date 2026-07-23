using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repository.API.Controllers;
using Repository.Application.Services;
using Repository.Domain.Entities;
using Repository.Domain.Repositories.Interfaces;
using Repository.Infrastructure;
using Repository.Infrastructure.Middleware;
using Repository.Infrastructure.Models;

namespace Repository.Tests;

public class PersonRepositoryTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task FindPagedAsync_With_StringSort_WorksAsync()
    {
        var ctx = CreateContext(Guid.NewGuid().ToString());
        var repo = new EfRepository<Person, Guid>(ctx);
        var uow = new EfUnitOfWork(ctx);

        var people = new[]
        {
                new Person(Guid.NewGuid(), "Anna", "Zeta", 25),
                new Person(Guid.NewGuid(), "Bruno", "Alpha", 30),
                new Person(Guid.NewGuid(), "Carlo", "Beta", 20)
            };

        await repo.AddRangeAsync(people);
        await uow.SaveChangesAsync();

        // Sort by LastName asc, Age desc
        var paged = await repo.FindPagedAsync(null, "LastName,Age", "asc,desc", null, 1, 10);
        Assert.Equal(3, paged.TotalCount);
        var items = paged.Items;
        // Expected order by LastName ascending: Alpha, Beta, Zeta
        Assert.Equal("Alpha", items[0].LastName);
        Assert.Equal("Beta", items[1].LastName);
        Assert.Equal("Zeta", items[2].LastName);
    }

    [Fact]
    public async Task FindPagedAsync_Paging_WorksAsync()
    {
        var ctx = CreateContext(Guid.NewGuid().ToString());
        var repo = new EfRepository<Person, Guid>(ctx);
        var uow = new EfUnitOfWork(ctx);

        for (var i = 0; i < 25; i++)
        {
            await repo.AddAsync(new Person(Guid.NewGuid(), $"First{i}", $"Last{i}", 20 + i));
        }

        await uow.SaveChangesAsync();

        var page1 = await repo.FindPagedAsync(null, "LastName", "asc", null, 1, 10);
        var page2 = await repo.FindPagedAsync(null, "LastName", "asc", null, 2, 10);

        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(10, page2.Items.Count);
    }

    [Fact]
    public async Task Controller_Search_Rejects_Invalid_SortPropertyAsync()
    {
        var ctx = CreateContext(Guid.NewGuid().ToString());
        var repo = new EfRepository<Person, Guid>(ctx);
        var uow = new EfUnitOfWork(ctx);
        var service = new PersonService(repo, uow);
        var reader = (IReadRepository<Person, Guid>)repo;

        // seed some data
        var p = new Person(Guid.NewGuid(), "T", "U", 30);
        await repo.AddAsync(p);
        await uow.SaveChangesAsync();

        var controller = new PersonController(service, reader)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // impostiamo QueryParameters con sortBy non consentito
        var qp = new QueryParameters { SortBy = "SomeUnknownProperty", SortDir = "asc", Page = 1, Size = 10 };
        controller.HttpContext!.Items[QueryMappingMiddleware.HttpContextKey] = qp;

        var result = await controller.SearchAsync();
        Assert.IsType<BadRequestObjectResult>(result);
    }
}