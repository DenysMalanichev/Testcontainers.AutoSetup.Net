using System.Data.Common;
using System.IO.Abstractions;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;
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

        var dbSetupMock = new Mock<DbSetup>( 
            "dbName",
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            "restorationPath",
            new Mock<IFileSystem>().Object
        );
        var containerMock = new Mock<IContainer>();
        
        var msSqlRestorer = new MsSqlDbRestorer(
            dbSetup: dbSetupMock.Object,
            container: containerMock.Object,
            dbConnectionFactory: connectionFactoryMock.Object,
            logger: Mock.Of<ILogger>());

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
        
        var dbSetupMock = new Mock<DbSetup>( 
            "dbName",
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            "restorationPath",
            new Mock<IFileSystem>().Object
        ) { CallBase = true };

        var msSqlRestorer = new MsSqlDbRestorer(
            dbSetup: dbSetupMock.Object,
            container: containerMock.Object,
            dbConnectionFactory: connectionFactoryMock.Object,
            logger: Mock.Of<ILogger>());

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
        
        var dbSetupMock = new Mock<DbSetup>( 
            "dbName",
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            "restorationPath",
            new Mock<IFileSystem>().Object
        );
        

        var msSqlRestorer = new MsSqlDbRestorer(
            dbSetupMock.Object,
            Mock.Of<IContainer>(),
            Mock.Of<IDbConnectionFactory>(),
            Mock.Of<ILogger>());

        // Act & Assert
        await Assert.ThrowsAsync<ExecFailedException>(async () => await msSqlRestorer.SnapshotAsync());
    }

    [Fact]
    public async Task MsSqlDbRestorer_IsSnapshotUpToDateAsyncFalse_IfMountCheckFailed()
    {
        // Arrange
        var containerMock = new Mock<IContainer>();
        const string restorationPath = "/tmp/missing-mount";

        containerMock.Setup(c => c.ExecAsync(It.Is<IList<string>>(args => 
            args.Contains($"findmnt {restorationPath}")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 1));
        
        containerMock.Setup(
            c => c.ExecAsync(It.Is<IList<string>>(
                args => args.Any(arg => arg.StartsWith($"ls {restorationPath}"))), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 0));

        var dbSetupMock = new Mock<DbSetup>( 
            "dbName",
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            restorationPath,
            new Mock<IFileSystem>().Object
        ) { CallBase = true };

        var msSqlRestorer = new MsSqlDbRestorer(
            dbSetupMock.Object,
            containerMock.Object,
            Mock.Of<IDbConnectionFactory>(),
            Mock.Of<ILogger>());

        // Act 
        var result = await msSqlRestorer.IsSnapshotUpToDateAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsMountExistsAsync_ThrowsExecFailedException_IfFailedCheckForMount()
    {
        // Arrange
        var containerMock = new Mock<IContainer>();
        const string restorationPath = "/tmp/missing-mount";

        containerMock.Setup(c => c.ExecAsync(It.Is<IList<string>>(args => 
            args.Contains($"findmnt {restorationPath}")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 100));
        
        containerMock.Setup(
            c => c.ExecAsync(It.Is<IList<string>>(
                args => args.Any(arg => arg.StartsWith($"ls {restorationPath}"))), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 0));

        var dbSetupMock = new Mock<DbSetup>( 
            "dbName",
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            restorationPath,
            new Mock<IFileSystem>().Object
        ) { CallBase = true };

        var msSqlRestorer = new MsSqlDbRestorer(
            dbSetupMock.Object,
            containerMock.Object,
            Mock.Of<IDbConnectionFactory>(),
            Mock.Of<ILogger>());

        // Act 
        await Assert.ThrowsAsync<ExecFailedException>(async () => 
            await msSqlRestorer.IsSnapshotUpToDateAsync());
    }

    [Fact]
    public async Task IsSnapshotValidAsync_ThrowsExecFailedException_IfSnapshotCheckFailed()
    {
        // Arrange
        var containerMock = new Mock<IContainer>();
        const string restorationPath = "/tmp/missing-mount";
        
        containerMock.Setup(
            c => c.ExecAsync(It.Is<IList<string>>(
                args => args.Any(arg => arg.StartsWith($"ls {restorationPath}"))), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 100));

        var dbSetupMock = new Mock<DbSetup>( 
            "dbName",
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            restorationPath,
            new Mock<IFileSystem>().Object
        ) { CallBase = true };

        var msSqlRestorer = new MsSqlDbRestorer(
            dbSetupMock.Object,
            containerMock.Object,
            Mock.Of<IDbConnectionFactory>(),
            Mock.Of<ILogger>());

        // Act 
        await Assert.ThrowsAsync<ExecFailedException>(async () => 
            await msSqlRestorer.IsSnapshotUpToDateAsync());
    }

    [Fact]
    public async Task IsSnapshotUpToDateAsyncc_ReturnsTrue_IfSnapshotAndMountsChecksSucceeded()
    {
        // Arrange
        var containerMock = new Mock<IContainer>();
        const string restorationPath = "/tmp/missing-mount";
        
        containerMock.Setup(
            c => c.ExecAsync(It.Is<IList<string>>(
                args => args.Any(arg => arg.StartsWith($"ls {restorationPath}"))), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 0));

                containerMock.Setup(c => c.ExecAsync(It.Is<IList<string>>(args => 
            args.Contains($"findmnt {restorationPath}")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 0));

        var dbSetupMock = new Mock<DbSetup>( 
            "dbName",
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            restorationPath,
            new Mock<IFileSystem>().Object
        ) { CallBase = true };

        var msSqlRestorer = new MsSqlDbRestorer(
            dbSetupMock.Object,
            containerMock.Object,
            Mock.Of<IDbConnectionFactory>(),
            Mock.Of<ILogger>());

        // Act 
        Assert.True(await msSqlRestorer.IsSnapshotUpToDateAsync());
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
