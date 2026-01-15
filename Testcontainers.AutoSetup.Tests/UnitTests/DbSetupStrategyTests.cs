using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Tests.TestCollections;
using System.IO.Abstractions;
using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Tests.UnitTests;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class DbSetupStrategyTests
{
    [Fact]
    public void Ctor_ThrowsArgumentException_IfFailedToInstantiateSeeder()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>( 
            "dbName",
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            null!,
            new Mock<IFileSystem>().Object
        );
        var containerMock = new Mock<IContainer>();
        var dbConnectionFactoryMock = new Mock<IDbConnectionFactory>();
        var fileSystemMock = new Mock<IFileSystem>();
        bool tryInitialRestoreFromSnapshot = false;
        var loggerMock = new Mock<ILogger>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new DbSetupStrategy<TestFailedCtorDbSeeder, TestDbRestorer>(
            dbSetupMock.Object,
            containerMock.Object,
            dbConnectionFactoryMock.Object,
            tryInitialRestoreFromSnapshot,
            fileSystemMock.Object,
            loggerMock.Object));
        Assert.StartsWith($"Failed to instantiate a seeder of type {typeof(TestFailedCtorDbSeeder)}",
         exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void Ctor_ThrowsArgumentException_IfFailedToInstantiateRestorer()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>( 
            "dbName",
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            null!,
            new Mock<IFileSystem>().Object
        );
        var containerMock = new Mock<IContainer>();
        var dbConnectionFactoryMock = new Mock<IDbConnectionFactory>();
        var fileSystemMock = new Mock<IFileSystem>();
        var loggerMock = new Mock<ILogger>();
        bool tryInitialRestoreFromSnapshot = false;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new DbSetupStrategy<TestDbSeeder, TestFailedCtorDbRestorer>(
                dbSetupMock.Object,
                containerMock.Object,
                dbConnectionFactoryMock.Object,
                tryInitialRestoreFromSnapshot,
                fileSystemMock.Object,
                loggerMock.Object));
        Assert.Equal($"Failed to instantiate a restorer of type {typeof(TestFailedCtorDbRestorer)}", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfContainerIsNull()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>( 
            "dbName",
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            null!,
            new Mock<IFileSystem>().Object
        );
        var dbConnectionFactoryMock = new Mock<IDbConnectionFactory>();
        var fileSystemMock = new Mock<IFileSystem>();
        var loggerMock = new Mock<ILogger>();
        bool tryInitialRestoreFromSnapshot = false;
        IContainer container = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
                dbSetupMock.Object,
                container,
                dbConnectionFactoryMock.Object,
                tryInitialRestoreFromSnapshot,
                fileSystemMock.Object,
                loggerMock.Object));
    }

    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfDbSetupIsNull()
    {
        // Arrange
        DbSetup dbSetupMock = null!;
        var containerMock = new Mock<IContainer>();
        var dbConnectionFactoryMock = new Mock<IDbConnectionFactory>();
        var fileSystemMock = new Mock<IFileSystem>();
        var loggerMock = new Mock<ILogger>();
        bool tryInitialRestoreFromSnapshot = false;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
                dbSetupMock,
                containerMock.Object,
                dbConnectionFactoryMock.Object,
                tryInitialRestoreFromSnapshot,
                fileSystemMock.Object,
                loggerMock.Object));
    }

    [Fact]
    public async Task InitializeGlobalAsync_RestoresDb_IfSetAndMountAndSnapshotExist()
    {
        // Arrange
        const string restorationPath = "test-restoration-path";
        const string dbName = "testdb";
        var dbSetupMock = new Mock<DbSetup>( 
            dbName,
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            restorationPath,
            new Mock<IFileSystem>().Object
        );
        bool tryInitialRestoreFromSnapshot = true;
        var containerMock = new Mock<IContainer>();

        containerMock.Setup(c => c.ExecAsync(new List<string>
            {
                "/bin/bash",
                "-c",
                $"findmnt {restorationPath}",
            }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(stdout: string.Empty, stderr: string.Empty, exitCode: 0));
        var cmd = $"ls {restorationPath}/{dbName}_snapshot_* > /dev/null 2>&1 && " + 
          $"test -n \"$(find {restorationPath} " +
          $"-maxdepth 1 -name '{dbName}_snapshot_*' " +
          $"-newermt '{DateTime.MinValue:yyyy-MM-dd HH:mm:ss}' -print -quit)\"";
        containerMock.Setup(c => c.ExecAsync(It.Is<IList<string>>(l => 
                l.Contains("/bin/bash") &&
                l.Contains("-c") &&
                l.Contains(cmd)), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecResult(stdout: string.Empty, stderr: string.Empty, exitCode: 0));
        var dbConnectionFactoryMock = new Mock<IDbConnectionFactory>();
        var fileSystemMock = new Mock<IFileSystem>();
        var loggerMock = new Mock<ILogger>();
        var setupStrategy = new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
                dbSetupMock.Object,
                containerMock.Object,
                dbConnectionFactoryMock.Object,
                tryInitialRestoreFromSnapshot,
                fileSystemMock.Object,
                loggerMock.Object);

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
        const string restorationPath = "test-restoration-path";
        const string dbName = "testdb";
        var dbSetupMock = new Mock<DbSetup>( 
            dbName,
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            restorationPath,
            new Mock<IFileSystem>().Object
        );
        var containerMock = new Mock<IContainer>();
        var dbConnectionFactoryMock = new Mock<IDbConnectionFactory>();
        var fileSystemMock = new Mock<IFileSystem>();
        var loggerMock = new Mock<ILogger>();
        dbSetupMock.Setup(s => s.ContainerConnectionString).Returns("container-conn");

        bool tryInitialRestoreFromSnapshot = false;

        var setupStrategy = new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
                dbSetupMock.Object,
                containerMock.Object,
                dbConnectionFactoryMock.Object,
                tryInitialRestoreFromSnapshot,
                fileSystemMock.Object,
                loggerMock.Object);

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
        const string restorationPath = "test-restoration-path";
        const string dbName = "testdb";
        var dbSetupMock = new Mock<DbSetup>( 
            dbName,
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            restorationPath,
            new Mock<IFileSystem>().Object
        );
        var containerMock = new Mock<IContainer>();
        var dbConnectionFactoryMock = new Mock<IDbConnectionFactory>();
        var fileSystemMock = new Mock<IFileSystem>();
        var loggerMock = new Mock<ILogger>();

        var strategy = new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
            dbSetupMock.Object,
                containerMock.Object,
                dbConnectionFactoryMock.Object,
                false,
                fileSystemMock.Object,
                loggerMock.Object);

        // Act
        await strategy.ResetAsync();

        // Assert
        var restorerField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_restorer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var spy = (TestDbRestorer)restorerField!.GetValue(strategy)!;

        Assert.True(spy.WasRestoreCalled);
    }

    [Fact]
    public async Task InitializeGlobalAsync_FallsBackToSeed_IfSnapshotIsNotUpToDate()
    {
        // Arrange
        const string restorationPath = "test-restoration-path";
        const string dbName = "testdb";
        var dbSetupMock = new Mock<DbSetup>( 
            dbName,
            "testConnStr",
            "migrationPath",
            DbType.MsSQL,
            false,
            restorationPath,
            new Mock<IFileSystem>().Object
        );
        var containerMock = new Mock<IContainer>();
        var dbConnectionFactoryMock = new Mock<IDbConnectionFactory>();
        var fileSystemMock = new Mock<IFileSystem>();
        var loggerMock = new Mock<ILogger>();

        var strategy = new DbSetupStrategy<TestDbSeeder, TestDbRestorer>(
            dbSetupMock.Object,
                containerMock.Object,
                dbConnectionFactoryMock.Object,
                true, // _tryInitialRestoreFromSnapshot
                fileSystemMock.Object,
                loggerMock.Object);

        var restorerField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_restorer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var restorerSpy = (TestDbRestorer)restorerField!.GetValue(strategy)!;
        restorerSpy.IsSnapshotUpToDate = false;

        // Act
        await strategy.InitializeGlobalAsync();

        // Assert        
        var seederField = typeof(DbSetupStrategy<TestDbSeeder, TestDbRestorer>)
            .GetField("_seeder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var seederSpy = (TestDbSeeder)seederField!.GetValue(strategy)!;

        Assert.False(restorerSpy.WasRestoreCalled, "Should not restore if mount is missing");
        Assert.True(seederSpy.WasSeedCalled, "Should seed if restore is skipped");
        Assert.True(restorerSpy.WasSnapshotCalled, "Should take new snapshot after seeding");
    }

    private class TestFailedCtorDbSeeder : DbSeeder
    {
        public TestFailedCtorDbSeeder(IDbConnectionFactory dbConnectionFactory, IFileSystem fileSystem) : base()
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
        public TestDbSeeder(ILogger logger) : base(logger)
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

        public bool IsSnapshotUpToDate { get; set; } = true;

        public TestDbRestorer(DbSetup dbSetup, IContainer container) 
            : base(dbSetup, container, Mock.Of<ILogger>())
        { }

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

        public override Task<bool> IsSnapshotUpToDateAsync(IFileSystem fileSystem = null!, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IsSnapshotUpToDate);
        }
    }

    private class TestFailedCtorDbRestorer : DbRestorer
    {
        public TestFailedCtorDbRestorer(DbSetup dbSetup, IContainer container) 
            : base(dbSetup, container, Mock.Of<ILogger>())
        {
            throw new Exception("Test restorer exception");
        }

        public override Task<bool> IsSnapshotUpToDateAsync(IFileSystem fileSystem = null!, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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