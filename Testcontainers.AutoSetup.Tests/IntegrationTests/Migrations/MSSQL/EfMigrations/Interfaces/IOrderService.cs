using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Entities.OrderAggregate;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Interfaces;

public interface IMSSQLOrderService
{
    Task CreateOrderAsync(int basketId, MSSQLAddress shippingAddress);
}
