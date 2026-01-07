using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations;

public class MySQLCatalogContextFactory : IDesignTimeDbContextFactory<MySQLCatalogContext>
{
    public MySQLCatalogContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MySQLCatalogContext>();
        optionsBuilder.UseMySQL();

        return new MySQLCatalogContext(optionsBuilder.Options);
    }
}
