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
public class RawMongoDbSetupExtensionTests
{
    [Fact]
    public async Task WithRawMongoDbSeeder_SetsTheCorrectRestorer()
    {
        // Arrange
        var dbSetupMock = new Mock<RawMongoDbSetup>(
            new List<RawMongoDataFile> { RawMongoDataFile.FromCsvWithHeaderfileFlag("c", "f") }, 
            "t", "c", false, null!, null!);
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>());

        // Act 
        builder.WithRawMongoDbSeeder();

        // Assert
        Assert.IsType<RawMongoDbSeeder>(builder._seeder);
    }

    [Fact]
    public async Task WithRawMongoDbSeeder_ThrowsArgumentException_OnWrongDbSetup()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!);
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => builder.WithRawMongoDbSeeder());
    }

    [Fact]
    public async Task WithRawMongoDbSeeder_ThrowsArgumentException_IfSeederIsAlreadySet()
    {
        // Arrange
        var dbSetupMock = new Mock<RawMongoDbSetup>(
            new List<RawMongoDataFile> { RawMongoDataFile.FromCsvWithHeaderfileFlag("c", "f") }, 
            "t", "c", false, null!, null!);
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>())
        {
            _seeder = Mock.Of<DbSeeder>()
        };

        // Act && Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => 
            builder.WithRawMongoDbSeeder());
        Assert.Contains("seeder", ex.Message, StringComparison.InvariantCultureIgnoreCase);
    }
}