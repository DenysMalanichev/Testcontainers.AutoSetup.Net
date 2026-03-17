using DotNet.Testcontainers.Containers;

namespace Testcontainers.AutoSetup.Core.Abstractions;

public interface IResetStrategy
{
    /// <summary>
    /// Resets an instance to the initial state.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
