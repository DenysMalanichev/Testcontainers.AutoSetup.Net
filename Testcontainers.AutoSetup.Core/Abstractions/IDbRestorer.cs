

using DotNet.Testcontainers.Containers;

namespace Testcontainers.AutoSetup.Core.Abstractions;

public interface IDbRestorer
{
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