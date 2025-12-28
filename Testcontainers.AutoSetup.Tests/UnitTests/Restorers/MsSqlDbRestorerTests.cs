using System.ComponentModel;
using System.Data.Common;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Moq;
using Moq.Protected;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.DbRestoration;
using Testcontainers.AutoSetup.Tests.TestCollections;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Restorers;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class MsSqlDbRestorerTests
{
    [Fact]
    public async Task RestoreAsync_RethrowsSqlException_IfSnapshotInitializationFailed()
    {
        // Arrange
        var commandMock = new Mock<DbCommand>();
        commandMock.Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(MakeSqlException());
        var connectionMock = new Mock<DbConnection>();
        connectionMock.Protected()
            .Setup<DbCommand>("CreateDbCommand")
            .Returns(commandMock.Object);
        var connectionFactoryMock = new Mock<IDbConnectionFactory>();
        connectionFactoryMock.Setup(f => f.CreateDbConnection(It.IsAny<string>()))
            .Returns(connectionMock.Object);

        var dbSetupMock = new Mock<DbSetup>();
        dbSetupMock.SetupGet(d => d.DbName).Returns("TestDb");
        var containerMock = new Mock<IContainer>();
        
        var msSqlRestorer = new MsSqlDbRestorer(
            dbSetup: dbSetupMock.Object,
            container: containerMock.Object,
            containerConnectionString: "Server=localhost;Database=master;User Id=sa;Password=Password123;",
            dbConnectionFactory: connectionFactoryMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(async () => await msSqlRestorer.RestoreAsync());
    }

    [Fact]
    public async Task SnapshotAsync_ExecutesSnapshotCommand()
    {
        // Arrange
        var containerMock = new Mock<IContainer>();
        containerMock.Setup(c => c.ExecAsync(It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(stderr: string.Empty, exitCode: 0, stdout: string.Empty));

        var commandMock = new Mock<DbCommand>();
        commandMock.Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        var connectionMock = new Mock<DbConnection>();
        connectionMock.Protected()
            .Setup<DbCommand>("CreateDbCommand")
            .Returns(commandMock.Object);
        connectionMock.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var connectionFactoryMock = new Mock<IDbConnectionFactory>();
        connectionFactoryMock.Setup(f => f.CreateDbConnection(It.IsAny<string>()))
            .Returns(connectionMock.Object);
        
        var msSqlRestorer = new MsSqlDbRestorer(
            dbSetup: Mock.Of<DbSetup>(),
            container: containerMock.Object,
            containerConnectionString: "Server=localhost;Database=master;User Id=sa;Password=Password123;",
            dbConnectionFactory: connectionFactoryMock.Object);

        // Act
        await  msSqlRestorer.SnapshotAsync();

        // Assert
        commandMock.Verify(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SnapshotAsync_ThrowsExecFailedException_IfRestorationDirectoryDoesntExist()
    {
        // Arrange
        var containerMock = new Mock<IContainer>();
        containerMock.Setup(c => c.ExecAsync(It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(stderr: "Directory does not exist", exitCode: 1, stdout: string.Empty));
        
        var msSqlRestorer = new MsSqlDbRestorer(
            dbSetup: Mock.Of<DbSetup>(),
            container: containerMock.Object,
            containerConnectionString: "Server=localhost;Database=master;User Id=sa;Password=Password123;",
            dbConnectionFactory: Mock.Of<IDbConnectionFactory>());

        // Act & Assert
        await Assert.ThrowsAsync<ExecFailedException>(async () => await msSqlRestorer.SnapshotAsync());
    }

    private static SqlException MakeSqlException() {
        SqlException exception = null!;
        try {
            SqlConnection conn = new SqlConnection(@"Data Source=.;Database=GUARANTEED_TO_FAIL;Connection Timeout=1");
            conn.Open();
        } catch(SqlException ex) {
            exception = ex;
        }
        return exception;
    }
}
