namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Entities.BasketAggregate;

public class BasketItem : BaseEntity
{

    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public int CatalogItemId { get; private set; }
    public int BasketId { get; private set; }

    public BasketItem(int catalogItemId, int quantity, decimal unitPrice)
    {
        CatalogItemId = catalogItemId;
        UnitPrice = unitPrice;
    }
}
