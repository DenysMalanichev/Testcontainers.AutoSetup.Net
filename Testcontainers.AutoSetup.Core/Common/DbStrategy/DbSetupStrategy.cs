using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;

namespace Testcontainers.AutoSetup.Core.Common;

public class DbSetupStrategy : IDbStrategy
{
    private readonly DbSeeder _seeder;
    private readonly DbRestorer _restorer;

    private readonly IContainer _container;
    private readonly DbSetup _dbSetup;
    private readonly bool _tryInitialRestoreFromSnapshot = true;

    public DbSetupStrategy(
        DbSetup dbSetup,
        DbSeeder seeder,
        DbRestorer restorer,
        IContainer container,
        bool tryInitialRestoreFromSnapshot = true)
    {        
        _tryInitialRestoreFromSnapshot = tryInitialRestoreFromSnapshot;
        _seeder = seeder ?? throw new ArgumentNullException(nameof(seeder));
        _dbSetup = dbSetup ?? throw new ArgumentNullException(nameof(dbSetup));
        _restorer = restorer ?? throw new ArgumentNullException(nameof(restorer));
        _container = container ?? throw new ArgumentNullException(nameof(container));        
    }

    /// <inheritdoc/>
    public async Task InitializeGlobalAsync(CancellationToken cancellationToken = default)
    {
        if (_tryInitialRestoreFromSnapshot 
            && await _restorer.IsSnapshotUpToDateAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            await _restorer.RestoreAsync(cancellationToken).ConfigureAwait(false);
            return;
        }
        await _seeder.SeedAsync(
            _dbSetup,
            _container,
            cancellationToken).ConfigureAwait(false);

        await _restorer.SnapshotAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _restorer.RestoreAsync(cancellationToken).ConfigureAwait(false);
    }
}