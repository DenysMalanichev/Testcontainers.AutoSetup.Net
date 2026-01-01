using System.IO.Abstractions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;

namespace Testcontainers.AutoSetup.EntityFramework.Entities;

public record EfDbSetup : DbSetup
{
    /// <summary>
    /// A <see cref="Func<>"/> taking a connection string and 
    /// returning an instance of <see cref="DbContext"/>
    /// </summary>
    public virtual required Func<string, DbContext> ContextFactory { get; init; }

    /// <inheridoc />
    public override Task<DateTime> GetMigrationsLastModificationDateAsync(CancellationToken cancellationToken = default)
    {
        var dirInfo = _fileSystem.DirectoryInfo.New(MigrationsPath);
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