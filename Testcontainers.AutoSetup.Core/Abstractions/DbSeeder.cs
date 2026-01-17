using DotNet.Testcontainers;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;

namespace Testcontainers.AutoSetup.Core.Abstractions;

public abstract class DbSeeder
{
    protected readonly ILogger _logger;

    public DbSeeder()
    {
        _logger = ConsoleLogger.Instance;
    }

    public DbSeeder(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Implements migrations to set up a DB and seed initial data into it.
    /// </summary>
    /// <param name="dbSetup"><see cref="DbSetup"/> with information about the DB being set up</param>
    /// <param name="container">An <see cref="IContainer"/> where a DB is initializing</param>
    /// <param name="cancellationToken"></param>
    public abstract Task SeedAsync(DbSetup dbSetup, IContainer container, CancellationToken cancellationToken);
}