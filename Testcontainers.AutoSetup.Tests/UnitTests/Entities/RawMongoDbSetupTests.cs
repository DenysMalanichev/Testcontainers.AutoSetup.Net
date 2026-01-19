using System.IO.Abstractions;
using Moq;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Entities;

public class RawMongoDbSetupTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;

    public RawMongoDbSetupTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMongoFilesIsNull()
    {
        // Arrange
        IList<RawMongoDataFile> nullFiles = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new RawMongoDbSetup(
                nullFiles,
                "myDb",
                "/migrations",
                false,
                null,
                _fileSystemMock.Object
            ));

        Assert.Equal("mongoFiles", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMongoFilesIsEmpty()
    {
        // Arrange
        var emptyFiles = new List<RawMongoDataFile>();

        // Act & Assert
        // This validates the .IsNullOrEmpty() check inside the constructor
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new RawMongoDbSetup(
                emptyFiles,
                "myDb",
                "/migrations",
                false,
                null,
                _fileSystemMock.Object
            ));

        Assert.Equal("mongoFiles", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties_WhenValidArgumentsProvided()
    {
        // Arrange
        var mongoFiles = new List<RawMongoDataFile>
        {
            new(collectionName: "users_col", fileName: "users", MongoDataFileExtension.JSON)
        };
        var dbName = "TestDatabase";
        var migrationsPath = "/app/migrations";
        var restoreFromDump = true;
        var restoreStatePath = "/state/path";

        // Act
        var setup = new RawMongoDbSetup(
            mongoFiles,
            dbName,
            migrationsPath,
            restoreFromDump,
            restoreStatePath,
            _fileSystemMock.Object
        );

        // Assert
        Assert.Equal(mongoFiles, setup.MongoFiles);
        Assert.Equal(dbName, setup.DbName); // Inherited from DbSetup
        Assert.Equal(migrationsPath, setup.MigrationsPath); // Inherited
        Assert.Equal(restoreFromDump, setup.RestoreFromDump); // Inherited
        Assert.Equal(restoreStatePath, setup.RestorationStateFilesDirectory); // Inherited
        
        // specific check for base constructor hardcoded values
        Assert.Equal(string.Empty, setup.ContainerConnectionString); 
        Assert.Equal(DbType.MongoDB, setup.DbType);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultAuthenticationValues_WhenNotOverridden()
    {
        // Arrange
        var mongoFiles = new List<RawMongoDataFile>
        {
            new(collectionName: "test", fileName: "test", MongoDataFileExtension.JSON)
        };

        // Act
        var setup = new RawMongoDbSetup(
            mongoFiles,
            "db",
            "path"
        );

        // Assert
        Assert.Equal("mongo", setup.Username);
        Assert.Equal("mongo", setup.Password);
        Assert.Equal("admin", setup.AuthenticationDatabase);
    }

    [Fact]
    public void Constructor_ShouldAllowOverridingDefaultValues_ViaObjectInitializer()
    {
        // Arrange
        var mongoFiles = new List<RawMongoDataFile> { new(collectionName: "users_col", fileName: "users", MongoDataFileExtension.JSON) };

        // Act
        var setup = new RawMongoDbSetup(mongoFiles, "db", "path")
        {
            Username = "customUser",
            Password = "customPassword",
            AuthenticationDatabase = "customAuthDb"
        };

        // Assert
        Assert.Equal("customUser", setup.Username);
        Assert.Equal("customPassword", setup.Password);
        Assert.Equal("customAuthDb", setup.AuthenticationDatabase);
    }
}
