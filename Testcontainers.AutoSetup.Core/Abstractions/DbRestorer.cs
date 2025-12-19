using DotNet.Testcontainers.Containers;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.Abstractions;

/// <summary>
/// An abstract class intended to create a DB snapshot
/// and restore this DB (its structure, seeded data) using created snapshot before each test. 
/// </summary>
public abstract class DbRestorer : IDbRestorer
{
    internal readonly string RestorationStateFilesDirectory;
    protected string _restorationSnapshotName = null!;
    protected readonly string _containerConnectionString;
    protected readonly DbSetup _dbSetup;
    protected readonly IContainer _container;

    /// <inheridoc/>
    public string? RestorationStateFilesPath 
    {
        get 
        {
            if(_restorationSnapshotName.IsNullOrEmpty())
            {
                return null!;
            }
            
            return Path.Combine(RestorationStateFilesDirectory, _restorationSnapshotName).Replace("\\", "/");
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
    public DbRestorer(
        DbSetup dbSetup, 
        IContainer container,
        string containerConnectionString,
        string restorationStateFilesDirectory)
    {
        _dbSetup = dbSetup ?? throw new ArgumentNullException(nameof(dbSetup));   
        _container = container ?? throw new ArgumentNullException(nameof(container));

        if(string.IsNullOrEmpty(restorationStateFilesDirectory))
        {
            throw new ArgumentNullException(nameof(restorationStateFilesDirectory));   
        }
        RestorationStateFilesDirectory = restorationStateFilesDirectory;

        if(string.IsNullOrEmpty(containerConnectionString))
        {
            throw new ArgumentNullException(nameof(containerConnectionString));   
        }
        _containerConnectionString = containerConnectionString;        
    }

    /// <summary>
    /// Ensures that the restoration directory exists 
    /// </summary>
    /// <exception cref="ExecFailedException">
    /// Thrown when failed to create a restoration directory
    /// </exception>
    protected async Task EnsureRestorationDirectoryExistsAsync()
    {        
        var result = await _container.ExecAsync(["/bin/bash", "-c", $"mkdir -p {RestorationStateFilesDirectory}"]);
        if(result.ExitCode != 0 || !string.IsNullOrEmpty(result.Stderr))
        {
            throw new ExecFailedException(result);
        }
    }

    /// <inheritdoc/>
    public abstract Task RestoreAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task SnapshotAsync(CancellationToken cancellationToken = default);
}