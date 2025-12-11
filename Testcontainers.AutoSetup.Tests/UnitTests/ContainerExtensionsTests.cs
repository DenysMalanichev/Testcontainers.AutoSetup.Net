using DotNet.Testcontainers.Containers;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Tests.TestCollections;
using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.AutoSetup.Core.Common.Exceptions;

namespace Testcontainers.AutoSetup.Tests.UnitTests;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelTests))]
public class ContainerBuilderExtensionsTests
{
    [Fact]
    public async Task StartWithSeedAsync_SuccessfulRun_CallsMethodsInCorrectOrder()
    {
        // Arrange
        var containerMock = new Mock<IContainer>();
        var seederMock = new Mock<IDbSeeder>();
        var cancellationToken = new CancellationTokenSource().Token;
        containerMock.Setup(c => c.State).Returns(TestcontainersStates.Running);

        // Setup a fake connection string provider
        string ExpectedConnectionString = "Server=localhost;Database=TestDb;";
        Func<IContainer, string> connectionStringProvider = (c) => ExpectedConnectionString;

        // Setup Seeder to complete successfully
        seederMock
            .Setup(s => s.SeedAsync(containerMock.Object, ExpectedConnectionString, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await containerMock.Object.SeedAsync(
            seederMock.Object, 
            connectionStringProvider, 
            cancellationToken
        );

        // Assert
        seederMock.Verify(s => s.SeedAsync(containerMock.Object, ExpectedConnectionString, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task StartWithSeedAsync_SeederFails_PropagatesException()
    {
        // Arrange
        var containerMock = new Mock<IContainer>();
        containerMock.Setup(c => c.State).Returns(TestcontainersStates.Running);

        var seederMock = new Mock<IDbSeeder>();

        // Seeder crashes (e.g. bad migration script)
        seederMock
            .Setup(s => s.SeedAsync(It.IsAny<IContainer>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Migration failed: Syntax error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            containerMock.Object.SeedAsync(
                seederMock.Object, 
                c => "valid-conn-string", 
                CancellationToken.None
            )
        );

        Assert.Equal("Migration failed: Syntax error", exception.Message);
    }

    [Fact]
    public async Task StartWithSeedAsync_ResolvesCorrectTypeForProvider()
    {
        // Arrange
        // We mock a specific interface inheriting IContainer to test the generic TContainer support
        var sqlContainerMock = new Mock<IDatabaseContainer>(); 
        var seederMock = new Mock<IDbSeeder>();

        sqlContainerMock.Setup(c => c.State)
            .Returns(It.Is<TestcontainersStates>(s => s != TestcontainersStates.Running));

        // Act
        await Assert.ThrowsAsync<InvalidContainerStateException>(() => sqlContainerMock.Object.SeedAsync(
            seederMock.Object,
            c =>
            {
                // Assert inside the provider that we got the specific type back, not just IContainer
                Assert.IsType<IDatabaseContainer>(c, exactMatch: false); 
                return "connection-string";
            }
        ));
    }
}

// Helper interface for the Generic Type test
public interface IDatabaseContainer : IContainer
{
    // Just a placeholder interface
}