using System.Data.SqlTypes;
using System.Diagnostics;
using DotNet.Testcontainers.Containers;
using MySql.Data.MySqlClient;
using Testcontainers.AutoSetup.Core.Attributes;
using Testcontainers.AutoSetup.Tests.IntegrationTests.TestCollections;
using Xunit.Abstractions;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.DbRestoration;

[DbReset]
[Trait("Category", "Integration")]
[Collection(nameof(ParallelIntegrationTestsCollection))]
public class MySqlRestorationTests : IntegrationTestsBase
{
     private readonly ITestOutputHelper _output;

    public MySqlRestorationTests(ITestOutputHelper output, ContainersFixture fixture)
        : base(fixture)
    {
        _output = output;
    }

    [Fact]
    public async Task MySqlRestorer_WithMySQLContainerBuilder_MigratesDatabase()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MySqlContainerFromSpecificBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MySqlContainerFromSpecificBuilder.State);

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new MySqlConnection(Setup.MySqlContainer_SpecificBuilder_EfDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();
        stopwatch.Stop();
        Console.WriteLine("[CONNECTION OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new MySqlCommand("SELECT COUNT(*) FROM `CatalogTestMySql`.`__EFMigrationsHistory`", connection);
        var migrationCount = (long)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task MySqlRestorer_WithGenericMySQLContainerBuilder_MigratesDatabase()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MySqlContainerFromGenericBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MySqlContainerFromGenericBuilder.State);

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new MySqlConnection(Setup.MySqlContainer_GenericBuilder_EfDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();
        stopwatch.Stop();
        Console.WriteLine("[CONNECTION OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new MySqlCommand("SELECT COUNT(*) FROM `GenericCatalogTestMySql`.`__EFMigrationsHistory`", connection);
        var migrationCount = (long)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task MySqlRestorer_WithSpecificMySQLContainerBuilder_MigratesRawSqlDatabase()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MySqlContainerFromSpecificBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MySqlContainerFromSpecificBuilder.State);

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new MySqlConnection(Setup.MySqlContainer_SpecificBuilder_EfDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();
        stopwatch.Stop();
        Console.WriteLine("[CONNECTION OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new MySqlCommand("SELECT COUNT(*) FROM `RawSql_CatalogTest`.`Catalog`", connection);
        var migrationCount = (long)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
    }

    [Fact]
    public async Task MySqlRestorer_WithGenericMySQLContainerBuilder_MigratesRawSqlDatabase()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MySqlContainerFromGenericBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MySqlContainerFromGenericBuilder.State);

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new MySqlConnection(Setup.MySqlContainer_GenericBuilder_EfDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();
        stopwatch.Stop();
        Console.WriteLine("[CONNECTION OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new MySqlCommand("SELECT COUNT(*) FROM `RawSql_CatalogTest`.`Catalog`", connection);
        var migrationCount = (long)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
    }

    [Fact]
    public async Task MySqlRestorer_WithGenericMySQLContainerBuilder_RemovesCreatedTableAfterTest()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MySqlContainerFromGenericBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MySqlContainerFromGenericBuilder.State);

        await using var connection = new MySqlConnection(Setup.MySqlContainer_GenericBuilder_EfDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();

        using var historyCmd = new MySqlCommand("CREATE TABLE TestTable (Id INT PRIMARY KEY)", connection);
        await historyCmd.ExecuteNonQueryAsync();

        // Trigger the reset
        await Setup.ResetEnvironmentAsync(this.GetType());

        using var checkTableCmd = new MySqlCommand("SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = 'GenericCatalogTestMySql' AND table_name = 'TestTable'", connection);
        var result = (long?)await checkTableCmd.ExecuteScalarAsync();

        Assert.NotNull(result); // Failed executing a query to check for the existence of TestTable after reset
        Assert.Equal(0, result); // TestTable should have been removed during the reset process
    }
}