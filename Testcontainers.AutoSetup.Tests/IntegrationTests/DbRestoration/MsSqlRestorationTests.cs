using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using Testcontainers.AutoSetup.Core.Attributes;
using Testcontainers.AutoSetup.Tests.IntegrationTests.TestCollections;
using Xunit.Abstractions;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.DbRestoration;

[DbReset]
[Trait("Category", "Integration")]
[Collection(nameof(ParallelIntegrationTestsCollection))]
public class MsSqlRestorationTests : IntegrationTestsBase
{
    private readonly ITestOutputHelper _output;

    public MsSqlRestorationTests(ITestOutputHelper output, ContainersFixture fixture)
        : base(fixture)
    {
        _output = output;
    }

    [Fact]
    public async Task EfSeeder_WithMSSQLContainerBuilder_MigratesDatabase()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MsSqlContainerFromSpecificBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MsSqlContainerFromSpecificBuilder.State);

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new SqlConnection(Setup.MsSqlContainer_SpecificBuilder_EfDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();
        stopwatch.Stop();
        _output.WriteLine("[CONNECTION OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
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
        await using var connection = new SqlConnection(Setup.MsSqlContainer_GenericBuilder_EfDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();
        stopwatch.Stop();
        _output.WriteLine("[CONNECTION OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
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
        var connStr = Setup.MsSqlContainer_SpecificBuilder_EfDbSetup!.BuildDbConnectionString();
        await using var connection = new SqlConnection(connStr);
        await connection.OpenAsync();
        stopwatch.Stop();
        _output.WriteLine("[CONNECTION OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
        var alterQueryString = $"INSERT INTO [CatalogTest].[dbo].[Baskets] ([BuyerId]) VALUES ('{Guid.NewGuid()}');";
        await using var alterQuery = new SqlCommand(alterQueryString, connection);
        await alterQuery.ExecuteNonQueryAsync();

        await connection.DisposeAsync();
        SqlConnection.ClearAllPools();
        await Setup.ResetEnvironmentAsync(this.GetType());

        using var newConnection = new SqlConnection(connStr);
        await newConnection.OpenAsync();

        var chechQueryString = "SELECT COUNT(1) FROM [CatalogTest].[dbo].[Baskets];";
        await using var checkQuery = new SqlCommand(chechQueryString, newConnection);
        var checkResult = await checkQuery.ExecuteScalarAsync();
        Assert.Equal(0, (int)checkResult!);
    }

    [Fact]
    public async Task MsSqlRestorer_WithGenericContainerBuilder_RestoresDbAfterPreviousTest()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MsSqlContainerFromGenericBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MsSqlContainerFromGenericBuilder.State);

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new SqlConnection(Setup.MsSqlContainer_GenericBuilder_EfDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();
        stopwatch.Stop();
        _output.WriteLine("[CONNECTION OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
    }

    [Fact]
    public async Task MsSqlRestorer_WithSpecificContainerBuilder_RestoresDBFromRawSqlFiles()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MsSqlContainerFromSpecificBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MsSqlContainerFromSpecificBuilder.State);

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new SqlConnection(Setup.MsSqlContainer_SpecificBuilder_RawSqlDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();
        stopwatch.Stop();
        _output.WriteLine("[CONNECTION OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM Catalog", connection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
    }

    [Fact]
    public async Task MsSqlRestorer_WithGenericContainerBuilder_RestoresDBFromRawSqlFiles()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MsSqlContainerFromGenericBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MsSqlContainerFromGenericBuilder.State);

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new SqlConnection(Setup.MsSqlContainer_GenericBuilder_RawSqlDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();
        stopwatch.Stop();
        _output.WriteLine("[CONNECTION OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM Catalog", connection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
    }
}