using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Entities.BasketAggregate;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Interfaces;

public interface IMSSQLBasketService
{
    Task TransferBasketAsync(string anonymousId, string userName);
    Task<MSSQLBasket> AddItemToBasket(string username, int catalogItemId, decimal price, int quantity = 1);
    Task DeleteBasketAsync(int basketId);
}
