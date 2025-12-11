using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Interfaces;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Entities;

public class CatalogBrand : BaseEntity, IAggregateRoot
{
    public string Brand { get; private set; }
    public CatalogBrand(string brand)
    {
        Brand = brand;
    }
}
