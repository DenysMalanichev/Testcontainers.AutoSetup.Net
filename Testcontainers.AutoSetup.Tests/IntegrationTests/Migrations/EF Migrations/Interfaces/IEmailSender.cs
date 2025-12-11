namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}
