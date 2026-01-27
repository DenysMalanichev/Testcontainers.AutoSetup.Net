using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Abstractions.Sql;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.EntityFramework.Abstractions;

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

        await ApplyEFMigrationsAsync(dbSetup, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies Entity Framework migrations to the target database.
    /// </summary>
    /// <param name="dbSetup"></param>
    /// <param name="cancellationToken"></param>
    private async Task ApplyEFMigrationsAsync(
        DbSetup dbSetup,
        CancellationToken cancellationToken = default)
    {
        var finalConnectionString = dbSetup.BuildDbConnectionString();

        await using var dbContext = ((IEfContextFactory)dbSetup).ContextFactory(finalConnectionString);

        await ExecuteMigrateAsync(dbSetup, dbContext, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully applied EF migrations to database '{Database}'", dbSetup.DbName);
    }

    /// <summary>
    /// Executes the migration using the provided DbContext.
    /// Isolates the migration logic for easier testing.
    /// </summary>
    protected virtual async Task ExecuteMigrateAsync(DbSetup dbSetup, DbContext dbContext, CancellationToken cancellationToken)
    {
        if(dbSetup.DbType is DbType.MongoDB)
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            return;   
        }
           
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
