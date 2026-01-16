using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.DbStrategy;
using Testcontainers.AutoSetup.Core.DbRestoration;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.DbSetupStrategyBuilders.RestorerExtensions;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class MySqlRestorerExtensionTests
{
    [Fact]
    public async Task WithMySqlDbRestorer_SetsTheCorrectRestorer()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", Core.Common.Enums.DbType.Other, false, null!, null!);
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>());

        // Act 
        builder.WithMySqlDbRestorer(Mock.Of<IDbConnectionFactory>());

        // Assert
        Assert.IsType<MySqlDbRestorer>(builder._restorer);
    }

    [Fact]
    public async Task WithMySqlDbRestorer_ThrowsArgumentException_IfRestorerIsAlreadySet()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", Core.Common.Enums.DbType.Other, false, null!, null!);
        var restorerMock = new Mock<DbRestorer>(dbSetupMock.Object, Mock.Of<IContainer>(), Mock.Of<ILogger>());
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>())
        {
            _restorer = restorerMock.Object
        };

        // Act && Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => 
            builder.WithMySqlDbRestorer(Mock.Of<IDbConnectionFactory>()));
        Assert.Contains("restorer", ex.Message, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task WithMySqlDbRestorer_ThrowsArgumentNullException_IfConnectionFactoryIsNull()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", Core.Common.Enums.DbType.Other, false, null!, null!);
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>())
        {
            _seeder = Mock.Of<DbSeeder>()
        };

        // Act && Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            builder.WithMySqlDbRestorer(null!));
        Assert.Contains("connectionFactory", ex.Message, StringComparison.InvariantCultureIgnoreCase);
    }
}
