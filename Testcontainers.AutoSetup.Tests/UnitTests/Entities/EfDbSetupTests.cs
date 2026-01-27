using System.IO.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.EntityFramework.Entities;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Entities;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class EfDbSetupTests
{
    [Fact]
    public void Constructor_SetsProperties_WhenArgumentsAreValid()
    {
        // Arrange
        var contextMock = new Mock<DbContext>();
        Func<string, DbContext> factory = (str) => contextMock.Object;
        
        var dbName = "TestDb";
        var connStr = "mongodb://localhost";
        var migrationPath = "./migrations";
        var fileSystemMock = new Mock<IFileSystem>();

        // Act
        var sut = new EfDbSetup(
            factory,
            dbName,
            connStr,
            migrationPath,
            DbType.MongoDB,
            true,
            "./state",
            fileSystemMock.Object
        );

        // Assert
        Assert.Equal(dbName, sut.DbName);
        Assert.Equal(connStr, sut.ContainerConnectionString);
        Assert.Equal(migrationPath, sut.MigrationsPath);
        Assert.Equal(DbType.MongoDB, sut.DbType);
        Assert.True(sut.RestoreFromDump);
        
        // Verify the factory actually works as expected
        var createdContext = sut.ContextFactory("test-connection");
        Assert.Same(contextMock.Object, createdContext);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenContextFactoryIsNull()
    {
        // Arrange
        Func<string, DbContext> nullFactory = null!;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new EfDbSetup(
            nullFactory,
            "TestDb",
            "conn-str",
            "./migrations"
        ));

        Assert.Equal("contextFactory", ex.ParamName);
    }

    [Fact]
    public void Constructor_AcceptsNullOptionalParameters()
    {
        // Arrange
        var contextMock = new Mock<DbContext>();
        Func<string, DbContext> factory = (str) => contextMock.Object;

        // Act
        var sut = new EfDbSetup(
            factory,
            "TestDb",
            "conn-str",
            "./migrations",
            fileSystem: null // Explicitly testing null IFileSystem
        );

        // Assert
        Assert.NotNull(sut);
    }
}
