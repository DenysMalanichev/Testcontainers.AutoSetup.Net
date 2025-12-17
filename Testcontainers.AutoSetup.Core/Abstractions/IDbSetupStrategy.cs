
using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.Abstractions;

public interface IDbSetupStrategy
{
    Task InitializeGlobalAsync(
        DbSetup dbSetup,
        IContainer container, 
        string containerConnectionString,
        CancellationToken cancellationToken = default);
}
