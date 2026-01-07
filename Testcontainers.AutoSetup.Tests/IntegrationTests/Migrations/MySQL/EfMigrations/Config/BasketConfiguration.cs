using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities.BasketAggregate;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Config;

public class MySQLBasketConfiguration : IEntityTypeConfiguration<MySQLBasket>
{
    public void Configure(EntityTypeBuilder<MySQLBasket> builder)
    {
        var navigation = builder.Metadata.FindNavigation(nameof(MySQLBasket.Items));
        navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(b => b.BuyerId)
            .IsRequired()
            .HasMaxLength(256);
    }
}
