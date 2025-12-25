using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.DbRestoration;
using Testcontainers.AutoSetup.Core.Helpers;
using Testcontainers.AutoSetup.EntityFramework;
using Testcontainers.AutoSetup.EntityFramework.Entities;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations;
using Testcontainers.MsSql;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core;
using Testcontainers.AutoSetup.Core.Extensions;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests;

public class GlobalTestSetup : GenericTestBase
{
    public MsSqlContainer MsSqlContainerFromSpecificBuilder = null!;
    public string? MsSqlContainerFromSpecificBuilderConnStr { get; private set; } = null!;
    public IContainer MsSqlContainerFromGenericBuilder = null!;
    public string? MsSqlContainerFromGenericBuilderConnStr { get; private set; } = null!;


    public readonly string? DockerEndpoint = EnvironmentHelper.GetDockerEndpoint();

    /// <inheritdoc/>
    public override async Task ConfigureSetupAsync()
    {
        // 1. Build & Start Containers
        MsSqlContainerFromSpecificBuilder = CreateMsSqlContainerFromSpecificBuilder();
        MsSqlContainerFromGenericBuilder = CreateMsSqlContainerFromGenericBuilder();
        
        await Task.WhenAll(
            MsSqlContainerFromSpecificBuilder.StartAsync(),
            MsSqlContainerFromGenericBuilder.StartAsync()
        );

        // 2. Register containers within the environment
        var dbSetup = MsSqlDbSetup(MsSqlContainerFromSpecificBuilder.GetConnectionString());        
        MsSqlContainerFromSpecificBuilderConnStr = dbSetup.BuildDbConnectionString(); 
        TestEnvironment.Register<EfSeeder, MsSqlDbRestorer>(
            dbSetup,
            MsSqlContainerFromSpecificBuilder);

        var mappedPort = MsSqlContainerFromGenericBuilder.GetMappedPublicPort(1433);
        var genericDbSetup = GenericMsSqlDbSetup(mappedPort);
        MsSqlContainerFromGenericBuilderConnStr = genericDbSetup.BuildDbConnectionString();
        TestEnvironment.Register<EfSeeder, MsSqlDbRestorer>(
            genericDbSetup,
            MsSqlContainerFromGenericBuilder);
    }

    /// <inheritdoc/>
    public override async Task ResetEnvironmentAsync(Type testClassType)
    {
        await OnTestStartAsync(testClassType);
    }

    private static MsSqlContainer CreateMsSqlContainerFromSpecificBuilder()
    {
        var builder = new MsSqlBuilder();
        var container = builder
            .WithAutoSetupDefaults(containerName: "MsSQL-testcontainer")
            .WithPassword("#AdminPass123")
            .Build();

        return container;
    }

    private static IContainer CreateMsSqlContainerFromGenericBuilder()
    {
        var builder = new ContainerBuilder();
        builder = builder.WithAutoSetupDefaults(containerName: "GenericMsSQL-testcontainer");
        if (EnvironmentHelper.IsCiRun())
        {
            builder = builder.WithPortBinding(1433, assignRandomHostPort: true);
        }
        else
        {
            builder = builder.WithPortBinding(Constants.GenericContainerPort, 1433);
        }
        var container = builder
            .WithImage("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04")
            .WithEnvironment("ACCEPT_EULA", "Y")            
            .WithEnvironment("MSSQL_SA_PASSWORD", "YourStrongPassword123!")
            .WithEnvironment("SQLCMDPASSWORD", "YourStrongPassword123!")
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitUntil()))       
            .Build();

        return container;
    }

    private static EfDbSetup MsSqlDbSetup(string containerConnectionString) => new() 
            {
                DbName = "CatalogTest", 
                DbType = Core.Common.Enums.DbType.MsSQL,
                ContainerConnectionString = containerConnectionString,
                ContextFactory = connString => new CatalogContext(
                    new DbContextOptionsBuilder<CatalogContext>()
                    .UseSqlServer(connString)
                    .Options),
                MigrationsPath = "./IntegrationTests/Migrations",
            };

    private static EfDbSetup GenericMsSqlDbSetup(int mappedPort) => new() 
            {
                DbType = Core.Common.Enums.DbType.MsSQL,
                DbName = "GenericCatalogTest", 
                ContainerConnectionString = $"Server={EnvironmentHelper.DockerHostAddress},{mappedPort};Database=GenericCatalogTest;User ID=sa;Password=YourStrongPassword123!;Encrypt=False;",
                ContextFactory = connString => new CatalogContext(
                    new DbContextOptionsBuilder<CatalogContext>()
                    .UseSqlServer(connString)
                    .Options),
                MigrationsPath = "./IntegrationTests/Migrations",
            };

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
