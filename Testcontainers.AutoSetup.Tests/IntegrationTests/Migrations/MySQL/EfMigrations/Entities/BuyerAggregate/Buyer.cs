using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Interfaces;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Entities.BuyerAggregate;

public class MySQLBuyer : MySQLBaseEntity, IMySQLAggregateRoot
{
    public string IdentityGuid { get; private set; }

    private List<MySQLPaymentMethod> _paymentMethods = new List<MySQLPaymentMethod>();

    public IEnumerable<MySQLPaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

    #pragma warning disable CS8618 // Required by Entity Framework
    private MySQLBuyer() { }

    public MySQLBuyer(string identity) : this()
    {
        IdentityGuid = identity;
    }
}
