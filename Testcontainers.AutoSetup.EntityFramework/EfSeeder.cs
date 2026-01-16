using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Abstractions.Sql;
using Testcontainers.AutoSetup.EntityFramework.Entities;

namespace Testcontainers.AutoSetup.EntityFramework;

public class EfSeeder : SqlDbSeeder
{
    public EfSeeder(ILogger logger)
        : base(logger)
    { }

    /// <inheridoc />
    public override async Task SeedAsync(
        DbSetup dbSetup,
        IContainer? container = null,
        CancellationToken cancellationToken = default)
    {        
        _logger.LogInformation("Applying EF migrations to database '{Database}'", dbSetup.DbName);

        await ApplyEFMigrationsAsync((EfDbSetup)dbSetup, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies Entity Framework migrations to the target database.
    /// </summary>
    /// <param name="dbSetup"></param>
    /// <param name="cancellationToken"></param>
    private async Task ApplyEFMigrationsAsync(
        EfDbSetup dbSetup,
        CancellationToken cancellationToken = default)
    {
        var finalConnectionString = dbSetup.BuildDbConnectionString();

        await using var dbContext = dbSetup.ContextFactory(finalConnectionString);

        await ExecuteMigrateAsync(dbContext, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully applied EF migrations to database '{Database}'", dbSetup.DbName);
    }

    /// <summary>
    /// Executes the migration using the provided DbContext.
    /// Isolates the migration logic for easier testing.
    /// </summary>
    protected virtual Task ExecuteMigrateAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.Database.MigrateAsync(cancellationToken);
    }
}
