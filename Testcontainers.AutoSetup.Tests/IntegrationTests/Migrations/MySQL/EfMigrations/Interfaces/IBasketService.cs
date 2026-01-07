using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities.BasketAggregate;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Interfaces;

public interface IMySQLBasketService
{
    Task TransferBasketAsync(string anonymousId, string userName);
    Task<MySQLBasket> AddItemToBasket(string username, int catalogItemId, decimal price, int quantity = 1);
    Task DeleteBasketAsync(int basketId);
}
