using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.Core.DbSeeding;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Seeders;

public class RawMongoDbSeederTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IContainer> _containerMock;
    private readonly RawMongoDbSeeder _seeder;

    public RawMongoDbSeederTests()
    {
        _loggerMock = new Mock<ILogger>();
        _containerMock = new Mock<IContainer>();
        
        _seeder = new RawMongoDbSeeder(_loggerMock.Object);
    }

    [Fact]
    public async Task SeedAsync_ShouldThrowArgumentException_IfSetupIsNotRawMongoDbSetup()
    {
        // Arrange
        // Create a base DbSetup or a different derived type to trigger the check
        var invalidSetup = new RawSqlDbSetup(
            dbName: "MongoTest",
            migrationsPath: "/test/path",
            containerConnectionString: "testconn",
            sqlFiles: ["file"]
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _seeder.SeedAsync(invalidSetup, _containerMock.Object, CancellationToken.None));

        Assert.Contains("must be provided as an argument", exception.Message);
    }

    [Fact]
    public async Task SeedAsync_ShouldExecuteCorrectCommand_ForSingleJsonFile_WithArrayFlag()
    {
        // Arrange
        var mongoFile = RawMongoDataFile.FromJson
        (
            fileName: "users", 
            collectionName: "users_col",
            isJsonArray: true // This should trigger the --jsonArray flag
        );

        var setup = new RawMongoDbSetup([mongoFile], "MyDb", "/path/test");

        var result = new ExecResult(string.Empty, string.Empty, 0);
        _containerMock
            .Setup(c => c.ExecAsync(It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await _seeder.SeedAsync(setup, _containerMock.Object, CancellationToken.None);

        // Assert
        // We inspect the command string passed to /bin/bash -c
        _containerMock.Verify(c => c.ExecAsync(
            It.Is<IList<string>>(args => 
                args.Count == 3 &&
                args[0] == "/bin/bash" && 
                args[1] == "-c" &&
                args[2].Contains("mongoimport") &&
                args[2].Contains("--db MyDb") &&
                args[2].Contains("--collection users_col") &&
                args[2].Contains("--type json") &&
                args[2].Contains("--jsonArray") && 
                args[2].Contains($"--file {Constants.MongoDB.DefaultMigrationsDataPath}/users.json")
            ),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_ShouldExecuteCorrectCommand_ForSingleJsonFile_WithoutArrayFlag()
    {
        // Arrange
        var mongoFile = RawMongoDataFile.FromJson
        (
            fileName: "users", 
            collectionName: "users_col",
            isJsonArray: false // This should not pass the --jsonArray flag
        );

        var setup = new RawMongoDbSetup([mongoFile], "MyDb", "/path/test");

        var result = new ExecResult(string.Empty, string.Empty, 0);
        _containerMock
            .Setup(c => c.ExecAsync(It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await _seeder.SeedAsync(setup, _containerMock.Object, CancellationToken.None);

        // Assert
        // We inspect the command string passed to /bin/bash -c
        _containerMock.Verify(c => c.ExecAsync(
            It.Is<IList<string>>(args => 
                args.Count == 3 &&
                args[0] == "/bin/bash" && 
                args[1] == "-c" &&
                args[2].Contains("mongoimport") &&
                args[2].Contains("--db MyDb") &&
                args[2].Contains("--collection users_col") &&
                args[2].Contains("--type json") &&
                !args[2].Contains("--jsonArray") && 
                args[2].Contains($"--file {Constants.MongoDB.DefaultMigrationsDataPath}/users.json")
            ),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_ShouldChainCommands_WhenMultipleFilesExist()
    {
        // Arrange
        var file1 = RawMongoDataFile.FromJson(fileName: "f1", collectionName: "c1");
        var file2 = RawMongoDataFile.FromJson(fileName: "f2", collectionName: "c2");

        var setup = new RawMongoDbSetup([file1, file2], "MyDb", "/path/test");

        var result = new ExecResult(string.Empty, string.Empty, 0);
        _containerMock
            .Setup(c => c.ExecAsync(It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await _seeder.SeedAsync(setup, _containerMock.Object, CancellationToken.None);

        // Assert
        _containerMock.Verify(c => c.ExecAsync(
            It.Is<IList<string>>(args => 
                args[2].Contains("mongoimport") &&
                args[2].Contains("&& mongoimport") && // Verify chaining logic
                args[2].Contains("--collection c1") &&
                args[2].Contains("--collection c2")
            ),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_ShouldThrowExecFailedException_WhenContainerExitCodeIsNonZero()
    {
        // Arrange
        var mongoFile = RawMongoDataFile.FromJson(fileName: "f1", collectionName: "c1");
         var setup = new RawMongoDbSetup([mongoFile], "MyDb", "/path/test");

        var result = new ExecResult(string.Empty, "Import Failed", 1);
        _containerMock
            .Setup(c => c.ExecAsync(It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExecFailedException>(() => 
            _seeder.SeedAsync(setup, _containerMock.Object, CancellationToken.None));

        Assert.Equal(1, ex.ExecResult.ExitCode);
        
        // Verify we logged the error
        // Note: Verifying ILogger extensions requires this specific It.IsAnyType syntax
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to migrate MongoDB files")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
