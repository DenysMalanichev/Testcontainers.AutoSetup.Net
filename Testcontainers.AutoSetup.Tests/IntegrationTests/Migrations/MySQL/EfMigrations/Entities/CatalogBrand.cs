using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Interfaces;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities;

public class MySQLCatalogBrand : MySQLBaseEntity, IMySQLAggregateRoot
{
    public string Brand { get; private set; }
    public MySQLCatalogBrand(string brand)
    {
        Brand = brand;
    }
}
