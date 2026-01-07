using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Config;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Entities;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Entities.BasketAggregate;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Entities.OrderAggregate;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Interfaces;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations;

public class MSSQLCatalogTenantDependantContext : DbContext
{
#pragma warning disable CS8618 // Required by Entity Framework

    // ITenant is a dummy dependency to test the DI context instantiation
    public MSSQLCatalogTenantDependantContext(
        DbContextOptions<MSSQLCatalogTenantDependantContext> options,
        IMSSQLTenantProvider tenant)
            : base(options) { }

    public DbSet<MSSQLBasket> Baskets { get; set; }
    public DbSet<MSSQLCatalogItem> CatalogItems { get; set; }
    public DbSet<MSSQLCatalogBrand> CatalogBrands { get; set; }
    public DbSet<MSSQLCatalogType> CatalogTypes { get; set; }
    public DbSet<MSSQLOrder> Orders { get; set; }
    public DbSet<MSSQLOrderItem> OrderItems { get; set; }
    public DbSet<MSSQLBasketItem> BasketItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Seed();

        builder.ApplyConfiguration(new MSSQLBasketConfiguration());
        builder.ApplyConfiguration(new MSSQLBasketItemConfiguration());
        builder.ApplyConfiguration(new MSSQLOrderItemConfiguration());
        builder.ApplyConfiguration(new MSSQLCatalogItemConfiguration());
        builder.ApplyConfiguration(new MSSQLCatalogBrandConfiguration());
        builder.ApplyConfiguration(new MSSQLCatalogTypeConfiguration());
        builder.ApplyConfiguration(new MSSQLOrderConfiguration());
    }
}