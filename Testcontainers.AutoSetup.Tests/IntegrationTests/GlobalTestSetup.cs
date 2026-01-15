using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.DbRestoration;
using Testcontainers.AutoSetup.Core.Helpers;
using Testcontainers.AutoSetup.EntityFramework;
using Testcontainers.AutoSetup.EntityFramework.Entities;
using Testcontainers.MsSql;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core;
using Testcontainers.AutoSetup.Core.Extensions;
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.MySql;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations;
using Testcontainers.AutoSetup.Tests.IntegrationTests.TestHelpers;
using Testcontainers.AutoSetup.Core.DbSeeding;
using Testcontainers.MongoDb;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests;

public class GlobalTestSetup : GenericTestBase
{
    public MsSqlContainer MsSqlContainerFromSpecificBuilder = null!;
    public DbSetup? MsSqlContainer_SpecificBuilder_EfDbSetup { get; private set; } = null!;
    public DbSetup? MsSqlContainer_SpecificBuilder_RawSqlDbSetup { get; private set; } = null!;
    public IContainer MsSqlContainerFromGenericBuilder = null!;
    public DbSetup? MsSqlContainer_GenericBuilder_EfDbSetup { get; private set; } = null!;
    public DbSetup? MsSqlContainer_GenericBuilder_RawSqlDbSetup { get; private set; } = null!;
    public MySqlContainer MySqlContainerFromSpecificBuilder = null!;
    public DbSetup? MySqlContainer_SpecificBuilder_EfDbSetup { get; private set; } = null!;
    public IContainer MySqlContainerFromGenericBuilder = null!;
    public DbSetup? MySqlContainer_GenericBuilder_EfDbSetup { get; private set; } = null!;
    public MongoDbContainer MongoContainerFromSpecificBuilder = null!;
    public DbSetup? MongoContainer_FromSpecificBuilder_RawMongoDbSetup { get; private set; } = null!;

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

        MySqlContainerFromSpecificBuilder = CreateMySqlContainerFromSpecificBuilder();
        MySqlContainerFromGenericBuilder = CreateMySqlContainerFromGenericBuilder();

        MongoContainerFromSpecificBuilder = CreateMongoDbContainerFromSpecificBuilder();
        
        await Task.WhenAll(
            MsSqlContainerFromSpecificBuilder.StartAsync(),
            MsSqlContainerFromGenericBuilder.StartAsync(),

            MySqlContainerFromSpecificBuilder.StartAsync(),
            MySqlContainerFromGenericBuilder.StartAsync(),

            MongoContainerFromSpecificBuilder.StartAsync()
        );

        // 2. Register containers within the environment

        // Register MsSql with EF Seeder
        MsSqlContainer_SpecificBuilder_EfDbSetup = MsSqlEFDbSetup(MsSqlContainerFromSpecificBuilder.GetConnectionString());        
        TestEnvironment.RegisterSqlDb<EfSeeder, MsSqlDbRestorer>(
            MsSqlContainer_SpecificBuilder_EfDbSetup,
            MsSqlContainerFromSpecificBuilder,
            new MsSqlDbConnectionFactory(),
            logger: Logger);

        // Register MsSql with Raw SQL Seeder
        MsSqlContainer_SpecificBuilder_RawSqlDbSetup = MsSqlRawSqlDbSetup(MsSqlContainerFromSpecificBuilder.GetConnectionString());        
        TestEnvironment.RegisterSqlDb<RawSqlDbSeeder, MsSqlDbRestorer>(
            MsSqlContainer_SpecificBuilder_RawSqlDbSetup,
            MsSqlContainerFromSpecificBuilder,
            new MsSqlDbConnectionFactory(),
            logger: Logger);

        // Register Generic MsSql with EF Seeder
        var mappedPort = MsSqlContainerFromGenericBuilder.GetMappedPublicPort(1433);
        MsSqlContainer_GenericBuilder_EfDbSetup = GenericMsSqlEFDbSetup(mappedPort);
        TestEnvironment.RegisterSqlDb<EfSeeder, MsSqlDbRestorer>(
            MsSqlContainer_GenericBuilder_EfDbSetup,
            MsSqlContainerFromGenericBuilder,
            new MsSqlDbConnectionFactory(),
            logger: Logger);

        // Register Generic MsSql with Raw SQL Seeder
        MsSqlContainer_GenericBuilder_RawSqlDbSetup = GenericMsSqlRawSqlDbSetup(mappedPort);
        TestEnvironment.RegisterSqlDb<RawSqlDbSeeder, MsSqlDbRestorer>(
            MsSqlContainer_GenericBuilder_RawSqlDbSetup,
            MsSqlContainerFromGenericBuilder,
            new MsSqlDbConnectionFactory(),
            logger: Logger);

