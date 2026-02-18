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

    [Fact]
    public async Task MySqlRestorer_WithGenericMySQLContainerBuilder_RemovesCreatedViewAfterTest()
    {
        // Arrange
        Assert.NotNull(Setup.MySqlContainerFromGenericBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MySqlContainerFromGenericBuilder.State);

        await using var connection = new MySqlConnection(Setup.MySqlContainer_GenericBuilder_EfDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();

        // Act
        using var viewCmd = new MySqlCommand("CREATE VIEW TestView AS SELECT 1 AS Number", connection);
        await viewCmd.ExecuteNonQueryAsync();

        await Setup.ResetEnvironmentAsync(this.GetType());

        // Assert
        using var checkViewCmd = new MySqlCommand(
            "SELECT COUNT(1) FROM information_schema.views WHERE table_schema = 'GenericCatalogTestMySql' AND table_name = 'TestView'",
            connection);

        var result = (long?)await checkViewCmd.ExecuteScalarAsync();

        Assert.NotNull(result);
        Assert.Equal(0, result); // TestView should be removed
    }

    [Fact]
    public async Task MySqlRestorer_WithGenericMySQLContainerBuilder_RemovesCreatedStoredProcedureAfterTest()
    {
        // Arrange
        Assert.NotNull(Setup.MySqlContainerFromGenericBuilder);

        await using var connection = new MySqlConnection(Setup.MySqlContainer_GenericBuilder_EfDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();

        // Act
        using var procCmd = new MySqlCommand(
            "CREATE PROCEDURE TestProcedure() BEGIN SELECT 1; END",
            connection);
        await procCmd.ExecuteNonQueryAsync();

        await Setup.ResetEnvironmentAsync(this.GetType());

        // Assert
        // Note: We check 'ROUTINES', not 'TABLES'
        using var checkProcCmd = new MySqlCommand(
            "SELECT COUNT(1) FROM information_schema.routines WHERE routine_schema = 'GenericCatalogTestMySql' AND routine_name = 'TestProcedure' AND routine_type = 'PROCEDURE'",
            connection);

        var result = (long?)await checkProcCmd.ExecuteScalarAsync();

        Assert.NotNull(result);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task MySqlRestorer_WithGenericMySQLContainerBuilder_RemovesCreatedTriggerAfterTest()
    {
        // Arrange
        Assert.NotNull(Setup.MySqlContainerFromGenericBuilder);

        await using var connection = new MySqlConnection(Setup.MySqlContainer_GenericBuilder_EfDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();

        // Act
        using var tableCmd = new MySqlCommand("CREATE TABLE TriggerHostTable (Id INT)", connection);
        await tableCmd.ExecuteNonQueryAsync();

        using var triggerCmd = new MySqlCommand(
            "CREATE TRIGGER TestTrigger BEFORE INSERT ON TriggerHostTable FOR EACH ROW SET NEW.Id = NEW.Id + 1",
            connection);
        await triggerCmd.ExecuteNonQueryAsync();

        await Setup.ResetEnvironmentAsync(this.GetType());

        // Assert
        using var checkTriggerCmd = new MySqlCommand(
            "SELECT COUNT(1) FROM information_schema.triggers WHERE trigger_schema = 'GenericCatalogTestMySql' AND trigger_name = 'TestTrigger'",
            connection);

        var result = (long?)await checkTriggerCmd.ExecuteScalarAsync();

        Assert.NotNull(result);
        Assert.Equal(0, result);
    }
}