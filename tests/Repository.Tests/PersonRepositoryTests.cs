using Microsoft.EntityFrameworkCore;
using Repository.Application.Services;
using Repository.Domain.Entities;
using Repository.Infrastructure;

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
    public async Task Add_GetById_SaveChanges_WorksAsync()
    {
        var ctx = CreateContext(Guid.NewGuid().ToString());
        var repo = new EfRepository<Person, Guid>(ctx);
        var uow = new EfUnitOfWork(ctx);

        var person = new Person(Guid.NewGuid(), "Mario", "Rossi", 30);
        await repo.AddAsync(person);
        await uow.SaveChangesAsync();

        var loaded = await repo.GetByIdAsync(person.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Mario", loaded!.FirstName);
    }

    [Fact]
    public async Task AddRange_GetAll_GetRange_WorksAsync()
    {
        var ctx = CreateContext(Guid.NewGuid().ToString());
        var repo = new EfRepository<Person, Guid>(ctx);
        var uow = new EfUnitOfWork(ctx);

        var list = new[]
        {
            new Person(Guid.NewGuid(), "A","A",1),
            new Person(Guid.NewGuid(), "B","B",2),
            new Person(Guid.NewGuid(), "C","C",3),
        };

        await repo.AddRangeAsync(list);
        await uow.SaveChangesAsync();

        var all = (await repo.GetAllAsync()).ToList();
        Assert.Equal(3, all.Count);

        var page = (await repo.GetRangeAsync(1, 1)).ToList();
        Assert.Single(page);
    }

    [Fact]
    public async Task Find_Update_UpdateRange_WorksAsync()
    {
        var ctx = CreateContext(Guid.NewGuid().ToString());
        var repo = new EfRepository<Person, Guid>(ctx);
        var uow = new EfUnitOfWork(ctx);

        var p1 = new Person(Guid.NewGuid(), "X", "Y", 10);
        var p2 = new Person(Guid.NewGuid(), "X", "Z", 20);

        await repo.AddRangeAsync(new[] { p1, p2 });
        await uow.SaveChangesAsync();

        var found = (await repo.FindAsync(p => p.FirstName == "X")).ToList();
        Assert.Equal(2, found.Count);

        // Update one
        p1.UpdateName("XX", "YY");
        await repo.UpdateAsync(p1);
        await uow.SaveChangesAsync();

        var reloaded = await repo.GetByIdAsync(p1.Id);
        Assert.Equal("XX", reloaded!.FirstName);

        // UpdateRange
        p1.SetAge(99);
        p2.SetAge(88);
        await repo.UpdateRangeAsync(new[] { p1, p2 });
        await uow.SaveChangesAsync();

        var all = (await repo.GetAllAsync()).ToList();
        Assert.Contains(all, x => x.Age == 99);
        Assert.Contains(all, x => x.Age == 88);
    }

    [Fact]
    public async Task Patch_And_PatchRange_WorksAsync()
    {
        var ctx = CreateContext(Guid.NewGuid().ToString());
        var repo = new EfRepository<Person, Guid>(ctx);
        var uow = new EfUnitOfWork(ctx);

        var p1 = new Person(Guid.NewGuid(), "P", "One", 1);
        var p2 = new Person(Guid.NewGuid(), "P", "Two", 2);
        await repo.AddRangeAsync(new[] { p1, p2 });
        await uow.SaveChangesAsync();

        var patched = await repo.PatchAsync(p1.Id, person =>
        {
            person.SetAge(person.Age + 5);
            return Task.CompletedTask;
        });

        await uow.SaveChangesAsync();

        Assert.NotNull(patched);
        Assert.Equal(6, patched!.Age);

        var patchedMany = (await repo.PatchRangeAsync([p1.Id, p2.Id], person =>
        {
            person.SetAge(person.Age + 1);
            return Task.CompletedTask;
        })).ToList();

        await uow.SaveChangesAsync();

        Assert.Equal(2, patchedMany.Count);
        var reloaded1 = await repo.GetByIdAsync(p1.Id);
        var reloaded2 = await repo.GetByIdAsync(p2.Id);
        Assert.Equal(7, reloaded1!.Age);
        Assert.Equal(3, reloaded2!.Age);
    }

    [Fact]
    public async Task Delete_And_DeleteRange_WorksAsync()
    {
        var ctx = CreateContext(Guid.NewGuid().ToString());
        var repo = new EfRepository<Person, Guid>(ctx);
        var uow = new EfUnitOfWork(ctx);

        var p1 = new Person(Guid.NewGuid(), "Del", "One", 1);
        var p2 = new Person(Guid.NewGuid(), "Del", "Two", 2);
        await repo.AddRangeAsync(new[] { p1, p2 });
        await uow.SaveChangesAsync();

        await repo.DeleteAsync(p1.Id);
        await uow.SaveChangesAsync();

        var all = (await repo.GetAllAsync()).ToList();
        Assert.Single(all);

        await repo.DeleteRangeAsync([p2.Id]);
        await uow.SaveChangesAsync();

        all = (await repo.GetAllAsync()).ToList();
        Assert.Empty(all);
    }

    [Fact]
    public async Task PersonService_Integrates_With_UnitOfWorkAsync()
    {
        var ctx = CreateContext(Guid.NewGuid().ToString());
        var repo = new EfRepository<Person, Guid>(ctx);
        var uow = new EfUnitOfWork(ctx);
        var service = new PersonService(repo, uow);

        var p = new Person(Guid.NewGuid(), "Serv", "User", 40);
        await service.CreateAsync(p);

        var loaded = await service.GetByIdAsync(p.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Serv", loaded!.FirstName);

        await service.PatchAsync(p.Id, person =>
        {
            person.SetAge(41);
            return Task.CompletedTask;
        });

        var re = await service.GetByIdAsync(p.Id);
        Assert.Equal(41, re!.Age);

        await service.DeleteAsync(p.Id);
        var after = await service.GetByIdAsync(p.Id);
        Assert.Null(after);
    }
}