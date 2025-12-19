using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Common.Entities;

public abstract record DbSetup
{   
    public required string DbName { get; init; }
    public required string MigrationsPath { get; init; }

    public DbType DbType { get; init; } = DbType.Other;
    public bool RestoreFromDump { get; init; } = false;

    /// <summary>
    /// Builds and returns a connection string to desiered DB
    /// </summary>
    /// <param name="containerConnStr">Connection string to DB's container</param>
    public abstract string BuildConnectionString(string containerConnStr);

    /// <summary>
    /// Returns a <see cref="DateTime"/> identifying the last time migrations files changed
    /// </summary>
    /// <param name="cancellationToken"></param>
    public abstract Task<DateTime> GetMigrationsLastModificationDateAsync(CancellationToken cancellationToken = default);
}