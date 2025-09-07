using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;
using ShelfMarket.Infrastructure.Repositories;

namespace ShelfMarket.Infrastructure.Tests;

[TestClass]
public class ShelfTypeRepositoryTests
{
    private ShelfMarketDbContext _context;
    private ShelfTypeRepository _repository;

    [TestInitialize]
    public void TestInitialize()
    {
        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");


        var options = new DbContextOptionsBuilder<ShelfMarketDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        _context = new ShelfMarketDbContext(options);

        // Ensure database is created and clean for each test
        _context.Database.EnsureCreated();
        _context.ShelfTypes.RemoveRange(_context.Set<ShelfType>());
        _context.SaveChanges();

        _repository = new ShelfTypeRepository(_context);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _context.ShelfTypes.RemoveRange(_context.Set<ShelfType>());
        _context.SaveChanges();
        _context.Dispose();
    }

    [TestMethod]
    public async Task AddAsync_AddsEntity()
    {
        var shelfType = new ShelfType("Type1", "Description1");

        await _repository.AddAsync(shelfType);

        var result = await _context.Set<ShelfType>().FirstOrDefaultAsync(x => x.Name == "Type1");
        Assert.IsNotNull(result);
        Assert.AreEqual("Description1", result.Description);
    }

    [TestMethod]
    public async Task AddRangeAsync_AddsEntities()
    {
        var shelfTypes = new List<ShelfType>
            {
                new ShelfType("TypeA", "DescA"),
                new ShelfType("TypeB", "DescB")
            };

        await _repository.AddRangeAsync(shelfTypes);

        var count = await _context.Set<ShelfType>().CountAsync();
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public async Task UpdateAsync_UpdatesEntity()
    {
        var shelfType = new ShelfType("TypeX", "DescX");
        await _repository.AddAsync(shelfType);

        shelfType.Description = "UpdatedDesc";
        await _repository.UpdateAsync(shelfType);

        var updated = await _context.Set<ShelfType>().FirstOrDefaultAsync(x => x.Name == "TypeX");
        Assert.IsNotNull(updated);
        Assert.AreEqual("UpdatedDesc", updated.Description);
    }

    [TestMethod]
    public async Task DeleteAsync_DeletesEntity()
    {
        var shelfType = new ShelfType("TypeDel", "DescDel");
        await _repository.AddAsync(shelfType);

        var added = await _context.Set<ShelfType>().FirstOrDefaultAsync(x => x.Name == "TypeDel");
        Assert.IsNotNull(added);

        await _repository.DeleteAsync(added.Id!.Value);

        var deleted = await _context.Set<ShelfType>().FirstOrDefaultAsync(x => x.Name == "TypeDel");
        Assert.IsNull(deleted);
    }
}

