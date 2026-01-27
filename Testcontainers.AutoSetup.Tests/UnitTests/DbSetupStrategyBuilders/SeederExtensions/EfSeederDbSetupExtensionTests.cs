using System.IO.Abstractions;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.DbStrategy;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.EntityFramework;
using Testcontainers.AutoSetup.EntityFramework.Entities;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.DbSetupStrategyBuilders.SeederExtensions;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class EfSeederDbSetupExtensionTests
{
    [Fact]
    public async Task WithEfSeeder_SetsTheCorrectRestorer()
    {
        // Arrange
        var dbSetupMock = new Mock<EfDbSetup>((string connStr) => Mock.Of<DbContext>(), "t", "c", "p", DbType.Other, false, "d", Mock.Of<IFileSystem>());
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>());

        // Act 
        builder.WithEfSeeder();

        // Assert
        Assert.IsType<EfSeeder>(builder._seeder);
    }

    [Fact]
    public async Task WithEfSeeder_ThrowsArgumentException_OnWrongDbSetup()
    {
        // Arrange
        var dbSetupMock = new Mock<DbSetup>("t", "c", "p", DbType.Other, false, null!, null!);
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => builder.WithEfSeeder());
    }

    [Fact]
    public async Task WithEfSeeder_ThrowsArgumentException_IfSeederIsAlreadySet()
    {
        // Arrange
        var dbSetupMock = new Mock<EfDbSetup>((string connStr) => Mock.Of<DbContext>(), "t", "c", "p", DbType.Other, false, "d", Mock.Of<IFileSystem>());
        var builder = new DbSetupStrategyBuilder(dbSetupMock.Object, Mock.Of<IContainer>())
        {
            _seeder = Mock.Of<DbSeeder>()
        };

        // Act && Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => 
            builder.WithRawSqlDbSeeder(Mock.Of<IDbConnectionFactory>()));
        Assert.Contains("seeder", ex.Message, StringComparison.InvariantCultureIgnoreCase);
    }
}
