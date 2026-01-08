using DotNet.Testcontainers.Containers;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;

namespace Testcontainers.AutoSetup.Core.Abstractions;

/// <summary>
/// An abstract class intended to create a DB snapshot
/// and restore this DB (its structure, seeded data) using created snapshot before each test. 
/// </summary>
public abstract class DbRestorer
{
    protected string _restorationSnapshotName = null!;
    protected readonly DbSetup _dbSetup;
    protected readonly IContainer _container;

    /// <summary>
    /// Returns the <see cref="string?"/> path to the current DB snapshot or null, if no snapshots exist
    /// </summary>
    public string? RestorationStateFilesPath 
    {
        get 
        {
            if(_restorationSnapshotName.IsNullOrEmpty())
            {
                return null;
            }
            
            return Path.Combine(_dbSetup.RestorationStateFilesDirectory, _restorationSnapshotName).Replace("\\", "/");
        }
    }

    /// <summary>
    /// Creates an instance of <see cref="DbRestorer"/>
    /// </summary>
    /// <param name="container">Container <see cref="IContainer"/> where the DB is running</param>
    /// <param name="dbSetup"><see cref="DbSetup"/> with inforamtion about the DB that is intended to be restored</param>
    /// <param name="containerConnectionString"><see cref="string"/> connection string to connect to the database</param>    
    /// <param name="restorationStateFilesDirectory">A path where the DB snapshot is stored</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbRestorer(DbSetup dbSetup, IContainer container)
    {
        _dbSetup = dbSetup ?? throw new ArgumentNullException(nameof(dbSetup));   
        _container = container ?? throw new ArgumentNullException(nameof(container));      
    }

    /// <summary>
    /// Ensures that the restoration directory exists 
    /// </summary>
    /// <exception cref="ExecFailedException">
    /// Thrown when failed to create a restoration directory
    /// </exception>
    protected async Task EnsureRestorationDirectoryExistsAsync()
    {        
        var result = await _container.ExecAsync(["/bin/bash", "-c", $"mkdir -p {_dbSetup.RestorationStateFilesDirectory}"]);
        if(result.ExitCode != 0 || !string.IsNullOrEmpty(result.Stderr))
        {
            throw new ExecFailedException(result);
        }
    }

    /// <summary>
    /// Restores a DB from the created snapshot
    /// </summary>
    /// <param name="cancellationToken"></param>
    public abstract Task RestoreAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a snapshot of the DB from which it will be restored
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public abstract Task SnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies whether a current snapshot is up to date with latest migrations
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract Task<bool> IsSnapshotUpToDateAsync(CancellationToken cancellationToken = default);
}