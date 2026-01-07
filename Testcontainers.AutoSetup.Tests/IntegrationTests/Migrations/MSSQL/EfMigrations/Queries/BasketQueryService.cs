using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Interfaces;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Queries;

public class BasketQueryService : IMSSQLBasketQueryService
{
    private readonly MSSQLCatalogContext _dbContext;

    public BasketQueryService(MSSQLCatalogContext dbContext)
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
