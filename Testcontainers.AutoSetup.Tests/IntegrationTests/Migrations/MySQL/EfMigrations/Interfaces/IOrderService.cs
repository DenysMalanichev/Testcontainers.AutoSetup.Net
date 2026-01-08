using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities.OrderAggregate;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Interfaces;

public interface IMySQLOrderService
{
    Task CreateOrderAsync(int basketId, MySQLAddress shippingAddress);
}
