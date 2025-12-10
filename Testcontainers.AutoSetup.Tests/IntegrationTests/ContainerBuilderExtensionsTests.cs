using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.AutoSetup.Core.Helpers;
using Testcontainers.MsSql;

namespace Testcontainers.AutoSetup.Tests;

[Trait("Category", "Integration")]
public class ContainerBuilderExtensionsTests
{
    private readonly string? dockerEndpoint = DockerHelper.GetDockerEndpoint();
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
        var builder = new MsSqlBuilder();
        if(dockerEndpoint is not null)
        {
            builder = builder.WithDockerEndpoint(dockerEndpoint);
        }
        var container = builder.WithName("MsSQL-testcontainer")
            .WithPassword("#AdminPass123")
            .WithReuse(reuse: !DockerHelper.IsCiRun())
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

    [Fact]
    public async Task ContainerBuilderExtensions_WithDbSeeder_HooksInsideTheGenericContainer()
    {
        // Arrange 
        const int systemPort = 23724;
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
        var builder = new ContainerBuilder();
        if(dockerEndpoint is not null)
        {
            builder = builder.WithDockerEndpoint(dockerEndpoint);
        }
        var container = builder
            .WithName("GenericMsSQL-testcontainer")
            .WithImage("mcr.microsoft.com/mssql/server:2025-latest")
            .WithPortBinding(1433, systemPort)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SA_PASSWORD", "YourStrongPassword123!")
            .WithReuse(reuse: !DockerHelper.IsCiRun())
            .WithDbSeeder(
                seederMock.Object,
                _ => $"Server=localhost,{systemPort};Database=master;User ID=sa;Password=YourStrongPassword123!;Encrypt=False;")
            .Build();
        await container.StartAsync();

        // Assert
        Assert.Single(seederMock.Invocations);
        Assert.NotNull(createdContainer);
        Assert.Equal(TestcontainersStates.Running, createdContainer.State);
    }
}
