namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Entities.BasketAggregate;

public class MSSQLBasketItem : MSSQLBaseEntity
{
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public int CatalogItemId { get; private set; }
    public int BasketId { get; private set; }

    public MSSQLBasketItem(int catalogItemId, int quantity, decimal unitPrice)
    {
        CatalogItemId = catalogItemId;
        UnitPrice = unitPrice;
    }
}
