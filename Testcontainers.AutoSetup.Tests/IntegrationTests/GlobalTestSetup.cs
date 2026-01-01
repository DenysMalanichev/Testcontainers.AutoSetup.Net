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
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.DbSeeding;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
namespace Testcontainers.AutoSetup.Tests.IntegrationTests;

public class GlobalTestSetup : GenericTestBase
{
    public MsSqlContainer MsSqlContainerFromSpecificBuilder = null!;
    public DbSetup? MsSqlContainer_SpecificBuilder_EfDbSetup { get; private set; } = null!;
    public DbSetup? MsSqlContainer_SpecificBuilder_RawSqlDbSetup { get; private set; } = null!;
    public IContainer MsSqlContainerFromGenericBuilder = null!;
    public DbSetup? MsSqlContainer_GenericBuilder_EfDbSetup { get; private set; } = null!;
    public DbSetup? MsSqlContainer_GenericBuilder_RawSqlDbSetup { get; private set; } = null!;

    public readonly string? DockerEndpoint = EnvironmentHelper.GetDockerEndpoint();

    public GlobalTestSetup(ILogger? logger = null)
        : base(logger)
    { }

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

        // Register MsSql with EF Seeder
        MsSqlContainer_SpecificBuilder_EfDbSetup = MsSqlEFDbSetup(MsSqlContainerFromSpecificBuilder.GetConnectionString());        
        TestEnvironment.Register<EfSeeder, MsSqlDbRestorer>(
            MsSqlContainer_SpecificBuilder_EfDbSetup,
            MsSqlContainerFromSpecificBuilder,
            logger: Logger);
        // Register MsSql with Raw SQL Seeder
        MsSqlContainer_SpecificBuilder_RawSqlDbSetup = MsSqlRawSqlDbSetup(MsSqlContainerFromSpecificBuilder.GetConnectionString());        
        TestEnvironment.Register<RawSqlDbSeeder, MsSqlDbRestorer>(
            MsSqlContainer_SpecificBuilder_RawSqlDbSetup,
            MsSqlContainerFromSpecificBuilder,
            logger: Logger);

        // Register Generic MsSql with EF Seeder
        var mappedPort = MsSqlContainerFromGenericBuilder.GetMappedPublicPort(1433);
        MsSqlContainer_GenericBuilder_EfDbSetup = GenericMsSqlEFDbSetup(mappedPort);
        TestEnvironment.Register<EfSeeder, MsSqlDbRestorer>(
            MsSqlContainer_GenericBuilder_EfDbSetup,
            MsSqlContainerFromGenericBuilder,
            logger: Logger);

        // Register Generic MsSql with Raw SQL Seeder
        MsSqlContainer_GenericBuilder_RawSqlDbSetup = GenericMsSqlRawSqlDbSetup(mappedPort);
        TestEnvironment.Register<RawSqlDbSeeder, MsSqlDbRestorer>(
            MsSqlContainer_GenericBuilder_RawSqlDbSetup,
            MsSqlContainerFromGenericBuilder,
            logger: Logger);
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

    private static EfDbSetup MsSqlEFDbSetup(string containerConnectionString) => new() 
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

    private static EfDbSetup GenericMsSqlEFDbSetup(int mappedPort) => new() 
            {
                DbType = Core.Common.Enums.DbType.MsSQL,
                DbName = "GenericCatalogTest", 
                ContainerConnectionString = $"Server={EnvironmentHelper.DockerHostAddress},{mappedPort};Database=master;User ID=sa;Password=YourStrongPassword123!;Encrypt=False;",
                ContextFactory = connString => new CatalogContext(
                    new DbContextOptionsBuilder<CatalogContext>()
                    .UseSqlServer(connString)
                    .Options),
                MigrationsPath = "./IntegrationTests/Migrations",
            };
    
    private static RawSqlDbSetup MsSqlRawSqlDbSetup(string containerConnectionString) => new() 
            {
                DbName = "RawSql_CatalogTest", 
                DbType = Core.Common.Enums.DbType.MsSQL,
                ContainerConnectionString = containerConnectionString,
                MigrationsPath = "./IntegrationTests/Migrations/SqlScripts",
                SqlFiles = 
                [
                    "001_CreateCatalogTable.sql",
                    "002_InsertInitialData.sql"
                ]
            };

    private static RawSqlDbSetup GenericMsSqlRawSqlDbSetup(int mappedPort) => new() 
            {
                DbType = Core.Common.Enums.DbType.MsSQL,
                DbName = "RawSql_CatalogTest", 
                ContainerConnectionString = $"Server={EnvironmentHelper.DockerHostAddress},{mappedPort};Database=master;User ID=sa;Password=YourStrongPassword123!;Encrypt=False;",
                MigrationsPath = "./IntegrationTests/Migrations/SqlScripts",
                SqlFiles = 
                [
                    "001_CreateCatalogTable.sql",
                    "002_InsertInitialData.sql"
                ]
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
