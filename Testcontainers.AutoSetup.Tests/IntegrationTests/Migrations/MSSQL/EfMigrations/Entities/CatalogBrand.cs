using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Interfaces;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Entities;

public class MSSQLCatalogBrand : MSSQLBaseEntity, IMSSQLAggregateRoot
{
    public string Brand { get; private set; }
    public MSSQLCatalogBrand(string brand)
    {
        Brand = brand;
    }
}
