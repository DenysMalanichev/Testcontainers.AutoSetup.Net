
using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.Abstractions;

public interface IDbSetupStrategy
{
    /// <summary>
    /// Initializes a database <see cref="IContainer"/> with migrating and seeding data,
    /// or using an existing snapshot if it is up to date and mount exists.
    /// </summary>
    /// <param name="cancellationToken"></param>
    Task InitializeGlobalAsync(CancellationToken cancellationToken = default);
}
