using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.AutoSetup.Core.Helpers;
using Testcontainers.AutoSetup.EntityFramework;
using Testcontainers.AutoSetup.Tests.TestCollections;
using Testcontainers.MsSql;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests;

[Trait("Category", "Integration")]
[Collection(nameof(ParallelTests))]
public class EntityFrameworkMigrationTests
{
    private readonly string? dockerEndpoint = DockerHelper.GetDockerEndpoint();

    [Fact]
    public async Task ContainerBuilderExtensions_WithDbSeeder_HooksInsideTheContainer_WriteAfterTheStartup()
    {
        // Arrange 
        IContainer createdContainer = null!;
        var seeder = new EfSeeder();

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
                seeder, (c) => c.GetConnectionString())
            .Build();
        await container.StartAsync();

        // Assert
        Assert.NotNull(createdContainer);
        Assert.Equal(TestcontainersStates.Running, createdContainer.State);
    }
}
