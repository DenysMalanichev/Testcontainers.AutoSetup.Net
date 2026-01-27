using DotNet.Testcontainers.Containers;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.DbStrategy;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.Core.DbSeeding;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.DbSetupStrategyBuilders.SeederExtensions;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class RawSqlDbSetupExtensionTests
{
    [Fact]
    public async Task WithRawSqlDbSeeder_SetsTheCorrectRestorer()
    {
        // Arrange
        var dbSetupMock = new Mock<RawSqlDbSetup>(new List<string> {"f"}, "t", "c", "p", Core.Common.Enums.DbType.Other, false, null!, null!);
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>());

        // Act 
        builder.WithRawSqlDbSeeder(Mock.Of<IDbConnectionFactory>());

        // Assert
        Assert.IsType<RawSqlDbSeeder>(builder._seeder);
    }

     [Fact]
    public async Task WithRawSqlDbSeeder_ThrowsArgumentException_OnWrongDbSetup()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!);
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => builder.WithRawSqlDbSeeder(Mock.Of<IDbConnectionFactory>()));
    }

    [Fact]
    public async Task WithRawSqlDbSeeder_ThrowsArgumentException_IfSeederIsAlreadySet()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", Core.Common.Enums.DbType.Other, false, null!, null!);
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>())
        {
            _seeder = Mock.Of<DbSeeder>()
        };

        // Act && Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => 
            builder.WithRawSqlDbSeeder(Mock.Of<IDbConnectionFactory>()));
        Assert.Contains("seeder", ex.Message, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task WithRawSqlDbSeeder_ThrowsArgumentNullException_IfConnectionFactoryIsNull()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!);
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>())
        {
            _seeder = Mock.Of<DbSeeder>()
        };

        // Act && Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            builder.WithRawSqlDbSeeder(null!));
        Assert.Contains("connectionFactory", ex.Message, StringComparison.InvariantCultureIgnoreCase);
    }
}
