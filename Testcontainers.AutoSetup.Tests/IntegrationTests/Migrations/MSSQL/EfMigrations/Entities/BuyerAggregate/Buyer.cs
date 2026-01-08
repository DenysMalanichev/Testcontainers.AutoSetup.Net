using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Interfaces;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Entities.BuyerAggregate;

public class MSSQLBuyer : MSSQLBaseEntity, IMSSQLAggregateRoot
{
    public string IdentityGuid { get; private set; }

    private List<MSSQLPaymentMethod> _paymentMethods = new List<MSSQLPaymentMethod>();

    public IEnumerable<MSSQLPaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

    #pragma warning disable CS8618 // Required by Entity Framework
    private MSSQLBuyer() { }

    public MSSQLBuyer(string identity) : this()
    {
        IdentityGuid = identity;
    }
}
