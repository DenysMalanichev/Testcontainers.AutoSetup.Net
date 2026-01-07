using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations;

public class MSSQLCatalogContextFactory : IDesignTimeDbContextFactory<MSSQLCatalogContext>
{
    public MSSQLCatalogContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MSSQLCatalogContext>();
        optionsBuilder.UseSqlServer();

        return new MSSQLCatalogContext(optionsBuilder.Options);
    }
}