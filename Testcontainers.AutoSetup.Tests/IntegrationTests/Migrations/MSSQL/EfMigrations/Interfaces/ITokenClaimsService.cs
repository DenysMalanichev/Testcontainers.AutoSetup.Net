namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Interfaces;

public interface IMSSQLTokenClaimsService
{
    Task<string> GetTokenAsync(string userName);
}
