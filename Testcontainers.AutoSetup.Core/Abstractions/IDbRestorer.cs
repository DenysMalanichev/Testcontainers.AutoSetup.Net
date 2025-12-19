namespace Testcontainers.AutoSetup.Core.Abstractions;

public interface IDbRestorer
{
    /// <summary>
    /// Returns the <see cref="string?"/> path to the current DB snapshot or null, if no snapshots exist
    /// </summary>
    string? RestorationStateFilesPath { get; }

    /// <summary>
    /// Creates a snapshot of the DB from which it will be restored
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task SnapshotAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Restores a DB from the created snapshot
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task RestoreAsync(CancellationToken cancellationToken = default);
}