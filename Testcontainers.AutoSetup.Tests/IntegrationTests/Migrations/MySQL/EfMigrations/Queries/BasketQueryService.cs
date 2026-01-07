using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Interfaces;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Queries;

public class MySQLBasketQueryService : IMySQLBasketQueryService
{
    private readonly MySQLCatalogContext _dbContext;

    public MySQLBasketQueryService(MySQLCatalogContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// This method performs the sum on the database rather than in memory
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    public async Task<int> CountTotalBasketItems(string username)
    {
        var totalItems = await _dbContext.Baskets
            .Where(basket => basket.BuyerId == username)
            .SelectMany(item => item.Items)
            .SumAsync(sum => sum.Quantity);

        return totalItems;
    }
}
