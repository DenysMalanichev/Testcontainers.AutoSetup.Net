using System.IO.Abstractions;
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
        public TestDbSetup(string dbName, string containerConnectionString, string migrationsPath, DbType dbType = DbType.Other, bool restoreFromDump = false, string? restorationStateFilesDirectory = null, IFileSystem? fileSystem = null) : base(dbName, containerConnectionString, migrationsPath, dbType, restoreFromDump, restorationStateFilesDirectory, fileSystem)
        {
        }

        public override string BuildDbConnectionString() 
            => "containerConnStr";
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
        ( 
            dbName: dbName,
            migrationsPath: migrationPath,
            containerConnectionString: testConnStr,
            dbType: DbType.MsSQL
        );
        // Assert
        Assert.Equal(dbName, sut.DbName);
        Assert.Equal(migrationPath, sut.MigrationsPath);
    }

    [Fact]
    public void DbSetup_HasCorrectDefaultValues()
    {
        // Arrange & Act
        var sut = new TestDbSetup 
        ( 
            dbName: "DefaultTest",
            migrationsPath: "./",
            containerConnectionString: "default-connection-string"
        );

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
        (
            dbName: "OverrideTest",
            migrationsPath: "./",
            containerConnectionString: "default-connection-string",
            dbType: specificType,
            restoreFromDump: true
        );

        // Assert
        Assert.Equal(DbType.MsSQL, sut.DbType);
        Assert.True(sut.RestoreFromDump);
    }
}