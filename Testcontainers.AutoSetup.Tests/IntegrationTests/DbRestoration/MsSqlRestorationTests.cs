using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Tests.TestCollections;
using Microsoft.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.DbRestoration;

[Trait("Category", "Integration")]
[Collection(nameof(ParallelTests))]
public class MsSqlRestorationTests(ContainersFixture fixture) : IntegrationTestsBase(fixture)
{
    [Fact]
    public async Task EfSeeder_WithMSSQLContainerBuilder_MigratesDatabase()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MsSqlContainerFromSpecificBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MsSqlContainerFromSpecificBuilder.State);

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new SqlConnection(Setup.MsSqlContainerFromSpecificBuilderConnStr);
        await connection.OpenAsync();
        stopwatch.Stop();
        System.Console.WriteLine("[CONNECTION OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task EfSeeder_WithGenericContainerBuilder_MigratesDatabase()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MsSqlContainerFromGenericBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MsSqlContainerFromGenericBuilder.State);
        
        var stopwatch = Stopwatch.StartNew();
        await using var connection = new SqlConnection(Setup.MsSqlContainerFromGenericBuilderConnStr);
        await connection.OpenAsync();
        stopwatch.Stop();
        System.Console.WriteLine("[CONNECTIOn OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
    }

    [Fact]
    public async Task MsSqlRestorer_WithMSSQLContainerBuilder_RestoresDbAfterPreviousTest()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MsSqlContainerFromSpecificBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MsSqlContainerFromSpecificBuilder.State);

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new SqlConnection(Setup.MsSqlContainerFromSpecificBuilderConnStr);
        await connection.OpenAsync();
        stopwatch.Stop();
        System.Console.WriteLine("[CONNECTIOn OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
    }

    [Fact]
    public async Task MsSqlRestorer_WithGenericContainerBuilder_RestoresDbAfterPreviousTest()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MsSqlContainerFromGenericBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MsSqlContainerFromGenericBuilder.State);

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new SqlConnection(Setup.MsSqlContainerFromGenericBuilderConnStr);
        await connection.OpenAsync();
        stopwatch.Stop();
        System.Console.WriteLine("[CONNECTIOn OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
    }
}