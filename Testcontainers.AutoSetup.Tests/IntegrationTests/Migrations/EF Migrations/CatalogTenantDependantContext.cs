using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Entities;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Entities.BasketAggregate;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Entities.OrderAggregate;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Interfaces;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations;

public class CatalogTenantDependantContext : DbContext
{
#pragma warning disable CS8618 // Required by Entity Framework

    // ITenant is a dummy dependency to test the DI context instantiation
    public CatalogTenantDependantContext(
        DbContextOptions<CatalogTenantDependantContext> options,
        ITenantProvider tenant)
            : base(options) { }

    public DbSet<Basket> Baskets { get; set; }
    public DbSet<CatalogItem> CatalogItems { get; set; }
    public DbSet<CatalogBrand> CatalogBrands { get; set; }
    public DbSet<CatalogType> CatalogTypes { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<BasketItem> BasketItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Seed();

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}