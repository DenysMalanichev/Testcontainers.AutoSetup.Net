namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Entities.OrderAggregate;

/// <summary>
/// Represents a snapshot of the item that was ordered. If catalog item details change, details of
/// the item that was part of a completed order should not change.
/// </summary>
public class CatalogItemOrdered // ValueObject
{
    public CatalogItemOrdered(int catalogItemId, string productName, string pictureUri)
    {
        CatalogItemId = catalogItemId;
        ProductName = productName;
        PictureUri = pictureUri;
    }

    #pragma warning disable CS8618 // Required by Entity Framework
    private CatalogItemOrdered() {}

    public int CatalogItemId { get; private set; }
    public string ProductName { get; private set; }
    public string PictureUri { get; private set; }
}
