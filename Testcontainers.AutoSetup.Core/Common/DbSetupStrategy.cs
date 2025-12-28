using System.IO.Abstractions;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.SqlDbHelpers;

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
        bool tryInitialRestoreFromSnapshot = true,
        string? restorationStateFilesDirectory = null!,
        ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _dbSetup = dbSetup ?? throw new ArgumentNullException(nameof(dbSetup));

        // TODO investigate better instanciation approach, without passing redundant arguments
        try
        {
            _seeder = (TSeeder)Activator.CreateInstance(
                typeof(TSeeder),
                [new SqlDbConnectionFactory(), new FileSystem(), logger])!;
        }
        catch(Exception ex)
        {
            throw new ArgumentException($"Failed to instantiate a seeder of type {typeof(TSeeder)}", ex);
        }
        
        try
        {
            _restorer = (TRestorer)Activator.CreateInstance(
                typeof(TRestorer),
                [dbSetup, container, dbSetup.ContainerConnectionString, restorationStateFilesDirectory, logger])!;
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
            && await IsMountExistsAsync(cancellationToken)
            && await IsSnapshotValidAsync(cancellationToken))
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

    /// <summary>
    /// Checks if current snapshot is up to date with migrations
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ExecFailedException"></exception>
    private async Task<bool> IsSnapshotValidAsync(CancellationToken cancellationToken)
    {
        // COMMAND EXPLANATION:
        // Checks if at least one snapshot exists in the directory
        // "ls ... > /dev/null 2>&1" silence the output, returns true (0) if files exist, false (2) if not - masked.
        // find ... -newermt ...  -> Looks for files in the dir newer than the snapshot LMD
        // -print -quit         -> Stop searching as soon as we find ONE new file
        // test -n "..."        -> Returns Exit Code 0 (Success) if the string is NOT EMPTY (new files found)
        //                      -> Returns Exit Code 1 (Fail) if the string is EMPTY (no new files found)
        var migrationsLMD = await _dbSetup.GetMigrationsLastModificationDateAsync(cancellationToken);
        var cmd = $"ls {_restorer.RestorationStateFilesDirectory}/{_dbSetup.DbName}_snapshot_* > /dev/null 2>&1 && " + 
           $"test -n \"$(find {_restorer.RestorationStateFilesDirectory} " +
           $"-maxdepth 1 -name '{_dbSetup.DbName}_snapshot_*' " +
           $"-newermt '{migrationsLMD:yyyy-MM-dd HH:mm:ss}' -print -quit)\"";
        var result = await _container.ExecAsync( 
        [
            "/bin/bash", 
            "-c", 
            cmd
        ], cancellationToken);

        if (result.ExitCode == 0 && result.Stderr.IsNullOrEmpty())
        {
            _logger.LogInformation("Snapshot is up to date (No newer migrations found).");
            return true; 
        }
        else if (result.ExitCode == 1 || result.ExitCode == 2)
        {
            _logger.LogWarning("No up-to-date snapshot exists, recreation required.");
            return false;
        }

        throw new ExecFailedException(result);
    }

    /// <summary>
    /// Ensures that the restoration directory exists 
    /// </summary>
    /// <exception cref="ExecFailedException">
    /// Thrown when failed to run a command to validate the mount
    /// </exception>
    private async Task<bool> IsMountExistsAsync(CancellationToken cancellationToken)
    {        
        var result = await _container.ExecAsync(
        [
            "/bin/bash", 
            "-c", 
            $"findmnt {_restorer.RestorationStateFilesDirectory}",
        ], cancellationToken);

        if (result.ExitCode == 0)
        {
            _logger!.LogInformation($"Required mount found.");
            return true;
        }
        else if (result.ExitCode == 1)
        {
            _logger.LogWarning($"No mount found at {_restorer.RestorationStateFilesDirectory}. Skipping initial restoration.");
            return false;
        }
        
        // Unexpected errors (e.g., findmnt not installed, permission denied)
        throw new ExecFailedException(result);
    }
}