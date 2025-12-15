using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Tests.TestCollections;
using Microsoft.Data.SqlClient;
using System.Data.SqlTypes;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.DbRestoration;

[Trait("Category", "Integration")]
[Collection(nameof(ParallelTests))]
public class MsSqlRestorationTests(ContainersFixture fixture) : IntegrationTestsBase(fixture)
{
    [Fact]
    public async Task EfSeeder_WithMSSQLContainerBuilder_MigratesDatabase()
    {
        // Arrange & Act stages (containers setup nad seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MsSqlContainerFromSpecificBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MsSqlContainerFromSpecificBuilder.State);

        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", Setup.MsSqlContainerFromSpecificBuilderConnection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
    }

    [Fact]
    public async Task EfSeeder_WithGenericContainerBuilder_MigratesDatabase()
    {
        // Arrange & Act stages (containers setup nad seeding) of the test are done within the GlobalTestSetup
        // Assert
        Assert.NotNull(Setup.MsSqlContainerFromGenericBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MsSqlContainerFromGenericBuilder.State);

        using var historyCmd = new SqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", Setup.MsSqlContainerFromGenericBuilderConnection);
        var migrationCount = (int)(await historyCmd.ExecuteScalarAsync() ?? throw new SqlNullValueException());

        Assert.True(migrationCount > 0, "No migrations were found in the history table.");
    }
}