using System.IO.Abstractions;
using Moq;
using Testcontainers.AutoSetup.Core.Common.Helpers;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Helpers;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class FileLMDHelperTests
{
    [Fact]
    public void GetDirectoryFilesLMD_ThrowsDirectoryNotFound_IfDirectoryDoesntExist()
    {
        // Arrange
        var directory = "./migrations";
        var files = new[] { "init.sql" };
        
        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(false);

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.DirectoryInfo.New(directory)).Returns(dirMock.Object);

        // Act & Assert
        var ex = Assert.Throws<DirectoryNotFoundException>(() => 
            FileLMDHelper.GetDirectoryFilesLastModificationDate(directory, files, fileSystemMock.Object));
            
        Assert.Contains(directory, ex.Message);
    }

    [Fact]
    public void GetDirectoryFilesLMD_ThrowsFileNotFound_IfNoFilesMatch()
    {
        // Arrange
        var directory = "./migrations";
        var files = new[] { "init.sql" };

        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);
        // Return empty or unrelated files
        dirMock.Setup(d => d.GetFileSystemInfos("*", SearchOption.AllDirectories))
            .Returns(new IFileSystemInfo[] 
            { 
                CreateFileMock("other.sql", DateTime.UtcNow).Object 
            });

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.DirectoryInfo.New(directory)).Returns(dirMock.Object);

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() => 
            FileLMDHelper.GetDirectoryFilesLastModificationDate(directory, files, fileSystemMock.Object));
            
        Assert.Contains("empty", ex.Message);
    }

    [Fact]
    public void GetDirectoryFilesLMD_ThrowsFileNotFound_IfPartialFilesFound()
    {
        // Arrange
        var directory = "./migrations";
        var files = new[] { "001_init.sql", "002_update.sql" }; // We expect 2 files

        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);
        
        // Only return 1 matching file
        var foundFile = CreateFileMock("001_init.sql", DateTime.UtcNow);
        
        dirMock.Setup(d => d.GetFileSystemInfos("*", SearchOption.AllDirectories))
            .Returns(new[] { foundFile.Object });

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.DirectoryInfo.New(directory)).Returns(dirMock.Object);

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() => 
            FileLMDHelper.GetDirectoryFilesLastModificationDate(directory, files, fileSystemMock.Object));
            
        Assert.Contains("Some of the specified SQL files were not found", ex.Message);
    }

    [Fact]
    public void GetDirectoryFilesLMD_ReturnsLatestDate_FromSpecificFiles()
    {
        // Arrange
        var directory = "./migrations";
        var files = new[] { "old.sql", "new.sql" };
        var oldDate = new DateTime(2024, 1, 1);
        var newDate = new DateTime(2024, 2, 1); // Latest

        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);
        dirMock.Setup(d => d.LastWriteTimeUtc).Returns(oldDate);

        var file1 = CreateFileMock("old.sql", oldDate);
        var file2 = CreateFileMock("new.sql", newDate);
        // Add a noise file that is newer but NOT in the list (should be ignored)
        var ignoredFile = CreateFileMock("ignored.sql", new DateTime(2025, 1, 1)); 

        dirMock.Setup(d => d.GetFileSystemInfos("*", SearchOption.AllDirectories))
            .Returns(new[] { file1.Object, file2.Object, ignoredFile.Object });

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.DirectoryInfo.New(directory)).Returns(dirMock.Object);

        // Act
        var result = FileLMDHelper.GetDirectoryFilesLastModificationDate(directory, files, fileSystemMock.Object);

        // Assert
        Assert.Equal(newDate, result); // Should be 2024-02-01, ignoring 2025
    }

    [Fact]
    public void GetDirectoryLMD_ThrowsDirectoryNotFound_IfDirectoryDoesntExist()
    {
        // Arrange
        var directory = "./migrations";
        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(false);

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.DirectoryInfo.New(directory)).Returns(dirMock.Object);

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => 
            FileLMDHelper.GetDirectoryLastModificationDate(directory, fileSystemMock.Object));
    }

    [Fact]
    public void GetDirectoryLMD_ThrowsFileNotFound_IfDirectoryIsEmpty()
    {
        // Arrange
        var directory = "./migrations";
        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);
        dirMock.Setup(d => d.GetFileSystemInfos("*", SearchOption.AllDirectories))
            .Returns(Array.Empty<IFileSystemInfo>());

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.DirectoryInfo.New(directory)).Returns(dirMock.Object);

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => 
            FileLMDHelper.GetDirectoryLastModificationDate(directory, fileSystemMock.Object));
    }

    [Fact]
    public void GetDirectoryLMD_ReturnsLatestDate_AmongFilesAndSubDirectories()
    {
        // Arrange
        var directory = "./migrations";
        var expectedLatest = new DateTime(2024, 5, 1);

        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);
        dirMock.Setup(d => d.LastWriteTimeUtc).Returns(new DateTime(2024, 1, 1));

        // File 1: Old
        var file1 = CreateFileMock("f1.sql", new DateTime(2024, 2, 1));
        // SubDir: The Newest Item
        var subDir = CreateFileMock("subdir", expectedLatest); 
        
        dirMock.Setup(d => d.GetFileSystemInfos("*", SearchOption.AllDirectories))
            .Returns(new[] { file1.Object, subDir.Object });

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.DirectoryInfo.New(directory)).Returns(dirMock.Object);

        // Act
        var result = FileLMDHelper.GetDirectoryLastModificationDate(directory, fileSystemMock.Object);

        // Assert
        Assert.Equal(expectedLatest, result);
    }

    [Fact]
    public void GetDirectoryLMD_ReturnsParentDirDate_IfNewerThanContent()
    {
        // Arrange
        var directory = "./migrations";
        var contentDate = new DateTime(2024, 1, 1);
        var parentDate = new DateTime(2024, 2, 1); // Parent folder modified later (e.g. deletion)

        var dirMock = new Mock<IDirectoryInfo>();
        dirMock.Setup(d => d.Exists).Returns(true);
        dirMock.Setup(d => d.LastWriteTimeUtc).Returns(parentDate);

        var file1 = CreateFileMock("f1.sql", contentDate);

        dirMock.Setup(d => d.GetFileSystemInfos("*", SearchOption.AllDirectories))
            .Returns(new[] { file1.Object });

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.DirectoryInfo.New(directory)).Returns(dirMock.Object);

        // Act
        var result = FileLMDHelper.GetDirectoryLastModificationDate(directory, fileSystemMock.Object);

        // Assert
        Assert.Equal(parentDate, result);
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------
    private Mock<IFileSystemInfo> CreateFileMock(string name, DateTime lastWriteTime)
    {
        var mock = new Mock<IFileSystemInfo>();
        mock.Setup(f => f.Name).Returns(name);
        mock.Setup(f => f.LastWriteTimeUtc).Returns(lastWriteTime);
        return mock;
    }
}
