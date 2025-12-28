using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Tests.TestCollections;
using Testcontainers.AutoSetup.Core.Common.SqlDbHelpers;
using System.IO.Abstractions;

namespace Testcontainers.AutoSetup.Tests.UnitTests;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class DbSetupStrategyTests
{
    [Fact]
    public void Ctor_ThrowsArgumentException_IfFailedToInstantiateSeeder()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();
        bool tryInitialRestoreFromSnapshot = false;
        const string restorationPath = "test-restoration-path";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new DbSetupStrategy<TestFailedCtorDbSeeder, TestDbRestorer>(
            dbSetupMock.Object,
            containerMock.Object,
            tryInitialRestoreFromSnapshot,
            restorationPath));
        Assert.Equal($"Failed to instantiate a seeder of type {typeof(TestFailedCtorDbSeeder)}", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void Ctor_ThrowsArgumentException_IfFailedToInstantiateRestorer()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();
        bool tryInitialRestoreFromSnapshot = false;
        const string restorationPath = "test-restoration-path";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new DbSetupStrategy<TestDbSeeder, TestFailedCtorDbRestorer>(
            dbSetupMock.Object,
            containerMock.Object,
            tryInitialRestoreFromSnapshot,
            restorationPath));
        Assert.Equal($"Failed to instantiate a restorer of type {typeof(TestFailedCtorDbRestorer)}", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfContainerIsNull()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>();
        IContainer container = null!;
        bool tryInitialRestoreFromSnapshot = false;
        const string restorationPath = "test-restoration-path";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
            dbSetupMock.Object,
            container,
            tryInitialRestoreFromSnapshot,
            restorationPath));
    }

    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfDbSetupIsNull()
    {
        // Arrange
        DbSetup dbSetupMock = null!;
        var container = new Mock<IContainer>();
        bool tryInitialRestoreFromSnapshot = false;
        const string restorationPath = "test-restoration-path";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
            dbSetupMock,
            container.Object,
            tryInitialRestoreFromSnapshot,
            restorationPath));
    }

    [Fact]
    public async Task InitializeGlobalAsync_RestoresDb_IfSetAndMountAndSnapshotExist()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>();
        dbSetupMock.Setup(ds => ds.GetMigrationsLastModificationDateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTime.MinValue);
        dbSetupMock.Setup(s => s.ContainerConnectionString).Returns("container-conn");
        var containerMock = new Mock<IContainer>();

        bool tryInitialRestoreFromSnapshot = true;
        const string restorationPath = "test-restoration-path";

        containerMock.Setup(c => c.ExecAsync(new List<string>
            {
                "/bin/bash",
                "-c",
                $"findmnt {restorationPath}",
            }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(stdout: string.Empty, stderr: string.Empty, exitCode: 0));
        var cmd = $"ls {restorationPath}/*snapshot_* > /dev/null 2>&1 && " + 
          $"test -z \"$(find {restorationPath} " +
           "-maxdepth 1 -name '*_snapshot_*' " +
          $"-newermt '{DateTime.MinValue:yyyy-MM-dd HH:mm:ss}' -print -quit)\"";
        containerMock.Setup(c => c.ExecAsync(new List<string>
            {
                "/bin/bash",
                "-c",
                "cmd",
            }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(stdout: string.Empty, stderr: string.Empty, exitCode: 0));
        var setupStrategy = new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
            dbSetupMock.Object,
            containerMock.Object,
            tryInitialRestoreFromSnapshot,
            restorationPath);

        // Act
        await setupStrategy.InitializeGlobalAsync();

        // Assert
        var restorerField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_restorer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var spy = (TestDbRestorer)restorerField!.GetValue(setupStrategy)!;
        Assert.True(spy.WasRestoreCalled);
    }

    [Fact]
    public async Task InitializeGlobalAsync_InitializeDb_IfRestorationIsNoSet()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();
        dbSetupMock.Setup(s => s.ContainerConnectionString).Returns("container-conn");

        bool tryInitialRestoreFromSnapshot = false;
        const string restorationPath = "test-restoration-path";

        var setupStrategy = new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
            dbSetupMock.Object,
            containerMock.Object,
            tryInitialRestoreFromSnapshot,
            restorationPath);

        // Act
        await setupStrategy.InitializeGlobalAsync();

        // Assert
        var restorerField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_restorer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var restorerSpy = (TestDbRestorer)restorerField!.GetValue(setupStrategy)!;
        Assert.False(restorerSpy.WasRestoreCalled);
        Assert.True(restorerSpy.WasSnapshotCalled);

        var seederField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_seeder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var seederSpy = (TestDbSeeder)seederField!.GetValue(setupStrategy)!;

        Assert.True(seederSpy.WasSeedCalled);
    }

    [Fact]
    public async Task ResetAsync_Should_Call_Restore()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();
        const string restorationPath = "/tmp/restore";
        dbSetupMock.Setup(s => s.ContainerConnectionString).Returns("container-conn");

        var strategy = new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
            dbSetupMock.Object,
            containerMock.Object,
            tryInitialRestoreFromSnapshot: true,
            restorationStateFilesDirectory: restorationPath);

        // Act
        await strategy.ResetAsync();

        // Assert
        var restorerField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_restorer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var spy = (TestDbRestorer)restorerField!.GetValue(strategy)!;

        Assert.True(spy.WasRestoreCalled);
    }

    [Fact]
    public async Task InitializeGlobalAsync_FallsBackToSeed_IfMountDoesNotExist()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();
        const string restorationPath = "/tmp/missing-mount";
        dbSetupMock.Setup(s => s.ContainerConnectionString).Returns("container-conn");

        containerMock.Setup(c => c.ExecAsync(It.Is<IList<string>>(args => 
            args.Contains($"findmnt {restorationPath}")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 1));

        var strategy = new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
            dbSetupMock.Object,
            containerMock.Object,
            tryInitialRestoreFromSnapshot: true,
            restorationStateFilesDirectory: restorationPath);

        // Act
        await strategy.InitializeGlobalAsync();

        // Assert
        var restorerField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_restorer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var restorerSpy = (TestDbRestorer)restorerField!.GetValue(strategy)!;
        
        var seederField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_seeder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var seederSpy = (TestDbSeeder)seederField!.GetValue(strategy)!;

        Assert.False(restorerSpy.WasRestoreCalled, "Should not restore if mount is missing");
        Assert.True(seederSpy.WasSeedCalled, "Should seed if restore is skipped");
        Assert.True(restorerSpy.WasSnapshotCalled, "Should take new snapshot after seeding");
    }

    [Fact]
    public async Task InitializeGlobalAsync_FallsBackToSeed_IfMountCheckFailed()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();
        const string restorationPath = "/tmp/missing-mount";
        dbSetupMock.Setup(s => s.ContainerConnectionString).Returns("container-conn");

        containerMock.Setup(c => c.ExecAsync(It.Is<IList<string>>(args => 
            args.Contains($"findmnt {restorationPath}")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 100));

        var strategy = new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
            dbSetupMock.Object,
            containerMock.Object,
            tryInitialRestoreFromSnapshot: true,
            restorationStateFilesDirectory: restorationPath);

        // Act & Assert
        await Assert.ThrowsAsync<ExecFailedException>(async () => 
            await strategy.InitializeGlobalAsync());

        // Assert
        var restorerField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_restorer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var restorerSpy = (TestDbRestorer)restorerField!.GetValue(strategy)!;
        
        var seederField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_seeder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var seederSpy = (TestDbSeeder)seederField!.GetValue(strategy)!;

        Assert.False(restorerSpy.WasRestoreCalled);
        Assert.False(seederSpy.WasSeedCalled);
        Assert.False(restorerSpy.WasSnapshotCalled);
    }

    [Fact]
    public async Task InitializeGlobalAsync_FallsBackToSeed_IfSnapshotIsInvalid()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();
        const string restorationPath = "/tmp/mount";
        dbSetupMock.Setup(s => s.ContainerConnectionString).Returns("container-conn");

        containerMock.Setup(c => c.ExecAsync(It.Is<IList<string>>(args => 
            args.Contains($"findmnt {restorationPath}")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 0));

        containerMock.Setup(c => c.ExecAsync(It.Is<IList<string>>(args => 
            args.Any(a => a.Contains("ls") && a.Contains("-newermt"))), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 1));

        var strategy = new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
            dbSetupMock.Object,
            containerMock.Object,
            tryInitialRestoreFromSnapshot: true,
            restorationStateFilesDirectory: restorationPath);

        // Act
        await strategy.InitializeGlobalAsync();

        // Assert
        var restorerField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_restorer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var spy = (TestDbRestorer)restorerField!.GetValue(strategy)!;
        
        Assert.False(spy.WasRestoreCalled);
        Assert.True(spy.WasSnapshotCalled);
    }

    [Fact]
    public async Task InitializeGlobalAsync_ThrowsExecFailedException_IfSnapshotCheckFailed()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>();
        var containerMock = new Mock<IContainer>();
        const string restorationPath = "/tmp/mount";
        dbSetupMock.Setup(s => s.ContainerConnectionString).Returns("container-conn");

        containerMock.Setup(c => c.ExecAsync(It.Is<IList<string>>(args => 
            args.Contains($"findmnt {restorationPath}")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 0));

        containerMock.Setup(c => c.ExecAsync(It.Is<IList<string>>(args => 
            args.Any(a => a.Contains("ls") && a.Contains("-newermt"))), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(string.Empty, string.Empty, 100));

        var strategy = new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
            dbSetupMock.Object,
            containerMock.Object,
            tryInitialRestoreFromSnapshot: true,
            restorationStateFilesDirectory: restorationPath);

        // Act & Assert
        await Assert.ThrowsAsync<ExecFailedException>(async () => 
            await strategy.InitializeGlobalAsync());

        var restorerField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_restorer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var spy = (TestDbRestorer)restorerField!.GetValue(strategy)!;
        
        Assert.False(spy.WasRestoreCalled);
        Assert.False(spy.WasSnapshotCalled);
    }


    private class TestFailedCtorDbSeeder : DbSeeder
    {
        public TestFailedCtorDbSeeder(IDbConnectionFactory dbConnectionFactory, IFileSystem fileSystem) : base(dbConnectionFactory, fileSystem)
        {
            throw new Exception("Test exception");
        }

        public override Task SeedAsync(DbSetup dbSetup, IContainer container, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private class TestDbSeeder : DbSeeder
    {
        public bool WasSeedCalled { get; private set; }
        public TestDbSeeder(IDbConnectionFactory dbConnectionFactory, IFileSystem fileSystem, ILogger logger) : base(dbConnectionFactory, fileSystem, logger)
        { }

        public override Task SeedAsync(DbSetup dbSetup, IContainer container, CancellationToken cancellationToken)
        {
            WasSeedCalled = true;
            return Task.CompletedTask;
        }
    }

    public class TestDbRestorer : DbRestorer
    {
        public bool WasRestoreCalled { get; private set; }
        public bool WasSnapshotCalled { get; private set; }

        public TestDbRestorer(
            DbSetup dbSetup,
            IContainer container,
            IDbConnectionFactory dbConnectionFactory,
            string containerConnectionString,
            string restorationStateFilesDirectory,
            ILogger? logger = null) 
            : base(
                dbSetup,
                container,
                dbConnectionFactory,
                containerConnectionString,
                restorationStateFilesDirectory)
        {
        }

        public override Task RestoreAsync(CancellationToken cancellationToken = default)
        {
            WasRestoreCalled = true;
            return Task.CompletedTask;
        }

        public override Task SnapshotAsync(CancellationToken cancellationToken = default)
        {
            WasSnapshotCalled = true;
            return Task.CompletedTask;
        }
    }

    private class TestFailedCtorDbRestorer : DbRestorer
    {
        public TestFailedCtorDbRestorer(
            DbSetup dbSetup,
            IContainer container,
            IDbConnectionFactory dbConnectionFactory,
            string containerConnectionString,
            string restorationStateFilesDirectory) 
            : base(
                dbSetup,
                container,
                dbConnectionFactory,
                containerConnectionString,
                restorationStateFilesDirectory)
        {
            throw new Exception("Test restorer exception");
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