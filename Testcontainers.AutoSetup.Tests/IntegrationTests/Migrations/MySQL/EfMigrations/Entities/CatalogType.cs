using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Interfaces;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities;

public class MySQLCatalogType : MySQLBaseEntity, IMySQLAggregateRoot
{
    public string Type { get; private set; }
    public MySQLCatalogType(string type)
    {
        Type = type;
    }
}
