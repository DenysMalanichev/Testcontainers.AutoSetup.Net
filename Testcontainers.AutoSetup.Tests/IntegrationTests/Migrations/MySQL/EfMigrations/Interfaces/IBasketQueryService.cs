namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Interfaces;

/// <summary>
/// Specific query used to fetch count without running in memory
/// </summary>
public interface IMySQLBasketQueryService
{
    Task<int> CountTotalBasketItems(string username);
}

