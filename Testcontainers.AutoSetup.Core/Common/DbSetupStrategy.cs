using DotNet.Testcontainers.Containers;
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

    public DbSetupStrategy(
        DbSetup dbSetup,
        IContainer container,
        string containerConnectionString,
        string? restorationStateFilesPath = null!)
    {
        _seeder = new TSeeder() ?? throw new ArgumentException($"Failed to instantiate a seeder of type {typeof(TSeeder)}");
        _restorer = (TRestorer)Activator.CreateInstance(
            typeof(TRestorer),
            [dbSetup, container, containerConnectionString, restorationStateFilesPath])!
                ?? throw new ArgumentException($"Failed to instantiate a restorer of type {typeof(TRestorer)}");
        
        _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public async Task InitializeGlobalAsync(
        DbSetup dbSetup,
        IContainer container,
        string containerConnectionString,
        CancellationToken cancellationToken = default)
    {
        if (await IsMountExistsAsync(cancellationToken) && await IsSnapshotValidAsync(cancellationToken))
        {
            await _restorer.RestoreAsync(cancellationToken);
            return;
        }
        await _seeder.SeedAsync(
            dbSetup,
            container,
            dbSetup.BuildConnectionString(containerConnectionString),
            cancellationToken);

        await _restorer.SnapshotAsync(cancellationToken);
    }

    public async Task ResetAsync(
        IContainer container,
        string containerConnectionString,
        CancellationToken cancellationToken = default)
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
        // find ... -newer ...  -> Looks for files in the dir newer than the snapshot file
        // -print -quit         -> Stop searching as soon as we find ONE new file (Performance)
        // test -z "..."        -> Returns Exit Code 0 (Success) if the string is EMPTY (no new files found)
        //                      -> Returns Exit Code 1 (Fail) if the string is NOT EMPTY (new files found)
        
        var cmd = $"test -z \"$(find {_restorer.RestorationStateFilesDirectory} -newer {_restorer.RestorationStateFilesPath} -print -quit)\"";

        var result = await _container.ExecAsync( 
        [
            "/bin/bash", 
            "-c", 
            cmd
        ], cancellationToken);

        if (result.ExitCode == 0)
        {
            Console.WriteLine("Snapshot is up to date (No newer migrations found).");
            return true; 
        }
        else if (result.ExitCode == 1)
        {
            Console.WriteLine("Migrations have changed. Snapshot recreation required.");
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
            $"findmnt -q {_restorer.RestorationStateFilesPath}",
        ], cancellationToken);

        if (result.ExitCode == 0)
        {
            return true;
        }
        else if (result.ExitCode == 1)
        {
            Console.WriteLine($"[WARNING] No mount found at {_restorer.RestorationStateFilesPath}. Skipping initial restoration.");
            return false;
        }
        
        // Unexpected errors (e.g., findmnt not installed, permission denied)
        throw new ExecFailedException(result);
    }
}