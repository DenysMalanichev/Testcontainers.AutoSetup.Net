using System.IO.Abstractions;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;

namespace Testcontainers.AutoSetup.Core.Abstractions;

public abstract class DbSeeder
{
    protected readonly IDbConnectionFactory _dbConnectionFactory;
    protected readonly IFileSystem _fileSystem;
    protected readonly ILogger _logger;

    public DbSeeder(IDbConnectionFactory dbConnectionFactory, IFileSystem fileSystem, ILogger? logger = null)
    {
        _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Implements migrations to set up a DB and seed initial data into it.
    /// </summary>
    /// <param name="dbSetup"><see cref="DbSetup"/> with information about the DB being set up</param>
    /// <param name="container">An <see cref="IContainer"/> where a DB is initializing</param>
    /// <param name="cancellationToken"></param>
    public abstract Task SeedAsync(DbSetup dbSetup, IContainer container, CancellationToken cancellationToken);
}