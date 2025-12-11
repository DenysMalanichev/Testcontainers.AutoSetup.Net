namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Interfaces;

public interface ITokenClaimsService
{
    Task<string> GetTokenAsync(string userName);
}
