using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.Common;

public class DbSetupStrategy : IDbStrategy
{
    private readonly IDbSeeder _seeder;
    private readonly IDbRestorer _restorer;

    public DbSetupStrategy(IDbSeeder seeder, IDbRestorer restorer)
    {
        _seeder = seeder ?? throw new ArgumentNullException(nameof(seeder));
        _restorer = restorer ?? throw new ArgumentNullException(nameof(restorer));
    }

    public async Task InitializeGlobalAsync(
        DbSetup dbSetup,
        IContainer container,
        string containerConnectionString,
        CancellationToken cancellationToken = default)
    {
            await _seeder.SeedAsync(
                dbSetup,
                container,
                dbSetup.BuildConnectionString(containerConnectionString),
                cancellationToken);

            await _restorer.SnapshotAsync(cancellationToken);
    }

    public async Task ResetAsync(
        IContainer container,
        string containerConnectionString,
        CancellationToken cancellationToken = default)
    {
        await _restorer.RestoreAsync(cancellationToken);
    }
}