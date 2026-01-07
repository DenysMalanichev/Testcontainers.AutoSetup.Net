namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities.BasketAggregate;

public class MySQLBasketItem : MySQLBaseEntity
{

    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public int CatalogItemId { get; private set; }
    public int BasketId { get; private set; }

    public MySQLBasketItem(int catalogItemId, int quantity, decimal unitPrice)
    {
        CatalogItemId = catalogItemId;
        UnitPrice = unitPrice;
    }
}
