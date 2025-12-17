using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.Abstractions;

public interface IDbSeeder
{
    Task SeedAsync(
        DbSetup dbSetup,
        IContainer container,
        string containerConnectionString,
        CancellationToken cancellationToken);
}