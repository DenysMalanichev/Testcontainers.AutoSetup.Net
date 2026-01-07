using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Entities.BasketAggregate;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Config;

public class MSSQLBasketItemConfiguration : IEntityTypeConfiguration<MSSQLBasketItem>
{
    public void Configure(EntityTypeBuilder<MSSQLBasketItem> builder)
    {
        builder.Property(bi => bi.UnitPrice)
            .IsRequired(true)
            .HasColumnType("decimal(18,2)");
    }
}
