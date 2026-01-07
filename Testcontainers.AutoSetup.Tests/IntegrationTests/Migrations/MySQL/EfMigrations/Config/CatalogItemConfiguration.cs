using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Config;

public class MySQLCatalogItemConfiguration : IEntityTypeConfiguration<MySQLCatalogItem>
{
    public void Configure(EntityTypeBuilder<MySQLCatalogItem> builder)
    {
        builder.ToTable("Catalog");

        builder.Property(ci => ci.Id)
            .UseIdentityColumn()
            .IsRequired();

        builder.Property(ci => ci.Name)
            .IsRequired(true)
            .HasMaxLength(50);

        builder.Property(ci => ci.Price)
            .IsRequired(true)
            .HasColumnType("decimal(18,2)");

        builder.Property(ci => ci.PictureUri)
            .IsRequired(false);

        builder.HasOne(ci => ci.CatalogBrand)
            .WithMany()
            .HasForeignKey(ci => ci.CatalogBrandId);

        builder.HasOne(ci => ci.CatalogType)
            .WithMany()
            .HasForeignKey(ci => ci.CatalogTypeId);
    }
}
