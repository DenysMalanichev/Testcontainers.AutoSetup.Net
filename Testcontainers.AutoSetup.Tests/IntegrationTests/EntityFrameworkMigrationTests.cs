using System.Data.SqlTypes;
using DotNet.Testcontainers.Builders;
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
[Collection(nameof(SequentialTests))]
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
            builder = builder.WithName("MsSQL-testcontainer")
                .WithLabel("reuse-id", "MsSQL-testcontainer-reuse-hash");
        }
        var container = builder
            .WithPassword("#AdminPass123")
            .WithReuse(reuse: !DockerHelper.IsCiRun())
            
            .WithDbSeeder(
                seeder, (c) => c.GetConnectionString())
            .Build();
        await container.StartAsync();

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
            builder = builder.WithDockerEndpoint(dockerEndpoint)
                .WithLabel("reuse-id", "MsSQL-testcontainer-reuse-hash");
        }
        if(!DockerHelper.IsCiRun())
        {
            builder = builder.WithName("MsSQL-testcontainer");
        }
        var container = builder
            .WithPassword("#AdminPass123")
            .WithReuse(reuse: !DockerHelper.IsCiRun())            
            .WithDbSeeder(
                seeder, (c) => c.GetConnectionString())
            .Build();
        await container.StartAsync();

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
        const int systemPort = 23724;
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

        string connectionString = $"Server={DockerHelper.DockerHostAddress},{systemPort};Database={dbName};User ID=sa;Password=YourStrongPassword123!;Encrypt=False;";

        // Act
        var builder = new ContainerBuilder();
        if(dockerEndpoint is not null)
        {
            builder = builder.WithDockerEndpoint(dockerEndpoint);
        }
        if(!DockerHelper.IsCiRun())
        {
            builder = builder.WithName("GenericMsSQL-testcontainer")
                .WithLabel("reuse-id", "GenericMsSQL-testcontainer-reuse-hash");
        }
        var container = builder
            .WithImage("mcr.microsoft.com/mssql/server:2025-latest")
            .WithPortBinding(systemPort, 1433)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SA_PASSWORD", "YourStrongPassword123!")
            .WithReuse(reuse: !DockerHelper.IsCiRun())
            .WithDbSeeder(seeder, _ => connectionString)
            .Build();
        await container.StartAsync();

        // Assert
        Assert.NotNull(container);
        Assert.Equal(TestcontainersStates.Running, container.State);

        using var connection = new SqlConnection(efDbSetup.BuildConnectionString(connectionString));
        await connection.OpenAsync();
        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
    }
}
