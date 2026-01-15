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
        (
            dbName: "TestDb",
            migrationsPath: "./migrations",
            containerConnectionString: "test-connection-string",
            sqlFiles: sqlFiles
        );
        // Assert
        Assert.Equal(sqlFiles, sut.SqlFiles);
    }

    [Fact]
    public async Task RawSqlDbSetup_ThrowsArgumentException_IfSqlFilesListIsNull()
    {
        // Arrange
        List<string> sqlFiles = null!;

        // Act && Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            new RawSqlDbSetup(
                dbName: "TestDb",
                migrationsPath: "./migrations",
                containerConnectionString: "test-connection-string",
                sqlFiles: sqlFiles
            ));
    }

    [Fact]
    public async Task RawSqlDbSetup_ThrowsArgumentException_IfSqlFilesListIsEmpty()
    {
        // Arrange
        List<string> sqlFiles = [];

        // Act && Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            new RawSqlDbSetup(
                dbName: "TestDb",
                migrationsPath: "./migrations",
                containerConnectionString: "test-connection-string",
                sqlFiles: sqlFiles
            ));
    }
}
