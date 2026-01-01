using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Entities;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class DbSetupTests
{
    private record TestDbSetup : DbSetup
    {
        public override string BuildDbConnectionString() 
            => "containerConnStr";

        public override Task<DateTime> GetMigrationsLastModificationDateAsync(CancellationToken cancellationToken = default) 
            => Task.FromResult(DateTime.MinValue);
    }

    [Fact]
    public void DbSetup_SetsRequiredPropertiesCorrectly()
    {
        // Arrange
        const string dbName = "TestDb";
        const string migrationPath = "./migrations";
        const string testConnStr = "test-connection-string";

        // Act
        var sut = new TestDbSetup 
        { 
            DbName = dbName, 
            MigrationsPath = migrationPath,
            ContainerConnectionString = testConnStr,
        };

        // Assert
        Assert.Equal(dbName, sut.DbName);
        Assert.Equal(migrationPath, sut.MigrationsPath);
    }

    [Fact]
    public void DbSetup_HasCorrectDefaultValues()
    {
        // Arrange & Act
        var sut = new TestDbSetup 
        { 
            DbName = "DefaultTest", 
            MigrationsPath = "./",
            ContainerConnectionString = "default-connection-string",
        };

        // Assert
        Assert.Equal(DbType.Other, sut.DbType);
        Assert.False(sut.RestoreFromDump);
    }

    [Fact]
    public void DbSetup_AllowsOverridingDefaults()
    {
        // Arrange
        var specificType = DbType.MsSQL;
        
        // Act
        var sut = new TestDbSetup 
        { 
            DbName = "OverrideTest", 
            MigrationsPath = "./",
            ContainerConnectionString = "default-connection-string",
            DbType = specificType,
            RestoreFromDump = true
        };

        // Assert
        Assert.Equal(DbType.MsSQL, sut.DbType);
        Assert.True(sut.RestoreFromDump);
    }
}