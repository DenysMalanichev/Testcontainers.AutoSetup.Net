using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Entities.OrderAggregate;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Config;

public class MSSQLOrderConfiguration : IEntityTypeConfiguration<MSSQLOrder>
{
    public void Configure(EntityTypeBuilder<MSSQLOrder> builder)
    {
        var navigation = builder.Metadata.FindNavigation(nameof(MSSQLOrder.OrderItems));

        navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(b => b.BuyerId)
            .IsRequired()
            .HasMaxLength(256);

        builder.OwnsOne(o => o.ShipToAddress, a =>
        {
            a.WithOwner();

            a.Property(a => a.ZipCode)
                .HasMaxLength(18)
                .IsRequired();

            a.Property(a => a.Street)
                .HasMaxLength(180)
                .IsRequired();

            a.Property(a => a.State)
                .HasMaxLength(60);

            a.Property(a => a.Country)
                .HasMaxLength(90)
                .IsRequired();

            a.Property(a => a.City)
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Navigation(x => x.ShipToAddress).IsRequired();
    }
}