        // Register MySql container with EF Seeder
        MySqlContainer_SpecificBuilder_EfDbSetup = MySqlEFDbSetup(MySqlContainerFromSpecificBuilder.GetConnectionString());
        TestEnvironment.RegisterSqlDb<EfSeeder, MySqlDbRestorer>(
            MySqlContainer_SpecificBuilder_EfDbSetup,
            MySqlContainerFromSpecificBuilder,
            new MySqlDbConnectionFactory(),
            logger: Logger);

        // Register MySql container with Raw SQL Seeder
        MySqlContainer_SpecificBuilder_EfDbSetup = MySqlRawSqlDbSetup(MySqlContainerFromSpecificBuilder.GetConnectionString());
        TestEnvironment.RegisterSqlDb<RawSqlDbSeeder, MySqlDbRestorer>(
            MySqlContainer_SpecificBuilder_EfDbSetup,
            MySqlContainerFromSpecificBuilder,
            new MySqlDbConnectionFactory(),
            logger: Logger);

        // Register MySql container with EF Seeder
        var mappedPortMySql = MySqlContainerFromGenericBuilder.GetMappedPublicPort(3306);
        MySqlContainer_GenericBuilder_EfDbSetup = GenericMySqlEFDbSetup(mappedPortMySql);
        TestEnvironment.RegisterSqlDb<EfSeeder, MySqlDbRestorer>(
            MySqlContainer_GenericBuilder_EfDbSetup,
            MySqlContainerFromGenericBuilder,
            new MySqlDbConnectionFactory(),
            logger: Logger);
            
        // Register MySql container with EF Seeder
        MySqlContainer_GenericBuilder_EfDbSetup = GenericMySqlRawSqlDbSetup(mappedPortMySql);
        TestEnvironment.RegisterSqlDb<RawSqlDbSeeder, MySqlDbRestorer>(
            MySqlContainer_GenericBuilder_EfDbSetup,
            MySqlContainerFromGenericBuilder,
            new MySqlDbConnectionFactory(),
            logger: Logger);

