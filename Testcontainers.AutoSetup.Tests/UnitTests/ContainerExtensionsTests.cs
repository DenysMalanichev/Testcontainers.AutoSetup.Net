using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.AutoSetup.Tests.TestCollections;

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
        
        // Setup StartAsync to complete successfully
        containerMock
            .Setup(c => c.StartAsync(cancellationToken))
            .Returns(Task.CompletedTask);

        // Setup a fake connection string provider
        string ExpectedConnectionString = "Server=localhost;Database=TestDb;";
        Func<IContainer, string> connectionStringProvider = (c) => ExpectedConnectionString;

        // Setup Seeder to complete successfully
        seederMock
            .Setup(s => s.SeedAsync(containerMock.Object, ExpectedConnectionString, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await containerMock.Object.StartWithSeedAsync(
            seederMock.Object, 
            connectionStringProvider, 
            cancellationToken
        );

        // Assert
        // 1. Verify StartAsync was called once
        containerMock.Verify(c => c.StartAsync(cancellationToken), Times.Once);

        // 2. Verify SeedAsync was called with the correct connection string
        seederMock.Verify(s => s.SeedAsync(containerMock.Object, ExpectedConnectionString, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task StartWithSeedAsync_ContainerStartFails_DoesNotInvokeSeeder()
    {
        // Arrange
        var containerMock = new Mock<IContainer>();
        var seederMock = new Mock<IDbSeeder>();
        
        // Simulate a container crash during startup
        containerMock
            .Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Docker daemon not reachable"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            containerMock.Object.StartWithSeedAsync(
                seederMock.Object, 
                c => "conn-string", 
                CancellationToken.None
            )
        );

        Assert.Equal("Docker daemon not reachable", exception.Message);

        // Verify Seeder was NEVER called
        seederMock.Verify(s => s.SeedAsync(
            It.IsAny<IContainer>(), 
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task StartWithSeedAsync_SeederFails_PropagatesException()
    {
        // Arrange
        var containerMock = new Mock<IContainer>();
        var seederMock = new Mock<IDbSeeder>();

        // Container starts fine
        containerMock
            .Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Seeder crashes (e.g. bad migration script)
        seederMock
            .Setup(s => s.SeedAsync(It.IsAny<IContainer>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Migration failed: Syntax error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            containerMock.Object.StartWithSeedAsync(
                seederMock.Object, 
                c => "valid-conn-string", 
                CancellationToken.None
            )
        );

        Assert.Equal("Migration failed: Syntax error", exception.Message);
        
        // Verify StartAsync DID happen before the crash
        containerMock.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartWithSeedAsync_ResolvesCorrectTypeForProvider()
    {
        // Arrange
        // We mock a specific interface inheriting IContainer to test the generic TContainer support
        var sqlContainerMock = new Mock<IDatabaseContainer>(); 
        var seederMock = new Mock<IDbSeeder>();

        sqlContainerMock
            .Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await sqlContainerMock.Object.StartWithSeedAsync(
            seederMock.Object,
            (IDatabaseContainer c) => 
            {
                // Assert inside the provider that we got the specific type back, not just IContainer
                Assert.IsAssignableFrom<IDatabaseContainer>(c); 
                return "connection-string";
            }
        );

        // Assert
        sqlContainerMock.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

// Helper interface for the Generic Type test
public interface IDatabaseContainer : IContainer
{
    // Just a placeholder interface
}