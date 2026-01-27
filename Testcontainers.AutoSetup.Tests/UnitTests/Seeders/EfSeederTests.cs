using System.IO.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.EntityFramework;
using Testcontainers.AutoSetup.EntityFramework.Entities;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Seeders;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class EfSeederTests
{
    [Fact]
    public async Task EfSeeder_MigratesDatabase()
    {
        // Arrange
        var dbContextMock = new Mock<DbContext>();
        var contextFactoryMock = new Mock<Func<string, DbContext>>();
        contextFactoryMock.Setup(f => f.Invoke(It.IsAny<string>())).Returns(dbContextMock.Object);
        var dbSetupMock = new Mock<EfDbSetup>(
            contextFactoryMock.Object,
            "TestDbName",
            "conn-str",
            "./migrations",
            DbType.Other,
            false,
            null!,
            new Mock<IFileSystem>().Object
        ) { CallBase = true }; 
        dbSetupMock.Setup(ds => ds.BuildDbConnectionString()).Returns("Server=dummy;Database=TestDb;");

        var seederMock = new Mock<EfSeeder>(Mock.Of<ILogger>())
        { CallBase = true };
        seederMock.Protected()
            .Setup<Task>("ExecuteMigrateAsync", ItExpr.IsAny<DbSetup>(), ItExpr.IsAny<DbContext>(), ItExpr.IsAny<CancellationToken>())
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await seederMock.Object.SeedAsync(dbSetupMock.Object, container: null!);

        // Assert
        seederMock.Protected().Verify(
            "ExecuteMigrateAsync", 
            Times.Once(), 
            ItExpr.IsAny<DbSetup>(),
            ItExpr.IsAny<DbContext>(), 
            ItExpr.IsAny<CancellationToken>()
        );

        // Verify the factory was used (ensures flow reached the migration step)
        contextFactoryMock.Verify(f => f.Invoke(It.IsAny<string>()), Times.Once);
    }
}
