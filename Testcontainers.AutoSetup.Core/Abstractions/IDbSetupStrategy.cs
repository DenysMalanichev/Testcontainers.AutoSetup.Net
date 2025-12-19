
using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.Abstractions;

public interface IDbSetupStrategy
{
    /// <summary>
    /// Initializes a database <see cref="IContainer"/> with migrating and seeding data,
    /// or using an existing snapshot if it is up to date and mount exists.
    /// </summary>
    /// <param name="dbSetup"><see cref="DbSetup"/> with information about the DB being set up</param>
    /// <param name="container">An <see cref="IContainer"/> where a DB is initializing</param>
    /// <param name="containerConnectionString">A <see cref="string"/> to connect to the container</param>
    /// <param name="cancellationToken"></param>
    Task InitializeGlobalAsync(
        DbSetup dbSetup,
        IContainer container, 
        string containerConnectionString,
        CancellationToken cancellationToken = default);
}
