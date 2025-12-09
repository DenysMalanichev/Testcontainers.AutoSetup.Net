using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.AutoSetup.Tests.Helpers;
using Testcontainers.MsSql;

namespace Testcontainers.AutoSetup.Tests;

[Trait("Category", "Integration")]
public class ContainerBuilderExtensionsTests
{
    private readonly string? dockerEndpoint = DockerAddressHelper.GetDockerEndpoint();
    private readonly IServiceProvider emptyServiceProvider = new ServiceCollection().BuildServiceProvider();

    [Fact]
    public async Task ContainerBuilderExtensions_WithDbSeeder_HooksInsideTheContainer_WriteAfterTheStartup()
    {
        // Arrange 
        IContainer createdContainer = null!;
        var seederMock = new Mock<IDbSeeder>();
        seederMock.Setup(
            (seeder) =>
                seeder.SeedAsync(
                    It.IsAny<IContainer>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>())
        ).Callback((IContainer container, string _, CancellationToken _) => createdContainer = container);

        // Act
        System.Console.WriteLine(dockerEndpoint);
        var container = new MsSqlBuilder()
            .WithName("MsSQL-testcontainer")
            .WithDockerEndpoint(dockerEndpoint)
            .WithPassword("#AdminPass123")
            .WithReuse(reuse: !DockerAddressHelper.IsCiRun())
            .WithLabel("reuse-id", "MsSQL-testcontainer-reuse-hash")
            .WithDbSeeder(
                seederMock.Object, (c) => c.GetConnectionString())
            .Build();
        await container.StartAsync();

        // Assert
        Assert.Single(seederMock.Invocations);
        Assert.NotNull(createdContainer);
        Assert.Equal(TestcontainersStates.Running, createdContainer.State);
    }
}
