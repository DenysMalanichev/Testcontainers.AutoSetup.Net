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
    // Dummy interface that matches the generic constraints of extension method.
    public interface ITestBuilder : IContainerBuilder<ITestBuilder, IContainer, IContainerConfiguration>, 
        IAbstractBuilder<ITestBuilder, IContainer, CreateContainerParameters> 
    { }

    [Fact]
    public void WithDbSeeder_ShouldRegisterStartupCallback()
    {
        // Arrange
        var builderMock = new Mock<ITestBuilder>();    
        var seederMock = new Mock<IDbSeeder>();

        // Setup the builder to behave fluently (return itself)
        builderMock.Setup(b => b.WithStartupCallback(It.IsAny<Func<IContainer, CancellationToken, Task>>()))
                   .Returns(builderMock.Object);

        // Act
        var result = builderMock.Object.WithDbSeeder(
            seederMock.Object, 
            c => "Server=test" // No implementation of GetConnectionString in IContainer
        );

        // Assert
        Assert.Same(builderMock.Object, result);
        // Verify that WithStartupCallback was actually called once
        builderMock.Verify(b => b.WithStartupCallback(It.IsAny<Func<IContainer, CancellationToken, Task>>()), Times.Once);
    }

    [Fact]
    public async Task WithDbSeeder_Callback_ShouldInvokeSeeder_WithCorrectParameters()
    {
        // Arrange
        var builderMock = new Mock<ITestBuilder>();
        var containerMock = new Mock<IContainer>();
        var seederMock = new Mock<IDbSeeder>();
        
        // We will capture the callback function that your extension method creates
        Func<IContainer, CancellationToken, Task> capturedCallback = null!;

        builderMock.Setup(b => b.WithStartupCallback(It.IsAny<Func<IContainer, CancellationToken, Task>>()))
                   .Callback<Func<IContainer, CancellationToken, Task>>(cb => capturedCallback = cb)
                   .Returns(builderMock.Object);

        // Apply the extension
        builderMock.Object.WithDbSeeder(
            seederMock.Object, 
            container => "Server=MyMockedConnectionString" // User provided lambda
        );

        // Act - Simulate the container starting and triggering the callback
        Assert.NotNull(capturedCallback); // Ensure we successfully captured it
        
        // We manually invoke the captured callback to test what happens inside it
        await capturedCallback.Invoke(containerMock.Object, CancellationToken.None);

        // Assert
        // Verify Seeder was called with the Container Object and the Result of the connection string lambda
        seederMock.Verify(s => s.SeedAsync(
            containerMock.Object, 
            "Server=MyMockedConnectionString", 
            CancellationToken.None
        ), Times.Once);
    }

    [Fact]
    public async Task WithDbSeeder_Callback_ShouldPropagateExceptions()
    {
        // Arrange
        var builderMock = new Mock<ITestBuilder>();
        var containerMock = new Mock<IContainer>();
        var seederMock = new Mock<IDbSeeder>();

        Func<IContainer, CancellationToken, Task> capturedCallback = null!;

        builderMock.Setup(b => b.WithStartupCallback(It.IsAny<Func<IContainer, CancellationToken, Task>>()))
                   .Callback<Func<IContainer, CancellationToken, Task>>(cb => capturedCallback = cb)
                   .Returns(builderMock.Object);

        // Simulate a failure in the seeder (e.g., migration failed)
        var expectedException = new InvalidOperationException("Migration failed!");
        seederMock.Setup(s => s.SeedAsync(It.IsAny<IContainer>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(expectedException);

        builderMock.Object.WithDbSeeder(seederMock.Object, c => "conn");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            capturedCallback.Invoke(containerMock.Object, CancellationToken.None));
            
        Assert.Equal("Migration failed!", exception.Message);
    }
}