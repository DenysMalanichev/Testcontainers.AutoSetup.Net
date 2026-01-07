using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Entities.OrderAggregate;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Config;

public class MSSQLOrderItemConfiguration : IEntityTypeConfiguration<MSSQLOrderItem>
{
    public void Configure(EntityTypeBuilder<MSSQLOrderItem> builder)
    {
        builder.OwnsOne(i => i.ItemOrdered, io =>
        {
            io.WithOwner();

            io.Property(cio => cio.ProductName)
                .HasMaxLength(50)
                .IsRequired();
        });

        builder.Property(oi => oi.UnitPrice)
            .IsRequired(true)
            .HasColumnType("decimal(18,2)");
    }
}
