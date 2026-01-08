using System.IO.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.EntityFramework.Entities;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Entities;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class EfDbSetupTests
{
    [Fact]
    public async Task GetMigrationsLastModificationDateAsync_ThrowsDirectoryNotFoundException_IfMigrationsPathDoesntExist()
    {
        // Arrange
        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(false);
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock
            .Setup(fs => fs.DirectoryInfo.New(It.IsAny<string>()))
            .Returns(dirMock.Object);
        var dbSetupMock = new Mock<EfDbSetup>(
            (string connStr) => (DbContext)null!,
            "TestDbName",
            "conn-str",
            "./migrations",
            DbType.Other,
            false,
            null!,
            fileSystemMock.Object
        ) { CallBase = true };

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>( 
            () => dbSetupMock.Object.GetMigrationsLastModificationDateAsync(It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetMigrationsLastModificationDateAsync_ThrowsFileNotFoundExceptionException_IfFoundFilesListIsNull()
    {
        // Arrange
        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);
        dirMock.Setup(d => d.GetFileSystemInfos("*", SearchOption.AllDirectories))
            .Returns((IFileSystemInfo[]?)null!);

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock
            .Setup(fs => fs.DirectoryInfo.New(It.IsAny<string>()))
            .Returns(dirMock.Object);

        var dbSetupMock = new Mock<EfDbSetup>(
            (string connStr) => (DbContext)null!,
            "TestDbName",
            "conn-str",
            "./migrations",
            DbType.Other,
            false,
            null!,
            fileSystemMock.Object
        ) { CallBase = true };

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>( 
            () => dbSetupMock.Object.GetMigrationsLastModificationDateAsync(It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetMigrationsLastModificationDateAsync_ThrowsFileNotFoundExceptionException_IfFoundFilesListIsEmpty()
    {
        // Arrange
        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);
        dirMock.Setup(d => d.GetFileSystemInfos("*", SearchOption.AllDirectories))
            .Returns([]);

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock
            .Setup(fs => fs.DirectoryInfo.New(It.IsAny<string>()))
            .Returns(dirMock.Object);

        var dbSetupMock = new Mock<EfDbSetup>(
            (string connStr) => (DbContext)null!,
            "TestDbName",
            "conn-str",
            "./migrations",
            DbType.Other,
            false,
            null!,
            fileSystemMock.Object
        ) { CallBase = true };

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>( 
            () => dbSetupMock.Object.GetMigrationsLastModificationDateAsync(It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetMigrationsLastModificationDateAsync_ReturnsLatestModifiedFileLMD()
    {
        // Arrange

        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);
        var expectedLatestLmd = new DateTime(2024, 2, 1);
        var file1Mock = new Mock<IFileSystemInfo>();
        file1Mock.Setup(f => f.LastWriteTimeUtc).Returns(expectedLatestLmd);
        var file2Mock = new Mock<IFileSystemInfo>();
        file2Mock.Setup(f => f.LastWriteTimeUtc).Returns(new DateTime(2024, 1, 1));
        dirMock.Setup(d => d.GetFileSystemInfos("*", SearchOption.AllDirectories))
            .Returns([file1Mock.Object, file2Mock.Object]);
        dirMock.Setup(d => d.LastWriteTimeUtc).Returns(new DateTime(2024, 1, 1));

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock
            .Setup(fs => fs.DirectoryInfo.New(It.IsAny<string>()))
            .Returns(dirMock.Object);

        var dbSetupMock = new Mock<EfDbSetup>(
            (string connStr) => (DbContext)null!,
            "TestDbName",
            "conn-str",
            "./migrations",
            DbType.Other,
            false,
            null!,
            fileSystemMock.Object
        ) { CallBase = true };

        // Act
        var result = await dbSetupMock.Object.GetMigrationsLastModificationDateAsync(It.IsAny<CancellationToken>());

        // Assert
        Assert.Equal(expectedLatestLmd, result);
    }

    [Fact]
    public async Task GetMigrationsLastModificationDateAsync_ReturnsParentDirLMD_IfItWasModifiedLatest()
    {
        // Arrange
        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);
        var expectedLatestLmd = new DateTime(2024, 2, 1);
        var file1Mock = new Mock<IFileSystemInfo>();
        file1Mock.Setup(f => f.LastWriteTimeUtc).Returns(new DateTime(2024, 1, 1));
        var file2Mock = new Mock<IFileSystemInfo>();
        file2Mock.Setup(f => f.LastWriteTimeUtc).Returns(new DateTime(2024, 1, 1));
        dirMock.Setup(d => d.GetFileSystemInfos("*", SearchOption.AllDirectories))
            .Returns([file1Mock.Object, file2Mock.Object]);
        dirMock.Setup(d => d.LastWriteTimeUtc).Returns(expectedLatestLmd);

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock
            .Setup(fs => fs.DirectoryInfo.New(It.IsAny<string>()))
            .Returns(dirMock.Object);

        var dbSetupMock = new Mock<EfDbSetup>(
            (string connStr) => (DbContext)null!,
            "TestDbName",
            "conn-str",
            "./migrations",
            DbType.Other,
            false,
            null!,
            fileSystemMock.Object
        ) { CallBase = true };

        // Act
        var result = await dbSetupMock.Object.GetMigrationsLastModificationDateAsync(It.IsAny<CancellationToken>());

        // Assert
        Assert.Equal(expectedLatestLmd, result);
    }

    [Fact]
    public async Task GetMigrationsLastModificationDateAsync_ReturnsSubDirLMD_IfItWasModifiedLatest()
    {
        // Arrange
        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);
        var expectedLatestLmd = new DateTime(2024, 2, 1);
        var file1Mock = new Mock<IFileSystemInfo>();
        file1Mock.Setup(f => f.LastWriteTimeUtc).Returns(new DateTime(2024, 1, 1));
        var file2Mock = new Mock<IFileSystemInfo>();
        file2Mock.Setup(f => f.LastWriteTimeUtc).Returns(new DateTime(2024, 1, 1));
        var subDirMock = new Mock<IDirectoryInfo>();
        subDirMock.Setup(f => f.LastWriteTimeUtc).Returns(expectedLatestLmd);
        subDirMock.Setup(f => f.Attributes).Returns(FileAttributes.Directory);
        subDirMock.Setup(f => f.Parent).Returns(dirMock.Object);
        dirMock.Setup(d => d.GetFileSystemInfos("*", SearchOption.AllDirectories))
            .Returns([file1Mock.Object, file2Mock.Object, subDirMock.Object]);
        dirMock.Setup(d => d.LastWriteTimeUtc).Returns(new DateTime(2024, 1, 1));

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock
            .Setup(fs => fs.DirectoryInfo.New(It.IsAny<string>()))
            .Returns(dirMock.Object);
            
        var dbSetupMock = new Mock<EfDbSetup>(
            (string connStr) => (DbContext)null!,
            "TestDbName",
            "conn-str",
            "./migrations",
            DbType.Other,
            false,
            null!,
            fileSystemMock.Object
        ) { CallBase = true };

        // Act
        var result = await dbSetupMock.Object.GetMigrationsLastModificationDateAsync(It.IsAny<CancellationToken>());

        // Assert
        Assert.Equal(expectedLatestLmd, result);
    }
}
