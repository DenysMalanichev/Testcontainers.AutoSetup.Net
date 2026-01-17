using DotNet.Testcontainers.Containers;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Tests.TestCollections;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Common.Enums;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Testcontainers.AutoSetup.Tests.UnitTests.DbSetupStrategyBuilders;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class DbSetupStrategyTests
{
    public static TheoryData<(DbSetup, DbSeeder, DbRestorer, IContainer)> CtorParamsSets => new()
    {
        (null!, Mock.Of<DbSeeder>(), new Mock<DbRestorer>(new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!).Object, Mock.Of<IContainer>(), Mock.Of<ILogger>()).Object, Mock.Of<IContainer>()),
        (new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!).Object, null!, new Mock<DbRestorer>(new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!).Object, Mock.Of<IContainer>(), Mock.Of<ILogger>()).Object, Mock.Of<IContainer>()),
        (new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!).Object, Mock.Of<DbSeeder>(), null!, Mock.Of<IContainer>()),
        (new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!).Object, Mock.Of<DbSeeder>(), new Mock<DbRestorer>(new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!).Object, Mock.Of<IContainer>(), Mock.Of<ILogger>()).Object, null!),
    };

    [Theory]
    [MemberData(nameof(CtorParamsSets))]
    public void Ctor_ThrowsArgumentNullException_ProvidedArgumentIsNull(
        (DbSetup dbSetup, DbSeeder seeder, DbRestorer restorer, IContainer container) ctorArgs)
    {
        // Arrange
        var ctorArgsTuple = ctorArgs as ITuple;
        string nameOfNullArg = null!;
        for(int i = 0; i< ctorArgsTuple.Length; i++) 
            if(ctorArgsTuple[i] is null)
                nameOfNullArg = i switch
                {
                    0 => nameof(ctorArgs.dbSetup),
                    1 => nameof(ctorArgs.seeder),
                    2 => nameof(ctorArgs.restorer),
                    3 => nameof(ctorArgs.container),
                    _ => throw new Exception("No null argument")
                };

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DbSetupStrategy(ctorArgs.dbSetup, ctorArgs.seeder, ctorArgs.restorer, ctorArgs.container));

        // Assert
        Assert.Contains(nameOfNullArg, exception.Message);
    }

    [Fact]
    public async Task InitializeGlobalAsync_RestoresAndSkipsSeeding_IfSnapshotIsUpToDate()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!);
        var restorerMock = new Mock<DbRestorer>(dbSetupMock.Object, Mock.Of<IContainer>(), Mock.Of<ILogger>());
        var seederMock = new Mock<DbSeeder>();
        var containerMock = new Mock<IContainer>();
        restorerMock.Setup(x => x.IsSnapshotUpToDateAsync(null!, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);

        var strategy = new DbSetupStrategy(dbSetupMock.Object, seederMock.Object, restorerMock.Object, containerMock.Object, tryInitialRestoreFromSnapshot: true);

        // Act
        await strategy.InitializeGlobalAsync();

        // Assert
        restorerMock.Verify(x => x.IsSnapshotUpToDateAsync(null!, It.IsAny<CancellationToken>()), Times.Once);
        restorerMock.Verify(x => x.RestoreAsync(It.IsAny<CancellationToken>()), Times.Once);
        seederMock.Verify(x => x.SeedAsync(It.IsAny<DbSetup>(), It.IsAny<IContainer>(), It.IsAny<CancellationToken>()), Times.Never);
        restorerMock.Verify(x => x.SnapshotAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeGlobalAsync_SeedsAndCreatesSnapshot_IfSnapshotIsNotUpToDate()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!);
        var restorerMock = new Mock<DbRestorer>(dbSetupMock.Object, Mock.Of<IContainer>(), Mock.Of<ILogger>());
        var seederMock = new Mock<DbSeeder>();
        var containerMock = new Mock<IContainer>();
        restorerMock.Setup(x => x.IsSnapshotUpToDateAsync(null!, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);

        var strategy = new DbSetupStrategy(dbSetupMock.Object, seederMock.Object, restorerMock.Object, containerMock.Object, tryInitialRestoreFromSnapshot: true);

        // Act
        await strategy.InitializeGlobalAsync();

        // Assert
        restorerMock.Verify(x => x.IsSnapshotUpToDateAsync(null!, It.IsAny<CancellationToken>()), Times.Once);
        restorerMock.Verify(x => x.RestoreAsync(It.IsAny<CancellationToken>()), Times.Never);
        seederMock.Verify(x => x.SeedAsync(dbSetupMock.Object, containerMock.Object, It.IsAny<CancellationToken>()), Times.Once);
        restorerMock.Verify(x => x.SnapshotAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeGlobalAsync_SeedsAndCreatesSnapshotWithoutCheckingStatus_IfTryRestoreIsFalse_()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!);
        var restorerMock = new Mock<DbRestorer>(dbSetupMock.Object, Mock.Of<IContainer>(), Mock.Of<ILogger>());
        var seederMock = new Mock<DbSeeder>();
        var containerMock = new Mock<IContainer>();
        var strategy = new DbSetupStrategy(dbSetupMock.Object, seederMock.Object, restorerMock.Object, containerMock.Object, tryInitialRestoreFromSnapshot: false);

        // Act
        await strategy.InitializeGlobalAsync();

        // Assert
        restorerMock.Verify(x => x.IsSnapshotUpToDateAsync(null!, It.IsAny<CancellationToken>()), Times.Never);
        restorerMock.Verify(x => x.RestoreAsync(It.IsAny<CancellationToken>()), Times.Never);
        seederMock.Verify(x => x.SeedAsync(dbSetupMock.Object, containerMock.Object, It.IsAny<CancellationToken>()), Times.Once);
        restorerMock.Verify(x => x.SnapshotAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task ResetAsync_ShouldAlwaysCallRestore()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!);
        var restorerMock = new Mock<DbRestorer>(dbSetupMock.Object, Mock.Of<IContainer>(), Mock.Of<ILogger>());
        var seederMock = new Mock<DbSeeder>();
        var strategy = new DbSetupStrategy(dbSetupMock.Object, seederMock.Object, restorerMock.Object, Mock.Of<IContainer>());

        // Act
        await strategy.ResetAsync();

        // Assert
        restorerMock.Verify(x => x.RestoreAsync(It.IsAny<CancellationToken>()), Times.Once);

        seederMock.Verify(x => x.SeedAsync(It.IsAny<DbSetup>(), It.IsAny<IContainer>(), It.IsAny<CancellationToken>()), Times.Never);
        restorerMock.Verify(x => x.SnapshotAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}