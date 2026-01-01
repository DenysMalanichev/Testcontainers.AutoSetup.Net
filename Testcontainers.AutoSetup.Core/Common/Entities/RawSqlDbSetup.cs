using System.IO.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;

namespace Testcontainers.AutoSetup.Core.Common.Entities;

public record RawSqlDbSetup : DbSetup
{
    /// <summary>
    /// A list of SQL files that will be executed in a provided DB in listed order.
    /// </summary>
    public virtual required IList<string> SqlFiles { get; init; }

    /// <inheridoc />
    public override Task<DateTime> GetMigrationsLastModificationDateAsync(CancellationToken cancellationToken = default)
    {
        var dirInfo = _fileSystem.DirectoryInfo.New(MigrationsPath);
        if (!dirInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Specified migrations folder does not exist ({MigrationsPath})");
        }

        // Use GetFileSystemInfos to get Files AND Directories recursively
        var fileSystemEntries = dirInfo.GetFileSystemInfos("*", SearchOption.AllDirectories)
            .Where(f => SqlFiles.Contains(f.Name))
            .ToArray();

        if (fileSystemEntries == null || fileSystemEntries.Length == 0)
        {
            throw new FileNotFoundException($"Specified migrations folder is empty ({MigrationsPath})");
        }
        if (fileSystemEntries.Length != SqlFiles.Count)
        {
            throw new FileNotFoundException($"Some of the specified SQL files were not found in the migrations folder ({MigrationsPath})");
        }

        var newestChange = fileSystemEntries.Max(x => x.LastWriteTimeUtc);

        if (dirInfo.LastWriteTimeUtc > newestChange)
        {
            newestChange = dirInfo.LastWriteTimeUtc;
        }

        return Task.FromResult(newestChange);
    }
}
