using Microsoft.EntityFrameworkCore;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;
using ShelfMarket.Infrastructure.Repositories;

namespace ShelfMarket.Infrastructure.Tests;

[TestClass]
public class ShelfRepositoryTests
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

    private static ShelfRepository CreateRepository(string dbName) =>
        new ShelfRepository(CreateDbContext(dbName));

    [TestMethod]
    public async Task AddAsync_AddsEntity()
    {
        var dbName = Guid.NewGuid().ToString();
        var repo = CreateRepository(dbName);

        var shelf = new Shelf
        {
            Id = Guid.NewGuid(),
            Number = 1,
            ShelfTypeId = Guid.NewGuid(),
            LocationX = 2,
            LocationY = 3,
            OrientationHorizontal = true
        };

        await repo.AddAsync(shelf, CancellationToken.None);

        await using var assertCtx = CreateDbContext(dbName);
        var stored = await assertCtx.Shelves.SingleAsync();
        Assert.AreEqual(shelf.Id, stored.Id);
        Assert.AreEqual((int)2, (int)stored.LocationX);
        Assert.AreEqual((int)3, (int)stored.LocationY);
        Assert.IsTrue(stored.OrientationHorizontal);
    }

    [TestMethod]
    public async Task AddRangeAsync_AddsEntities()
    {
        var dbName = Guid.NewGuid().ToString();
        var repo = CreateRepository(dbName);

        var shelves = new List<Shelf>
            {
                new Shelf { Id = Guid.NewGuid(), Number = 1, ShelfTypeId = Guid.NewGuid(), LocationX = 1, LocationY = 1, OrientationHorizontal = true },
                new Shelf { Id = Guid.NewGuid(), Number = 2, ShelfTypeId = Guid.NewGuid(), LocationX = 3, LocationY = 1, OrientationHorizontal = false }
            };

        await repo.AddRangeAsync(shelves, CancellationToken.None);

        await using var assertCtx = CreateDbContext(dbName);
        var count = await assertCtx.Shelves.CountAsync();
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public async Task UpdateAsync_UpdatesEntity()
    {
        var dbName = Guid.NewGuid().ToString();
        var repo = CreateRepository(dbName);

        var shelf = new Shelf
        {
            Id = Guid.NewGuid(),
            Number = 10,
            ShelfTypeId = Guid.NewGuid(),
            LocationX = 5,
            LocationY = 5,
            OrientationHorizontal = true
        };

        await repo.AddAsync(shelf, CancellationToken.None);

        shelf.Number = 11;
        shelf.LocationX = 6;
        shelf.OrientationHorizontal = false;

        await repo.UpdateAsync(shelf, CancellationToken.None);

        await using var assertCtx = CreateDbContext(dbName);
        var stored = await assertCtx.Shelves.AsNoTracking().SingleAsync(s => s.Id == shelf.Id);
        Assert.AreEqual(11, stored.Number);
        Assert.AreEqual(6, stored.LocationX);
        Assert.IsFalse(stored.OrientationHorizontal);
    }

    [TestMethod]
    public async Task DeleteAsync_DeletesEntity()
    {
        var dbName = Guid.NewGuid().ToString();
        var repo = CreateRepository(dbName);

        var shelf = new Shelf
        {
            Id = Guid.NewGuid(),
            Number = 3,
            ShelfTypeId = Guid.NewGuid(),
            LocationX = 7,
            LocationY = 7,
            OrientationHorizontal = true
        };

        await repo.AddAsync(shelf, CancellationToken.None);
        await repo.DeleteAsync(shelf.Id!.Value, CancellationToken.None);

        await using var assertCtx = CreateDbContext(dbName);
        var any = await assertCtx.Shelves.AnyAsync();
        Assert.IsFalse(any);
    }

    [TestMethod]
    public async Task IsLocationFreeAsync()
    {
        var dbName = Guid.NewGuid().ToString();
        var repo = CreateRepository(dbName);

        // Seed an existing horizontal shelf occupying (5,5) and (6,5)
        var existing = new Shelf
        {
            Id = Guid.NewGuid(),
            Number = 20,
            ShelfTypeId = Guid.NewGuid(),
            LocationX = 5,
            LocationY = 5,
            OrientationHorizontal = true
        };
        await repo.AddAsync(existing, CancellationToken.None);

        // Overlaps with existing anchor
        var free1 = await repo.IsLocationFreeAsync(5, 5, true, CancellationToken.None);
        Assert.IsFalse(free1);

        // Overlaps with existing second cell
        var free2 = await repo.IsLocationFreeAsync(6, 5, true, CancellationToken.None);
        Assert.IsFalse(free2);

        // Free spot (not locked and no overlap)
        var free3 = await repo.IsLocationFreeAsync(10, 10, true, CancellationToken.None);
        Assert.IsTrue(free3);

        // Locked start cell (in range Y:0-4, X:11-18) -> false
        var lockedStart = await repo.IsLocationFreeAsync(11, 0, true, CancellationToken.None);
        Assert.IsFalse(lockedStart);

        // Second cell locked -> false
        var secondLocked = await repo.IsLocationFreeAsync(10, 0, true, CancellationToken.None); // second is (11,0)
        Assert.IsFalse(secondLocked);
    }
}
