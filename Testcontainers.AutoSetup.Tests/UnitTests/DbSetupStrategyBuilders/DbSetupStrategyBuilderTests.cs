using DotNet.Testcontainers;
using DotNet.Testcontainers.Containers;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Common.DbStrategy;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.Tests.TestCollections;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Testcontainers.AutoSetup.Tests.UnitTests.DbSetupStrategyBuilders;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class DbSetupStrategyBuilderTests
{
    private readonly Mock<DbSetup> _dbSetupMock;
    private readonly Mock<IContainer> _containerMock;
    private readonly Mock<ILogger> _loggerMock;

    public DbSetupStrategyBuilderTests()
    {
        // 1. Setup Common Test Data
        _dbSetupMock = new Mock<DbSetup>("conn_str", "image", "sa", DbType.MsSQL, false, null!, null!);
        _containerMock = new Mock<IContainer>();
        _loggerMock = new Mock<ILogger>();
    }

    // -------------------------------------------------------------------------
    // Constructor Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_ShouldThrow_WhenDbSetupIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DbSetupStrategyBuilder(null!, _containerMock.Object));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenContainerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DbSetupStrategyBuilder(_dbSetupMock.Object, null!));
    }

    [Fact]
    public void Constructor_ShouldUseConsoleLogger_WhenLoggerIsNull()
    {
        // Act
        var builder = new DbSetupStrategyBuilder(_dbSetupMock.Object, _containerMock.Object, logger: null);

        // Assert
        Assert.NotNull(builder._logger);
        Assert.IsType<ConsoleLogger>(builder._logger);
    }

    [Fact]
    public void Constructor_ShouldUseProvidedLogger_WhenLoggerIsProvided()
    {
        // Act
        var builder = new DbSetupStrategyBuilder(_dbSetupMock.Object, _containerMock.Object, _loggerMock.Object);

        // Assert
        Assert.Same(_loggerMock.Object, builder._logger);
    }

    [Fact]
    public void Build_ShouldThrow_WhenSeederIsNotConfigured()
    {
        // Arrange
        var builder = new DbSetupStrategyBuilder(_dbSetupMock.Object, _containerMock.Object);

        builder._restorer = new Mock<DbRestorer>(_dbSetupMock.Object, _containerMock.Object, _loggerMock.Object).Object; 

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Equal("Seeder is not configured.", ex.Message);
    }

    [Fact]
    public void Build_ShouldThrow_WhenRestorerIsNotConfigured()
    {
        // Arrange
        var builder = new DbSetupStrategyBuilder(_dbSetupMock.Object, _containerMock.Object);

        builder._seeder = new Mock<DbSeeder>(_loggerMock.Object).Object;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(builder.Build);
        Assert.Equal("Restorer is not configured.", ex.Message);
    }


    [Fact]
    public void Build_ShouldReturnStrategy_WhenAllComponentsAreConfigured()
    {
        // Arrange
        var builder = new DbSetupStrategyBuilder(_dbSetupMock.Object, _containerMock.Object);

        var mockSeeder = new Mock<DbSeeder>(_loggerMock.Object).Object;
        var mockRestorer = new Mock<DbRestorer>(_dbSetupMock.Object, _containerMock.Object, _loggerMock.Object).Object;;

        builder._seeder = mockSeeder;
        builder._restorer = mockRestorer;

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<DbSetupStrategy>(result);
    }
}
