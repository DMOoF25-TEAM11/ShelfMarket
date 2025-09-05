using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Tests;

[TestClass]
public sealed class DbContextConnectionTest
{
    [TestMethod]
    public async Task ConnectionString_CanConnectToDatabase()
    {
        // Load configuration (ensure appsettings.json is copied to output directory)
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Use the production connection string (change to "ShelfMarketDb_Development" if needed)
        var connectionString = config.GetConnectionString("ShelfMarketDb_Development");

        var options = new DbContextOptionsBuilder<ShelfMarketDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new ShelfMarketDbContext(options);

        // Act
        var canConnect = await context.Database.CanConnectAsync();

        // Assert
        Assert.IsTrue(canConnect, "Unable to connect to the database with the provided connection string.");
    }
}
