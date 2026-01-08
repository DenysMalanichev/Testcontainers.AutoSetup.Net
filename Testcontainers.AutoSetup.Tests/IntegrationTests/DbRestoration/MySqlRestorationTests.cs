using System.Data.SqlTypes;
using System.Diagnostics;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
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
    public async Task EfSeeder_WithMySQLContainerBuilder_MigratesDatabase()
    {
        // Arrange & Act stages (containers setup and seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MySqlContainerFromSpecificBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MsSqlContainerFromSpecificBuilder.State);

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new MySqlConnection(Setup.MySqlContainer_SpecificBuilder_EfDbSetup!.BuildDbConnectionString());
        await connection.OpenAsync();
        stopwatch.Stop();
        Console.WriteLine("[CONNECTION OPENED IN TEST IN] " + stopwatch.ElapsedMilliseconds);
        using var historyCmd = new MySqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection);
        var migrationCount = (long)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task EfSeeder_WithGenericMySQLContainerBuilder_MigratesDatabase()
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
        using var historyCmd = new MySqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", connection);
        var migrationCount = (long)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
        await connection.DisposeAsync();
    }
}