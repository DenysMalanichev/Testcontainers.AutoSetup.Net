using DotNet.Testcontainers.Containers;

namespace Testcontainers.AutoSetup.Core.Abstractions;

public interface IDbResetStrategy
{
    /// <summary>
    /// Resets a DB to the initial state by applying an exisiting snapshot.
    /// </summary>
    /// <param name="container">An <see cref="IContainer"/> where a DB is initializing</param>
    /// <param name="containerConnectionString">A <see cref="string"/> to connect to the container</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ResetAsync(
        IContainer container,
        string containerConnectionString,
        CancellationToken cancellationToken = default);
}
