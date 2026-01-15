using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.Core.Common.Helpers;

namespace Testcontainers.AutoSetup.Core.Abstractions.Entities;

public abstract partial record DbSetup
{   
    public virtual string DbName { get; init; }

    /// <summary>
    /// Connection string used to connect to the container's master database instance
    /// </summary>
    public virtual string ContainerConnectionString { get; init; }
    public virtual string MigrationsPath { get; init; }

    public virtual DbType DbType { get; init; } = DbType.Other;
    public virtual bool RestoreFromDump { get; init; } = false;
    public virtual string RestorationStateFilesDirectory { get; init; }

    protected virtual IFileSystem FileSystem { get; init; } = new FileSystem();

    public DbSetup(
        string dbName,
        string containerConnectionString, 
        string migrationsPath,
        DbType dbType = DbType.Other,
        bool restoreFromDump = false,
        string? restorationStateFilesDirectory = null,
        IFileSystem? fileSystem = null)
    {
        DbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
        ContainerConnectionString = containerConnectionString ?? throw new ArgumentNullException(nameof(containerConnectionString));
        MigrationsPath = migrationsPath ?? throw new ArgumentNullException(nameof(migrationsPath));
        DbType = dbType;
        RestoreFromDump = restoreFromDump;
        RestorationStateFilesDirectory = restorationStateFilesDirectory ?? RestorationFilePathResolver.Resolve(DbType)!;
        FileSystem = fileSystem ?? new FileSystem();
    }

    /// <summary>
    /// Builds and returns a connection string to desiered DB
    /// </summary>
    public virtual string BuildDbConnectionString()
    {
        var containerConnStr = ContainerConnectionString ?? throw new InvalidOperationException(
            "ContainerConnectionString must be provided to build the full connection string.");
        if(DbName is not null)
        {
            containerConnStr = DbNameRegex().Replace(containerConnStr, $"Database={DbName};"); 

            if(DbType == DbType.MySQL)
                containerConnStr = containerConnStr.Replace("Uid=mysql", "Uid=root");           
        }

        return containerConnStr;
    }
    
    [GeneratedRegex("Database=[^;]*;")]
    private static partial Regex DbNameRegex();
}