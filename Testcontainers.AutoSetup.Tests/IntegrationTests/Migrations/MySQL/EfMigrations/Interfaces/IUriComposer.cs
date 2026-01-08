namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Interfaces;

public interface IMySQLUriComposer
{
    string ComposePicUri(string uriTemplate);
}
