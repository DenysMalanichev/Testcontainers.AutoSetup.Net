namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MySQL.EfMigrations.Interfaces;

public interface IMySQLEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}
