using System.IO.Abstractions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.EntityFramework.Entities;

public record EfDbSetup : DbSetup
{  
    /// <summary>
    /// A <see cref="Func<>"/> taking a connection string and 
    /// returning an instance of <see cref="DbContext"/>
    /// </summary>
    public virtual Func<string, DbContext> ContextFactory { get; init; }

    public EfDbSetup(
        Func<string, DbContext> contextFactory,
        string dbName,
        string containerConnectionString,
        string migrationsPath,
        DbType dbType = DbType.Other,
        bool restoreFromDump = false,
        string? restorationStateFilesDirectory = null,
        IFileSystem? fileSystem = null)
            : base(
                dbName,
                containerConnectionString,
                migrationsPath,
                dbType,
                restoreFromDump,
                restorationStateFilesDirectory,
                 fileSystem)
    {
        ContextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <inheridoc />
    public override Task<DateTime> GetMigrationsLastModificationDateAsync(CancellationToken cancellationToken = default)
    {
        var dirInfo = FileSystem.DirectoryInfo.New(MigrationsPath);
        if (!dirInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Specified migrations folder does not exist ({MigrationsPath})");
        }

        // Use GetFileSystemInfos to get Files AND Directories recursively
        var allFileSystemEntries = dirInfo.GetFileSystemInfos("*", SearchOption.AllDirectories);

        if (allFileSystemEntries == null || allFileSystemEntries.Length == 0)
        {
            throw new FileNotFoundException($"Specified migrations folder is empty ({MigrationsPath})");
        }

        // Find the max date among files AND subdirectories
        var newestChange = allFileSystemEntries.Max(x => x.LastWriteTimeUtc);
        
        // Also compare against the root directory itself (in case a direct child was deleted)
        if (dirInfo.LastWriteTimeUtc > newestChange)
        {
            newestChange = dirInfo.LastWriteTimeUtc;
        }

        return Task.FromResult(newestChange);
    }
}