using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Interfaces;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Entities;

public class MSSQLCatalogType : MSSQLBaseEntity, IMSSQLAggregateRoot
{
    public string Type { get; private set; }
    public MSSQLCatalogType(string type)
    {
        Type = type;
    }
}
