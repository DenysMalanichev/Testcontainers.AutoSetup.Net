using DotNet.Testcontainers.Containers;

namespace Testcontainers.AutoSetup.Core.Abstractions;

public interface IDbResetStrategy
{
    /// <summary>
    /// Resets a DB to the initial state by applying an exisiting snapshot.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
