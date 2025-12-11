using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.AutoSetup.Core.Helpers;
using Testcontainers.AutoSetup.Tests.TestCollections;
using Testcontainers.MsSql;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests;

[Trait("Category", "Integration")]
[Collection(nameof(ParallelTests))]
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
        if(!DockerHelper.IsCiRun())
        {
            builder = builder
                .WithName("MsSQL-testcontainer")
                .WithReuse(reuse: true)
                .WithLabel("reuse-id", "MsSQL-testcontainer-reuse-hash");
        }
        var container = builder
            .WithPassword("#AdminPass123")
            .Build();
        await container.StartAsync();
        await container.SeedAsync(seederMock.Object, (c) => c.GetConnectionString());

        // Assert
        Assert.Single(seederMock.Invocations);
        Assert.NotNull(createdContainer);
        Assert.Equal(TestcontainersStates.Running, createdContainer.State);
    }

    [Fact]
    public async Task ContainerBuilderExtensions_WithDbSeeder_HooksInsideTheGenericContainer()
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
        var builder = new ContainerBuilder();
        if(dockerEndpoint is not null)
        {
            builder = builder.WithDockerEndpoint(dockerEndpoint);
        }
        if(!DockerHelper.IsCiRun())
        {
            builder = builder
                .WithName("GenericMsSQL-testcontainer")
                .WithReuse(reuse: true)
                .WithLabel("reuse-id", "GenericMsSQL-testcontainer-reuse-hash")
                .WithPortBinding(23578, 1433);
        }
        else
        {
            builder = builder.WithPortBinding(1433, assignRandomHostPort: true);
        }
        var container = builder
            .WithImage("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", "YourStrongPassword123!")
            .WithEnvironment("SQLCMDPASSWORD", "YourStrongPassword123!")
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitUntil()))
            .Build();

        await container.StartAsync();  
        var mappedPort = container.GetMappedPublicPort(1433);
        string connectionString = $"Server={DockerHelper.DockerHostAddress},{mappedPort};User ID=sa;Password=YourStrongPassword123!;Encrypt=False;";
                
        await container.SeedAsync(seederMock.Object,
                _ => connectionString);

        // Assert
        Assert.Single(seederMock.Invocations);
        Assert.NotNull(createdContainer);
        Assert.Equal(TestcontainersStates.Running, createdContainer.State);
    }

    /// <inheritdoc cref="IWaitUntil" />
    /// <remarks>
    /// Uses the sqlcmd utility scripting variables to detect readiness of the MsSql container:
    /// https://learn.microsoft.com/en-us/sql/tools/sqlcmd/sqlcmd-utility?view=sql-server-linux-ver15#sqlcmd-scripting-variables.
    /// </remarks>
    private sealed class WaitUntil : IWaitUntil
    {
        private readonly string[] _command = { "/opt/mssql-tools/bin/sqlcmd", "-Q", "SELECT 1;", "-U", "sa" };

        /// <inheritdoc />
        public async Task<bool> UntilAsync(IContainer container)
        {
            var execResult = await container.ExecAsync(_command)
                .ConfigureAwait(false);

            return 0L.Equals(execResult.ExitCode);
        }
    }
}