        // Register MongoDB container with raw files Seeder
        MongoContainer_FromSpecificBuilder_RawMongoDbSetup = SpecificMongoDbRawDbSetup(MongoContainerFromSpecificBuilder.GetConnectionString());
        TestEnvironment.RegisterMongoDb<RawMongoDbSeeder, MongoDbRestorer>(
            MongoContainer_FromSpecificBuilder_RawMongoDbSetup,
            MongoContainerFromSpecificBuilder,
            logger: Logger);
    }

    /// <inheritdoc/>
    public override async Task ResetEnvironmentAsync(Type testClassType)
    {
        await OnTestStartAsync(testClassType);
    }

    private static MsSqlContainer CreateMsSqlContainerFromSpecificBuilder()
    {
        var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04");
        var container = builder
            .WithMSSQLAutoSetupDefaults(containerName: "MsSQL-testcontainer")
            .WithPassword("#AdminPass123")
            .Build();

        return container;
    }

    private static IContainer CreateMsSqlContainerFromGenericBuilder()
    {
        var builder = new ContainerBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04");
        builder = builder.WithMSSQLAutoSetupDefaults(containerName: "GenericMsSQL-testcontainer")
            .WithPortBinding(1433, assignRandomHostPort: true);
        var container = builder
            .WithEnvironment("ACCEPT_EULA", "Y")            
            .WithEnvironment("MSSQL_SA_PASSWORD", "YourStrongPassword123!")
            .WithEnvironment("SQLCMDPASSWORD", "YourStrongPassword123!")
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitUntil()))       
            .Build();

        return container;
    }

    private static MySqlContainer CreateMySqlContainerFromSpecificBuilder()
    {
        var builder = new MySqlBuilder("mysql:8.0.44-debian");
        var container = builder
            .WithMySQLAutoSetupDefaults(containerName: "MySQL-testcontainer")
            .WithUsername("root")
            .WithCommand("--skip-name-resolve")
            .Build();

        return container;
    }
    
    private static IContainer CreateMySqlContainerFromGenericBuilder()
    {
        var builder = new ContainerBuilder("mysql:8.0.44-debian");
        builder = builder.WithMySQLAutoSetupDefaults(containerName: "GenericMySQL-testcontainer")
           .WithPortBinding(3306, assignRandomHostPort: true);
        var container = builder
            .WithEnvironment("MYSQL_ROOT_PASSWORD", "mysql")
            .WithCommand("--skip-name-resolve")
            .Build();

        return container;
    }

    private static MongoDbContainer CreateMongoDbContainerFromSpecificBuilder()
    {
        var builder = new MongoDbBuilder("mongo:6.0.27-jammy");
        builder = builder.WithMongoAutoSetupDefaults(
            containerName: "Mongo-testcontainer",
            migrationsPath: "./IntegrationTests/Migrations/MongoDB");
        var container = builder.Build();

        return container;
    }

    private static EfDbSetup MsSqlEFDbSetup(string containerConnectionString) => new(
        dbName: "CatalogTest",
        dbType: Core.Common.Enums.DbType.MsSQL,
        containerConnectionString: containerConnectionString,
        contextFactory: connString => new MSSQLCatalogContext(
            new DbContextOptionsBuilder<MSSQLCatalogContext>()
                .UseSqlServer(connString)
                .Options),
        migrationsPath: "./IntegrationTests/Migrations/MSSQL/EfMigrations"
    )   ;

    private static EfDbSetup GenericMsSqlEFDbSetup(int mappedPort) => new(
            dbType: Core.Common.Enums.DbType.MsSQL,
            dbName: "GenericCatalogTest", 
            containerConnectionString: $"Server={EnvironmentHelper.DockerHostAddress},{mappedPort};Database=master;User ID=sa;Password=YourStrongPassword123!;Encrypt=False;",
            contextFactory: connString => new MSSQLCatalogContext(
                new DbContextOptionsBuilder<MSSQLCatalogContext>()
                    .UseSqlServer(connString)
                    .Options),
            migrationsPath: "./IntegrationTests/Migrations/MSSQL/EfMigrations"
        );
    
    private static RawSqlDbSetup MsSqlRawSqlDbSetup(string containerConnectionString) => new(
            dbName: "RawSql_CatalogTest", 
            dbType: Core.Common.Enums.DbType.MsSQL,
            containerConnectionString: containerConnectionString,
            migrationsPath: "./IntegrationTests/Migrations/MSSQL/SqlScripts",
            sqlFiles: 
                [
                    "001_MSSQL_CreateCatalogTable.sql",
                    "002_MSSQL_InsertInitialData.sql"
                ]
        );

    private static RawSqlDbSetup GenericMsSqlRawSqlDbSetup(int mappedPort) => new(
            dbName: "RawSql_CatalogTest", 
            dbType: Core.Common.Enums.DbType.MsSQL,
            containerConnectionString: $"Server={EnvironmentHelper.DockerHostAddress},{mappedPort};Database=master;User ID=sa;Password=YourStrongPassword123!;Encrypt=False;",
            migrationsPath: "./IntegrationTests/Migrations/MSSQL/SqlScripts",
            sqlFiles: 
                [
                    "001_MSSQL_CreateCatalogTable.sql",
                    "002_MSSQL_InsertInitialData.sql"
                ]
        );

    private static EfDbSetup MySqlEFDbSetup(string containerConnectionString) => new( 
            dbName: "CatalogTestMySql", 
            dbType: Core.Common.Enums.DbType.MySQL,
            containerConnectionString: containerConnectionString,
            contextFactory: connString => new MySQLCatalogContext(
                new DbContextOptionsBuilder<MySQLCatalogContext>()
                    .UseMySQL(connString, providerOptions => { providerOptions.EnableRetryOnFailure(); })
                    .Options),
            migrationsPath: "./IntegrationTests/Migrations/MySQL/EfMigrations"
        );

    private static RawSqlDbSetup MySqlRawSqlDbSetup(string connectionString) => new(
            dbName: "RawSql_CatalogTest", 
            dbType: Core.Common.Enums.DbType.MySQL,
            containerConnectionString: connectionString,
            migrationsPath: "./IntegrationTests/Migrations/MySQL/SqlScripts",
            sqlFiles: 
                [
                    "001_MySQL_CreateCatalogTable.sql",
                    "002_MySQL_InsertInitialData.sql"
                ]
        );

    private static EfDbSetup GenericMySqlEFDbSetup(int mappedPort) => new( 
            dbName: "GenericCatalogTestMySql", 
            dbType: Core.Common.Enums.DbType.MySQL,
            containerConnectionString: $"Server={EnvironmentHelper.DockerHostAddress};Port={mappedPort};Database=mysql;Uid=root;Pwd=mysql;",
            contextFactory: connString => new MySQLCatalogContext(
                new DbContextOptionsBuilder<MySQLCatalogContext>()
                    .UseMySQL(connString, providerOptions => { providerOptions.EnableRetryOnFailure(); })
                    .Options),
            migrationsPath: "./IntegrationTests/Migrations/MySQL/EfMigrations"
        );

    private static RawSqlDbSetup GenericMySqlRawSqlDbSetup(int mappedPort) => new(
            dbName: "RawSql_CatalogTest",
            dbType: Core.Common.Enums.DbType.MySQL,
            containerConnectionString: $"Server={EnvironmentHelper.DockerHostAddress};Port={mappedPort};Database=mysql;Uid=root;Pwd=mysql;",
            migrationsPath: "./IntegrationTests/Migrations/MySQL/SqlScripts",
            sqlFiles:
                [
                    "001_MySQL_CreateCatalogTable.sql",
                    "002_MySQL_InsertInitialData.sql"
                ]
        );

    private static RawMongoDbSetup SpecificMongoDbRawDbSetup(string connectionString) => new(
            dbName: "MongoTest",
            dbType: Core.Common.Enums.DbType.MySQL,
            migrationsPath: "./IntegrationTests/Migrations/MongoDB",
            mongoFiles:
                new Dictionary<string, string>()
                {
                    {"orders", "orders.json"},
                    {"users", "users.json"}
                }      
        );

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
