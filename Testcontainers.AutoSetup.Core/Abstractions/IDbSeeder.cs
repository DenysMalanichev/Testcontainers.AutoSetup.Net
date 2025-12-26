using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.Abstractions;

public interface IDbSeeder
{
    /// <summary>
    /// Implements migrations to set up a DB and seed initial data into it.
    /// </summary>
    /// <param name="dbSetup"><see cref="DbSetup"/> with information about the DB being set up</param>
    /// <param name="container">An <see cref="IContainer"/> where a DB is initializing</param>
    /// <param name="containerConnectionString">A <see cref="string"/> to connect to the container</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SeedAsync(
        DbSetup dbSetup,
        IContainer container,
        string containerConnectionString,
        CancellationToken cancellationToken);
}