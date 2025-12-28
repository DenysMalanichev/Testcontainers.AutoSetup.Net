using System.IO.Abstractions;
using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Abstractions.Entities;

public abstract record DbSetup
{   
    public virtual required string DbName { get; init; }

    /// <summary>
    /// Connection string used to connect to the container's master database instance
    /// </summary>
    public virtual required string ContainerConnectionString { get; init; }
    public virtual required string MigrationsPath { get; init; }

    public virtual DbType DbType { get; init; } = DbType.Other;
    public virtual bool RestoreFromDump { get; init; } = false;

    protected virtual IFileSystem _fileSystem { get; init; } = new FileSystem();

    /// <summary>
    /// Builds and returns a connection string to desiered DB
    /// </summary>
    public virtual string BuildDbConnectionString()
    {
        var containerConnStr = ContainerConnectionString ?? throw new InvalidOperationException(
            "ContainerConnectionString must be provided to build the full connection string.");
        if(DbName is not null)
        {
            containerConnStr = containerConnStr.Replace("Database=master", $"Database={DbName}");            
        }

        return containerConnStr;
    }

    /// <summary>
    /// Returns a <see cref="DateTime"/> identifying the last time migrations files changed
    /// </summary>
    /// <param name="cancellationToken"></param>
    public abstract Task<DateTime> GetMigrationsLastModificationDateAsync(CancellationToken cancellationToken = default);
}