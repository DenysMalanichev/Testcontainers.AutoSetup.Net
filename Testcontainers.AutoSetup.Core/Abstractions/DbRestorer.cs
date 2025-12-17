using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.Abstractions;

/// <summary>
/// An abstract class intended to create a DB snapshot
/// and restore this DB (its structure, seeded data) using created snapshot before each test. 
/// </summary>
public abstract class DbRestorer : IDbRestorer
{
    protected readonly string _restorationStateFilesPath;
    protected readonly string _containerConnectionString;
    protected readonly DbSetup _dbSetup;
    protected readonly IContainer _container;

    /// <summary>
    /// Creates an instance of <see cref="DbRestorer"/>
    /// </summary>
    /// <param name="container">Container <see cref="IContainer"/> where the DB is running</param>
    /// <param name="dbSetup"><see cref="DbSetup"/> with inforamtion about the DB that is intended to be restored</param>
    /// <param name="containerConnectionString"><see cref="string"/> connection string to connect to the database</param>    
    /// <param name="restorationStateFilesPath">A path where the DB snapshot is stored</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbRestorer(
        DbSetup dbSetup, 
        IContainer container,
        string containerConnectionString,
        string restorationStateFilesPath)
    {
        _dbSetup = dbSetup ?? throw new ArgumentNullException(nameof(dbSetup));   
        _container = container ?? throw new ArgumentNullException(nameof(container));

        if(string.IsNullOrEmpty(restorationStateFilesPath))
        {
            throw new ArgumentNullException(nameof(restorationStateFilesPath));   
        }
        _restorationStateFilesPath = restorationStateFilesPath;

        if(string.IsNullOrEmpty(containerConnectionString))
        {
            throw new ArgumentNullException(nameof(containerConnectionString));   
        }
        _containerConnectionString = containerConnectionString;        
    }

    /// <inheritdoc/>
    public abstract Task RestoreAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task SnapshotAsync(CancellationToken cancellationToken = default);
}