namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities.OrderAggregate;

public class MySQLOrderItem : MySQLBaseEntity
{
    public MySQLCatalogItemOrdered ItemOrdered { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Units { get; private set; }

    #pragma warning disable CS8618 // Required by Entity Framework
    private MySQLOrderItem() {}

    public MySQLOrderItem(MySQLCatalogItemOrdered itemOrdered, decimal unitPrice, int units)
    {
        ItemOrdered = itemOrdered;
        UnitPrice = unitPrice;
        Units = units;
    }
}
