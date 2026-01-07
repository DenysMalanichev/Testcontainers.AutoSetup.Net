using DotNet.Testcontainers.Containers;
using Moq;
using Moq.Protected;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class DbRestorerTests
{
    [Fact]
    public void RestorationStateFilesPath_ReturnsNull_IfSnapshotNameIsNotSet()
    {
        // Arrange
        const string restorationStateFilesDirectory = "/test/path";
        var dbSetupMock = new Mock<DbSetup>("dbName", "containerConnectionString", "migrationsPath", DbType.Other, true, restorationStateFilesDirectory, null!);
        var containerMock = new Mock<IContainer>();
        var dbRestorer = new TestDbRestorer(dbSetupMock.Object, containerMock.Object);

        // Act & Assert
        Assert.Null(dbRestorer.RestorationStateFilesPath);
    }

    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfDbSetupIsNull()
    {
        // Arrange
        DbSetup dbSetupMock = null!;
        var containerMock = new Mock<IContainer>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TestDbRestorer(dbSetupMock, containerMock.Object));
    }

    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfContainerIsNull()
    {
        // Arrange
        const string restorationStateFilesDirectory = "/test/path";
        var dbSetupMock = new Mock<DbSetup>("dbName", "containerConnectionString", "migrationsPath", DbType.Other, true, restorationStateFilesDirectory, null!)
        {
            CallBase = true
        };
        dbSetupMock.Setup(ds => ds.RestorationStateFilesDirectory).Returns(restorationStateFilesDirectory);
        IContainer containerMock = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TestDbRestorer(dbSetupMock.Object, containerMock));
    }


    [Fact]
    public async Task EnsureRestorationDirectoryExistsAsync_ThrowsExecFailedException_IfCommandExecutionFinishedWithNonZeroCode()
    {
        // Arrange
        const string restorationStateFilesDirectory = "/test/path";
        var dbSetupMock = new Mock<DbSetup>("dbName", "containerConnectionString", "migrationsPath", DbType.Other, true, restorationStateFilesDirectory, null!)
        {
            CallBase = true
        };
        dbSetupMock.Setup(ds => ds.RestorationStateFilesDirectory).Returns(restorationStateFilesDirectory);
        var containerMock = new Mock<IContainer>();
        containerMock.Setup(c => c.ExecAsync(It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(stdout: string.Empty, stderr: string.Empty, exitCode: 1));
        var dbRestorer = new TestDbRestorer(dbSetupMock.Object, containerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ExecFailedException>(dbRestorer.TestEnsureRestorationDirectoryExistsAsync);
    }

    [Fact]
    public async Task EnsureRestorationDirectoryExistsAsync_ThrowsExecFailedException_IfCommandExecutionFinishedWithNonEmptyStderr()
    {
        // Arrange
        // const string restorationSnapshotName = "TestRestorationSnapshotName";
        const string restorationStateFilesDirectory = "/test/path";
        var dbSetupMock = new Mock<DbSetup>("dbName", "containerConnectionString", "migrationsPath", DbType.Other, true, restorationStateFilesDirectory, null!)
        {
            CallBase = true
        };
        dbSetupMock.Setup(ds => ds.RestorationStateFilesDirectory).Returns(restorationStateFilesDirectory);
        var containerMock = new Mock<IContainer>();
        containerMock.Setup(c => c.ExecAsync(It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(stdout: string.Empty, stderr: "test-std-err-text", exitCode: 0));
        var dbRestorer = new TestDbRestorer(dbSetupMock.Object, containerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ExecFailedException>(dbRestorer.TestEnsureRestorationDirectoryExistsAsync);
    }

    private class TestDbRestorer : DbRestorer
    {
        public TestDbRestorer(
            DbSetup dbSetup,
            IContainer container) 
            : base(dbSetup, container)
        {
        }

        public Task TestEnsureRestorationDirectoryExistsAsync()
        {
            return EnsureRestorationDirectoryExistsAsync();
        }

        public override Task RestoreAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task SnapshotAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> IsSnapshotUpToDateAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
