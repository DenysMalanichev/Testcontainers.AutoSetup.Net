using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Entities;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class RawSqlDbSetupTests
{
    [Fact]
    public void RawSqlDbSetup_SetsSqlFilesPropertyCorrectly()
    {
        // Arrange
        var sqlFiles = new List<string> { "script1.sql", "script2.sql", "script3.sql" };

        // Act
        var sut = new RawSqlDbSetup
        {
            DbName = "TestDb",
            MigrationsPath = "./migrations",
            ContainerConnectionString = "test-connection-string",
            SqlFiles = sqlFiles
        };

        // Assert
        Assert.Equal(sqlFiles, sut.SqlFiles);
    }
}
