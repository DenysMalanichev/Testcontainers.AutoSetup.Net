using System.Data.SqlTypes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.AutoSetup.Core.Helpers;
using Testcontainers.AutoSetup.EntityFramework;
using Testcontainers.AutoSetup.EntityFramework.Entities;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations;
using Testcontainers.AutoSetup.Tests.TestCollections;
using Testcontainers.MsSql;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests;

[Trait("Category", "Integration")]
[Collection(nameof(ParallelTests))]
public class EntityFrameworkMigrationTests
{
    private readonly string? dockerEndpoint = DockerHelper.GetDockerEndpoint();

    [Fact]
    public async Task EfSeeder_WithMSSQLContainerBuilder_MigratesDatabase()
    {
        // Arrange 
        var efDbSetup = new EfDbSetup 
            { 
                DbName = "CatalogTest", 
                ContextFactory = connString => new CatalogContext(
                    new DbContextOptionsBuilder<CatalogContext>()
                    .UseSqlServer(connString)
                    .Options),
            };
        var seeder = new EfSeeder(tryRecreateFromDump: false, efDbSetup);

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
        await container.SeedAsync(seeder, (c) => c.GetConnectionString());

        // Assert
        Assert.NotNull(container);
        Assert.Equal(TestcontainersStates.Running, container.State);

        using var connection = new SqlConnection(efDbSetup.BuildConnectionString(container.GetConnectionString()));
        await connection.OpenAsync();
        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
    }

    [Fact]
    public async Task EfSeeder_WithMSSQLContainerBuilder_MigratesMultipleDatabases()
    {
        // Arrange 
        var efDbSetup1 = new EfDbSetup
        {
            DbName = "CatalogTest1",
            ContextFactory = connString => new CatalogContext(
                new DbContextOptionsBuilder<CatalogContext>()
                .UseSqlServer(connString)
                .Options),
        };
        var efDbSetup2 = new EfDbSetup
        {
            DbName = "CatalogTest2",
            ContextFactory = connString => new CatalogContext(
                new DbContextOptionsBuilder<CatalogContext>()
                .UseSqlServer(connString)
                .Options),
        };
        var seeder = new EfSeeder(tryRecreateFromDump: false, efDbSetup1, efDbSetup2);

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
        await container.SeedAsync(seeder, (c) => c.GetConnectionString());

        // Assert
        Assert.NotNull(container);
        Assert.Equal(TestcontainersStates.Running, container.State);

        using var connection = new SqlConnection(efDbSetup1.BuildConnectionString(container.GetConnectionString()));
        await connection.OpenAsync();
        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");

        using var connection2 = new SqlConnection(efDbSetup2.BuildConnectionString(container.GetConnectionString()));
        await connection2.OpenAsync();
        using var historyCmd2 = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection2);
        var migrationCount2 = (int)(await historyCmd2.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount2 > 0, "No migrations were found in the history table.");
    }

    [Fact]
    public async Task EfSeeder_WithGenericContainerBuilder_MigratesDatabase()
    {
        // Arrange 
        const string dbName = "CatalogTest_Generic";
        var efDbSetup = new EfDbSetup
        {
            DbName = dbName,
            ContextFactory = connString => new CatalogContext(
                new DbContextOptionsBuilder<CatalogContext>()
                .UseSqlServer(connString)
                .Options),
        };
        var seeder = new EfSeeder(tryRecreateFromDump: false, efDbSetup);

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
                // TODO move port to constant class
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
        string connectionString = $"Server={DockerHelper.DockerHostAddress},{mappedPort};Database={dbName};User ID=sa;Password=YourStrongPassword123!;Encrypt=False;";

        await container.SeedAsync(seeder, _ => connectionString);

        // Assert
        Assert.NotNull(container);
        Assert.Equal(TestcontainersStates.Running, container.State);

        using var connection = new SqlConnection(efDbSetup.BuildConnectionString(connectionString));
        await connection.OpenAsync();
        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
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
