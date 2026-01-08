namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MSSQL.EfMigrations.Interfaces;

public interface IMSSQLEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}
