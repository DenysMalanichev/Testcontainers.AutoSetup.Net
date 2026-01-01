using Testcontainers.AutoSetup.Core.Common.SqlDbHelpers;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Helpers;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class SqlDbConnectionFactoryTests
{
    [Fact]
    public void CreateDbConnection_ReturnsSqlConnectionInstance()
    {
        // Arrange
        var connectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
        var factory = new SqlDbConnectionFactory();

        // Act
        var dbConnection = factory.CreateDbConnection(connectionString);

        // Assert
        Assert.NotNull(dbConnection);
        Assert.IsType<Microsoft.Data.SqlClient.SqlConnection>(dbConnection);
        Assert.Equal(connectionString, dbConnection.ConnectionString);
    }
}