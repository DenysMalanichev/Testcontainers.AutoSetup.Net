using System.IO.Abstractions;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.Helpers;

namespace Testcontainers.AutoSetup.Core.Common;

public class DbSetupStrategy<TSeeder, TRestorer> : IDbStrategy
    where TSeeder : DbSeeder
    where TRestorer : DbRestorer
{
    private readonly TSeeder _seeder;
    private readonly TRestorer _restorer;

    private readonly IContainer _container;
    private readonly DbSetup _dbSetup;
    private readonly bool _tryInitialRestoreFromSnapshot = true;
    private readonly ILogger _logger;

    public DbSetupStrategy(
        DbSetup dbSetup,
        IContainer container,
        IDbConnectionFactory connectionFactory,
        bool tryInitialRestoreFromSnapshot = true,
        IFileSystem? fileSystem = null,
        ILogger? logger = null)
    {
        var resolver = new DependencyResolver();

        _logger = logger ?? NullLogger.Instance;
        resolver.Register(_logger);
        resolver.Register(fileSystem ?? new FileSystem());

        _container = container ?? throw new ArgumentNullException(nameof(container));
        resolver.Register(_container);

        _dbSetup = dbSetup ?? throw new ArgumentNullException(nameof(dbSetup));
        resolver.Register(_dbSetup);

        ArgumentNullException.ThrowIfNull(connectionFactory);
        resolver.Register(connectionFactory);

        try
        {
            _seeder = resolver.CreateInstance<TSeeder>();
        }
        catch(Exception ex)
        {
            throw new ArgumentException($"Failed to instantiate a seeder of type {typeof(TSeeder)}", ex);
        }
        
        try
        {
            _restorer = resolver.CreateInstance<TRestorer>();
        }
        catch(Exception ex)
        {
            throw new ArgumentException($"Failed to instantiate a restorer of type {typeof(TRestorer)}", ex);   
        }
        
        _tryInitialRestoreFromSnapshot = tryInitialRestoreFromSnapshot;
    }

    public DbSetupStrategy(
        DbSetup dbSetup,
        IContainer container,
        bool tryInitialRestoreFromSnapshot = true,
        IFileSystem? fileSystem = null,
        ILogger? logger = null)
    {
        // TODO do not pass all dependencies as arguments.
        // Create a Register dependency method within a strategy and register only required arguments.
        // Alternatively, make arguments params(?)
        var resolver = new DependencyResolver();

        _logger = logger ?? NullLogger.Instance; // TODO shouldn't it be Testcontainers default logger????
        resolver.Register(_logger);
        resolver.Register(fileSystem ?? new FileSystem());

        _container = container ?? throw new ArgumentNullException(nameof(container));
        resolver.Register(_container);

        _dbSetup = dbSetup ?? throw new ArgumentNullException(nameof(dbSetup));
        resolver.Register(_dbSetup);

        try
        {
            _seeder = resolver.CreateInstance<TSeeder>();
        }
        catch(Exception ex)
        {
            throw new ArgumentException($"Failed to instantiate a seeder of type {typeof(TSeeder)}", ex);
        }
        
        try
        {
            _restorer = resolver.CreateInstance<TRestorer>();
        }
        catch(Exception ex)
        {
            throw new ArgumentException($"Failed to instantiate a restorer of type {typeof(TRestorer)}", ex);   
        }
        
        _tryInitialRestoreFromSnapshot = tryInitialRestoreFromSnapshot;
    }

    /// <inheritdoc/>
    public async Task InitializeGlobalAsync(CancellationToken cancellationToken = default)
    {
        if (_tryInitialRestoreFromSnapshot 
            && await _restorer.IsSnapshotUpToDateAsync(cancellationToken))
        {
            await _restorer.RestoreAsync(cancellationToken);
            return;
        }
        await _seeder.SeedAsync(
            _dbSetup,
            _container,
            cancellationToken);

        await _restorer.SnapshotAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _restorer.RestoreAsync(cancellationToken);
    }
}