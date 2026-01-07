using Testcontainers.AutoSetup.Tests.IntegrationTests.TestHelpers;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Helpers;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class MsSqlDbConnectionFactoryTests
{
    [Fact]
    public void CreateDbConnection_ReturnsSqlConnectionInstance()
    {
        // Arrange
        var connectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
        var factory = new MsSqlDbConnectionFactory();

        // Act
        var dbConnection = factory.CreateDbConnection(connectionString);

        // Assert
        Assert.NotNull(dbConnection);
        Assert.IsType<Microsoft.Data.SqlClient.SqlConnection>(dbConnection);
        Assert.Equal(connectionString, dbConnection.ConnectionString);
    }
}