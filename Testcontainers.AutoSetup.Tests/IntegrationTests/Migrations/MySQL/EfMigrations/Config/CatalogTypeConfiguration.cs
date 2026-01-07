using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Config;

public class MySQLCatalogTypeConfiguration : IEntityTypeConfiguration<MySQLCatalogType>
{
    public void Configure(EntityTypeBuilder<MySQLCatalogType> builder)
    {
        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Id)
           .UseIdentityColumn()
           .IsRequired();

        builder.Property(cb => cb.Type)
            .IsRequired()
            .HasMaxLength(100);
    }
}
