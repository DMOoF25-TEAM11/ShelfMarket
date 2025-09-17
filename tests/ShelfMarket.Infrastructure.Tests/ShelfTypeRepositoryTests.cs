using Microsoft.EntityFrameworkCore;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;
using ShelfMarket.Infrastructure.Repositories;

namespace ShelfMarket.Infrastructure.Tests;

[TestClass]
public class ShelfTypeRepositoryTests
{
    private static ShelfMarketDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ShelfMarketDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .EnableSensitiveDataLogging()
            .Options;

        var ctx = new ShelfMarketDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static ShelfTypeRepository CreateRepository(string dbName) =>
        new(CreateDbContext(dbName));

    [TestMethod]
    public async Task AddAsync_AddsEntity()
    {
        var dbName = Guid.NewGuid().ToString();
        var repo = CreateRepository(dbName);

        var shelfType = new ShelfType("Type1", "Description1");

        await repo.AddAsync(shelfType);

        var result = await repo.GetByIdAsync(shelfType.Id!.Value);
        Assert.IsNotNull(result);
        Assert.AreEqual("Description1", result.Description);
    }

    [TestMethod]
    public async Task AddRangeAsync_AddsEntities()
    {
        var dbName = Guid.NewGuid().ToString();
        var repo = CreateRepository(dbName);

        var shelfTypes = new List<ShelfType>
            {
                new ShelfType("TypeA", "DescA"),
                new ShelfType("TypeB", "DescB")
            };

        await repo.AddRangeAsync(shelfTypes);

        var allShelfTypes = await repo.GetAllAsync();
        var count = allShelfTypes.Count();

        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public async Task UpdateAsync_UpdatesEntity()
    {
        var dbName = Guid.NewGuid().ToString();
        var repo = CreateRepository(dbName);

        var shelfType = new ShelfType("TypeX", "DescX");
        await repo.AddAsync(shelfType);

        shelfType.Description = "UpdatedDesc";
        await repo.UpdateAsync(shelfType);

        var updated = await repo.GetByIdAsync(shelfType.Id!.Value);
        Assert.IsNotNull(updated);
        Assert.AreEqual("UpdatedDesc", updated.Description);
    }

    [TestMethod]
    public async Task DeleteAsync_DeletesEntity()
    {
        var dbName = Guid.NewGuid().ToString();
        var ctx = CreateDbContext(dbName);
        var repo = new ShelfTypeRepository(ctx);

        var shelfType = new ShelfType("TypeDel", "DescDel");
        await repo.AddAsync(shelfType);

        var added = await ctx.Set<ShelfType>().FirstOrDefaultAsync(x => x.Name == "TypeDel");
        Assert.IsNotNull(added);

        await repo.DeleteAsync(added.Id!.Value);

        var deleted = await ctx.Set<ShelfType>().FirstOrDefaultAsync(x => x.Name == "TypeDel");
        Assert.IsNull(deleted);
    }
}
