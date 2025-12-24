using DotNet.Testcontainers.Containers;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.Common;

public class DbSetupStrategy<TSeeder, TRestorer> : IDbStrategy
    where TSeeder : IDbSeeder, new()
    where TRestorer : DbRestorer
{
    private readonly TSeeder _seeder;
    private readonly TRestorer _restorer;

    private readonly IContainer _container;
    private readonly DbSetup _dbSetup;
    private readonly bool _tryInitialRestoreFromSnapshot = true;
    private readonly string _containerConnectionString;

    public DbSetupStrategy(
        DbSetup dbSetup,
        IContainer container,
        string containerConnectionString,
        bool tryInitialRestoreFromSnapshot = true,
        string? restorationStateFilesPath = null!)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _dbSetup = dbSetup ?? throw new ArgumentNullException(nameof(dbSetup));
        _containerConnectionString = containerConnectionString
            ?? throw new ArgumentNullException(nameof(containerConnectionString));

        try
        {
            _seeder = new TSeeder();
        }
        catch(Exception ex)
        {
            throw new ArgumentException($"Failed to instantiate a seeder of type {typeof(TSeeder)}", ex);
        }
        
        try
        {
            _restorer = (TRestorer)Activator.CreateInstance(
            typeof(TRestorer),
            [dbSetup, container, containerConnectionString, restorationStateFilesPath])!;
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
        var connectionString = _dbSetup.BuildConnectionString(_containerConnectionString);
        await _seeder.SeedAsync(
            _dbSetup,
            _container,
            connectionString,
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
        // test -z "..."        -> Returns Exit Code 0 (Success) if the string is EMPTY (no new files found)
        //                      -> Returns Exit Code 1 (Fail) if the string is NOT EMPTY (new files found)
        var migrationsLMD = await _dbSetup.GetMigrationsLastModificationDateAsync(cancellationToken);
        var cmd = $"ls {_restorer.RestorationStateFilesDirectory}/*snapshot_* > /dev/null 2>&1 && " + 
          $"test -z \"$(find {_restorer.RestorationStateFilesDirectory} " +
           "-maxdepth 1 -name '*_snapshot_*' " +
          $"-newermt '{migrationsLMD:yyyy-MM-dd HH:mm:ss}' -print -quit)\"";
        var result = await _container.ExecAsync( 
        [
            "/bin/bash", 
            "-c", 
            cmd
        ], cancellationToken);

        if (result.ExitCode == 0 && result.Stderr.IsNullOrEmpty())
        {
            Console.WriteLine("Snapshot is up to date (No newer migrations found).");
            return true; 
        }
        else if (result.ExitCode == 1 || result.ExitCode == 2)
        {
            Console.WriteLine("No up-to-date snapshot exists, recreation required.");
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
            return true;
        }
        else if (result.ExitCode == 1)
        {
            Console.WriteLine($"[WARNING] No mount found at {_restorer.RestorationStateFilesDirectory}. Skipping initial restoration.");
            return false;
        }
        
        // Unexpected errors (e.g., findmnt not installed, permission denied)
        throw new ExecFailedException(result);
    }
}