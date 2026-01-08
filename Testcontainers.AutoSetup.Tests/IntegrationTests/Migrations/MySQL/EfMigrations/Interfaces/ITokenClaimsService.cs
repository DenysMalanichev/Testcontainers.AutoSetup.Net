namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Interfaces;

public interface IMySQLTokenClaimsService
{
    Task<string> GetTokenAsync(string userName);
}
