using System.Data.Common;
using System.IO.Abstractions;
using Moq;
using Moq.Protected;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.Core.DbSeeding;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Seeders;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class RawSqlDbSeederTests
{
    [Fact]
    public async Task SeedAsync_ExecutesRawSql()
    {
        // Arrange
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.Path.GetFullPath(It.IsAny<string>())).Returns("C:/migrations");
        fileSystemMock.Setup(fs => fs.Path.Combine(It.IsAny<string>(), It.IsAny<string>())).Returns("C:/migrations/script1.sql");
        fileSystemMock.Setup(fs => fs.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync("CREATE TABLE Foo (Id int);");
        var sqlFiles = new List<string> { "script1.sql" };

        var dbSetupMock = new Mock<RawSqlDbSetup>(
            sqlFiles,
            "TestDbName",
            "conn-str",
            "./migrations",
            DbType.Other,
            true,
            null!,
            fileSystemMock.Object
        ) { CallBase = true };

        var mockCommand = new Mock<DbCommand>();
        var capturedSql = string.Empty;
        mockCommand.SetupSet(c => c.CommandText = It.IsAny<string>())
           .Callback<string>(val => capturedSql = val);
        mockCommand.Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(1)
                   .Verifiable();

        var mockConnection = new Mock<DbConnection>();
        mockConnection.Protected()
            .Setup<DbCommand>("CreateDbCommand").Returns(mockCommand.Object);
        mockConnection.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var mockFactory = new Mock<IDbConnectionFactory>();
        mockFactory.Setup(f => f.CreateDbConnection(It.IsAny<string>())).Returns(mockConnection.Object);

        var seeder = new RawSqlDbSeeder(mockFactory.Object, fileSystemMock.Object, null);

        // Act
        await seeder.SeedAsync(dbSetupMock.Object, container: null!);

        // Assert
        mockCommand.Verify(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("CREATE TABLE Foo (Id int);", capturedSql);
    }

    [Fact]
    public async Task SeedAsync_ExecutesRawSqlWithGoChecks_ForMsSqlDb()
    {
        // Arrange
        var sqlFiles = new List<string> { "script1.sql" };
        
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.Path.GetFullPath(It.IsAny<string>())).Returns("C:/migrations");
        fileSystemMock.Setup(fs => fs.Path.Combine(It.IsAny<string>(), It.IsAny<string>())).Returns("C:/migrations/script1.sql");
        fileSystemMock.Setup(fs => fs.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(
                        @"CREATE TABLE Foo (Id int);
                        GO
                        INSERT INTO Foo (Id) VALUES (1);
                        GO");

        var dbSetupMock = new Mock<RawSqlDbSetup>(
            sqlFiles,
            "TestDbName",
            "conn-str",
            "./migrations",
            DbType.MsSQL, // Only MSSQL supports GO syntax 
            true,
            null!,
            fileSystemMock.Object
        ) { CallBase = true };


        var mockCommand = new Mock<DbCommand>();
        var capturedSql = new List<string>();
        mockCommand.SetupSet(c => c.CommandText = It.IsAny<string>())
           .Callback<string>(val => capturedSql.Add(val.Trim()));
        mockCommand.Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(1)
                   .Verifiable();

        var mockConnection = new Mock<DbConnection>();
        mockConnection.Protected()
            .Setup<DbCommand>("CreateDbCommand").Returns(mockCommand.Object);
        mockConnection.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var mockFactory = new Mock<IDbConnectionFactory>();
        mockFactory.Setup(f => f.CreateDbConnection(It.IsAny<string>())).Returns(mockConnection.Object);

        var seeder = new RawSqlDbSeeder(mockFactory.Object, fileSystemMock.Object, null);

        // Act
        await seeder.SeedAsync(dbSetupMock.Object, container: null!);

        // Assert
        mockCommand.Verify(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.Contains("CREATE TABLE Foo (Id int);", capturedSql);
        Assert.Contains("INSERT INTO Foo (Id) VALUES (1);", capturedSql);
    }
}
