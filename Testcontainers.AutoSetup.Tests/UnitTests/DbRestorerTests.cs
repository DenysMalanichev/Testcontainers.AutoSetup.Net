using DotNet.Testcontainers.Containers;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
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
        const string restorationSnapshotName = null!;
        const string restorationStateFilesDirectory = "/test/path";
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();
        var dbRestorer = new TestDbRestorer(
            dbSetupMock.Object,
            containerMock.Object,
            "test-conn-str", restorationStateFilesDirectory, restorationSnapshotName);

        // Act & Assert
        Assert.Null(dbRestorer.RestorationStateFilesPath);
    }

    [Fact]
    public void RestorationStateFilesPath_ReturnsValidPath_IfSnapshotNameIsSet()
    {
        // Arrange
        const string restorationSnapshotName = "TestRestorationSnapshotName";
        const string restorationStateFilesDirectory = "/test/path";
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();
        var dbRestorer = new TestDbRestorer(dbSetupMock.Object, containerMock.Object, "test-conn-str", restorationStateFilesDirectory, restorationSnapshotName);

        // Act & Assert
        Assert.Equal($"{restorationStateFilesDirectory}/{restorationSnapshotName}", dbRestorer.RestorationStateFilesPath);
    }

    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfDbSetupIsNull()
    {
        // Arrange
        const string restorationSnapshotName = "TestRestorationSnapshotName";
        const string restorationStateFilesDirectory = "/test/path";
        DbSetup dbSetupMock = null!;
        var containerMock = new Mock<IContainer>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TestDbRestorer(dbSetupMock, containerMock.Object, "test-conn-str", restorationStateFilesDirectory, restorationSnapshotName));
    }

    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfContainerIsNull()
    {
        // Arrange
        const string restorationSnapshotName = "TestRestorationSnapshotName";
        const string restorationStateFilesDirectory = "/test/path";
        var dbSetupMock = new Mock<DbSetup>();
        IContainer containerMock = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TestDbRestorer(dbSetupMock.Object, containerMock, "test-conn-str", restorationStateFilesDirectory, restorationSnapshotName));
    }

    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfContainerConnStringIsNull()
    {
        // Arrange
        const string restorationSnapshotName = "TestRestorationSnapshotName";
        const string restorationStateFilesDirectory = "/test/path";
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TestDbRestorer(dbSetupMock.Object, containerMock.Object, containerConnectionString: null!, restorationStateFilesDirectory, restorationSnapshotName));
    }

    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfContainerConnStringIsEmpty()
    {
        // Arrange
        const string restorationSnapshotName = "TestRestorationSnapshotName";
        const string restorationStateFilesDirectory = "/test/path";
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TestDbRestorer(dbSetupMock.Object, containerMock.Object, containerConnectionString: string.Empty, restorationStateFilesDirectory, restorationSnapshotName));
    }

    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfRestorationStateFilesDirectoryIsNull()
    {
        // Arrange
        const string restorationSnapshotName = "TestRestorationSnapshotName";
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TestDbRestorer(dbSetupMock.Object, containerMock.Object,"test-conn-str", restorationStateFilesDirectory: null!, restorationSnapshotName));
    }

    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfRestorationStateFilesDirectoryIsEmpty()
    {
        // Arrange
        const string restorationSnapshotName = "TestRestorationSnapshotName";
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TestDbRestorer(dbSetupMock.Object, containerMock.Object, "test-conn-str", restorationStateFilesDirectory: string.Empty, restorationSnapshotName));
    }

    [Fact]
    public async Task EnsureRestorationDirectoryExistsAsync_ThrowsExecFailedException_IfCommandExecutionFinishedWithNonZeroCode()
    {
        // Arrange
        const string restorationSnapshotName = "TestRestorationSnapshotName";
        const string restorationStateFilesDirectory = "/test/path";
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();
        containerMock.Setup(c => c.ExecAsync(It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(stdout: string.Empty, stderr: string.Empty, exitCode: 1));
        var dbRestorer = new TestDbRestorer(dbSetupMock.Object, containerMock.Object, "test-conn-str", restorationStateFilesDirectory, restorationSnapshotName);

        // Act & Assert
        await Assert.ThrowsAsync<ExecFailedException>(dbRestorer.TestEnsureRestorationDirectoryExistsAsync);
    }

    [Fact]
    public async Task EnsureRestorationDirectoryExistsAsync_ThrowsExecFailedException_IfCommandExecutionFinishedWithNonEmptyStderr()
    {
        // Arrange
        const string restorationSnapshotName = "TestRestorationSnapshotName";
        const string restorationStateFilesDirectory = "/test/path";
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();
        containerMock.Setup(c => c.ExecAsync(It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(stdout: string.Empty, stderr: "test-std-err-text", exitCode: 0));
        var dbRestorer = new TestDbRestorer(dbSetupMock.Object, containerMock.Object,  "test-conn-str", restorationStateFilesDirectory, restorationSnapshotName);

        // Act & Assert
        await Assert.ThrowsAsync<ExecFailedException>(dbRestorer.TestEnsureRestorationDirectoryExistsAsync);
    }

    private class TestDbRestorer : DbRestorer
    {
        public TestDbRestorer(
            DbSetup dbSetup,
            IContainer container,
            string containerConnectionString,
            string restorationStateFilesDirectory,
            string restorationSnapshotName) 
            : base(dbSetup, container, containerConnectionString, restorationStateFilesDirectory)
        {
            _restorationSnapshotName = restorationSnapshotName;
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
    }
}
