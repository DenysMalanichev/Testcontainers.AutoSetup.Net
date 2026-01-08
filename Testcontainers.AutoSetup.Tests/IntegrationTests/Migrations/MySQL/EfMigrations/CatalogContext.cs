using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Config;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities.BasketAggregate;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities.OrderAggregate;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations;

public class MySQLCatalogContext : DbContext
{
    #pragma warning disable CS8618 // Required by Entity Framework
    public MySQLCatalogContext(DbContextOptions<MySQLCatalogContext> options) : base(options) {}

    public DbSet<MySQLBasket> Baskets { get; set; }
    public DbSet<MySQLCatalogItem> CatalogItems { get; set; }
    public DbSet<MySQLCatalogBrand> CatalogBrands { get; set; }
    public DbSet<MySQLCatalogType> CatalogTypes { get; set; }
    public DbSet<MySQLOrder> Orders { get; set; }
    public DbSet<MySQLOrderItem> OrderItems { get; set; }
    public DbSet<MySQLBasketItem> BasketItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Seed();

        builder.ApplyConfiguration(new MySQLBasketConfiguration());
        builder.ApplyConfiguration(new MySQLBasketItemConfiguration());
        builder.ApplyConfiguration(new MySQLOrderItemConfiguration());
        builder.ApplyConfiguration(new MySQLCatalogItemConfiguration());
        builder.ApplyConfiguration(new MySQLCatalogBrandConfiguration());
        builder.ApplyConfiguration(new MySQLCatalogTypeConfiguration());
        builder.ApplyConfiguration(new MySQLOrderConfiguration());
    }
}
