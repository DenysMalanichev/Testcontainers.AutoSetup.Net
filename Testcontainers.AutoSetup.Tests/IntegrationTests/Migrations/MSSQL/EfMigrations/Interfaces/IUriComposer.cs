namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Interfaces;

public interface IMSSQLUriComposer
{
    string ComposePicUri(string uriTemplate);
}
