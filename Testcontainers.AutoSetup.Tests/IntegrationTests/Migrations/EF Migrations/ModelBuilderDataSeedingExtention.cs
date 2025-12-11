using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Entities;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations;

public static class ModelBuilderExtensions
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CatalogBrand>().HasData(GetPreconfiguredCatalogBrands());
        modelBuilder.Entity<CatalogType>().HasData(GetPreconfiguredCatalogTypes());
        modelBuilder.Entity<CatalogItem>().HasData(GetPreconfiguredItems());
    }

    private static IEnumerable<CatalogBrand> GetPreconfiguredCatalogBrands()
    {
        return new List<CatalogBrand>
        {
            new("Azure") { Id = 1 },
            new(".NET") { Id = 2 },
            new("Visual Studio") { Id = 3 },
            new("SQL Server") { Id = 4 },
            new("Other") { Id = 5 }
        };
    }

    private static IEnumerable<CatalogType> GetPreconfiguredCatalogTypes()
    {
        return new List<CatalogType>
        {
            new("Mug") { Id = 1 },
            new("T-Shirt") { Id = 2 },
            new("Sheet") { Id = 3 },
            new("USB Memory Stick") { Id = 4 }
        };
    }

    private static IEnumerable<CatalogItem> GetPreconfiguredItems()
    {
        return new List<CatalogItem>
        {
            new(2, 2, ".NET Bot Black Sweatshirt", ".NET Bot Black Sweatshirt", 19.5M, "http://catalogbaseurltobereplaced/images/products/1.png") { Id = 1 },
            new(1, 2, ".NET Black & White Mug", ".NET Black & White Mug", 8.50M, "http://catalogbaseurltobereplaced/images/products/2.png") { Id = 2 },
            new(2, 5, "Prism White T-Shirt", "Prism White T-Shirt", 12, "http://catalogbaseurltobereplaced/images/products/3.png") { Id = 3 },
            new(2, 2, ".NET Foundation Sweatshirt", ".NET Foundation Sweatshirt", 12, "http://catalogbaseurltobereplaced/images/products/4.png") { Id = 4 },
            new(3, 5, "Roslyn Red Sheet", "Roslyn Red Sheet", 8.5M, "http://catalogbaseurltobereplaced/images/products/5.png") { Id = 5 },
            new(2, 2, ".NET Blue Sweatshirt", ".NET Blue Sweatshirt", 12, "http://catalogbaseurltobereplaced/images/products/6.png") { Id = 6 },
            new(2, 5, "Roslyn Red T-Shirt", "Roslyn Red T-Shirt", 12, "http://catalogbaseurltobereplaced/images/products/7.png") { Id = 7 },
            new(2, 5, "Kudu Purple Sweatshirt", "Kudu Purple Sweatshirt", 8.5M, "http://catalogbaseurltobereplaced/images/products/8.png") { Id = 8 },
            new(1, 5, "Cup<T> White Mug", "Cup<T> White Mug", 12, "http://catalogbaseurltobereplaced/images/products/9.png") { Id = 9 },
            new(3, 2, ".NET Foundation Sheet", ".NET Foundation Sheet", 12, "http://catalogbaseurltobereplaced/images/products/10.png") { Id = 10 },
            new(3, 2, "Cup<T> Sheet", "Cup<T> Sheet", 8.5M, "http://catalogbaseurltobereplaced/images/products/11.png") { Id = 11 },
            new(2, 5, "Prism White TShirt", "Prism White TShirt", 12, "http://catalogbaseurltobereplaced/images/products/12.png") { Id = 12 }
        };
    }
}