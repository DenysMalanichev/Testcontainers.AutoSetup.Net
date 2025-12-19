using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.EntityFramework.Entities;

namespace Testcontainers.AutoSetup.EntityFramework;

public class EfSeeder : IDbSeeder
{
    public async Task SeedAsync(
        DbSetup dbSetup,
        IContainer container,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        await ApplyEFMigrationsAsync((EfDbSetup)dbSetup, connectionString, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ApplyEFMigrationsAsync(
        EfDbSetup dbSetup,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var finalConnectionString = dbSetup.BuildConnectionString(connectionString);

        using var dbContext = dbSetup.ContextFactory(finalConnectionString);

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
