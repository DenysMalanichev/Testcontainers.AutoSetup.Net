namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Interfaces;

/// <summary>
/// Specific query used to fetch count without running in memory
/// </summary>
public interface IMSSQLBasketQueryService
{
    Task<int> CountTotalBasketItems(string username);
}

