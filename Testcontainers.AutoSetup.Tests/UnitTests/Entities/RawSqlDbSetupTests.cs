using System.IO.Abstractions;
using Moq;
using Moq.Protected;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Entities;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class RawSqlDbSetupTests
{
    [Fact]
    public void RawSqlDbSetup_SetsSqlFilesPropertyCorrectly()
    {
        // Arrange
        var sqlFiles = new List<string> { "script1.sql", "script2.sql", "script3.sql" };

        // Act
        var sut = new RawSqlDbSetup
        (
            dbName: "TestDb",
            migrationsPath: "./migrations",
            containerConnectionString: "test-connection-string",
            sqlFiles: sqlFiles
        );
        // Assert
        Assert.Equal(sqlFiles, sut.SqlFiles);
    }

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

        var sqlFiles = new List<string> { "script1.sql", "script2.sql" };
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

        // Act & Assert Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => dbSetupMock.Object.GetMigrationsLastModificationDateAsync(It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetMigrationsLastModificationDateAsync_ThrowsFileNotFoundException_IfMigrationsPathIsEmpty()
    {
        // Arrange        
        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);
        dirMock.Setup(d => d.GetFiles("*", SearchOption.AllDirectories)).Returns([]);

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock
            .Setup(fs => fs.DirectoryInfo.New(It.IsAny<string>()))
            .Returns(dirMock.Object);

        var sqlFiles = new List<string> { "script1.sql", "script2.sql" };
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

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => dbSetupMock.Object.GetMigrationsLastModificationDateAsync(It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetMigrationsLastModificationDateAsync_ThrowsFileNotFoundException_IfNotAllMigrationsFilesFound()
    {
        // Arrange        
        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);

        var file1Mock = new Mock<IFileInfo>();
        file1Mock.Setup(f => f.Name).Returns("script1.sql");
        dirMock.Setup(d => d.GetFiles("*", SearchOption.AllDirectories))
            .Returns([file1Mock.Object]);

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock
            .Setup(fs => fs.DirectoryInfo.New(It.IsAny<string>()))
            .Returns(dirMock.Object);

        var sqlFiles = new List<string> { "script1.sql", "script2.sql" };
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

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => dbSetupMock.Object.GetMigrationsLastModificationDateAsync(It.IsAny<CancellationToken>()));
    }
}
