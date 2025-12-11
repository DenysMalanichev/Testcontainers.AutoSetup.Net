using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Entities.BasketAggregate;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Interfaces;

public interface IBasketService
{
    Task TransferBasketAsync(string anonymousId, string userName);
    Task<Basket> AddItemToBasket(string username, int catalogItemId, decimal price, int quantity = 1);
    Task DeleteBasketAsync(int basketId);
}
