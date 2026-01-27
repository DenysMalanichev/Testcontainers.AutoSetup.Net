using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.DbStrategy;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.Core.DbRestoration;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.DbSetupStrategyBuilders.RestorerExtensions;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class MongoDbRestorerExtensionTests
{
    [Fact]
    public async Task WithMongoDbRestorer_SetsTheCorrectRestorer()
    {
        // Arrange
        var dbSetupMock = new Mock<MongoDbSetup>("t", "c", "p", DbType.Other, false, null!, null!);
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>());

        // Act 
        builder.WithMongoDbRestorer();

        // Assert
        Assert.IsType<MongoDbRestorer>(builder._restorer);
    }

    [Fact]
    public async Task WithMongoDbRestorer_ThrowsArgumentException_OnWrongDbSetup()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!);
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => builder.WithMongoDbRestorer());
    }

    [Fact]
    public async Task WithMongoDbRestorer_ThrowsArgumentException_IfRestorerIsAlreadySet()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!);
        var restorerMock = new Mock<DbRestorer>(dbSetupMock.Object, Mock.Of<IContainer>(), Mock.Of<ILogger>());
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>())
        {
            _restorer = restorerMock.Object
        };

        // Act && Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>  builder.WithMongoDbRestorer());
        Assert.Contains("restorer", ex.Message, StringComparison.InvariantCultureIgnoreCase);
    }
}