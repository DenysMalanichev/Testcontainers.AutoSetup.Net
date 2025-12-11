using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Entities.OrderAggregate;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Interfaces;

public interface IOrderService
{
    Task CreateOrderAsync(int basketId, Address shippingAddress);
}
