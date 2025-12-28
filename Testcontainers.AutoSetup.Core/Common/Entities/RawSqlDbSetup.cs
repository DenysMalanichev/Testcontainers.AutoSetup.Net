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

        var files = GetMigrationFilesInfo(dirInfo);

        var newestFileTime = GetFilesLastModificationDate(files);

        // In case a file was deleted recently, which updates the folder but leaves no file "newer"
        if (dirInfo.LastWriteTimeUtc > newestFileTime)
        {
            return Task.FromResult(dirInfo.LastWriteTimeUtc);
        }

        return Task.FromResult(newestFileTime);
    }

    /// <summary>
    /// Gets the latest modification date from a list of files and their parrent directories
    /// </summary>
    /// <param name="files">Array of file information objects</param>
    /// <returns>The latest modification date among all files</returns>
    private static DateTime GetFilesLastModificationDate(IFileInfo[] files)
    {
        var maxTime = DateTime.MinValue;

        foreach (var fileInfo in files)
        {
            if (fileInfo.LastWriteTimeUtc > maxTime)
            {
                maxTime = fileInfo.LastWriteTimeUtc;
            }
        }

        return maxTime;
    }

    /// <summary>
    /// Gets file information objects for the specified SQL files in the migrations directory
    /// </summary>
    /// <param name="directoryInfo">The directory to search for SQL files</param>
    /// <returns>An array of FileInfo objects for the specified SQL files</returns>
    /// <exception cref="FileNotFoundException"></exception>
    private IFileInfo[] GetMigrationFilesInfo(IDirectoryInfo directoryInfo)
    {
        var files = directoryInfo.GetFiles("*", SearchOption.AllDirectories)?
            .Where(f => SqlFiles.Contains(f.Name))
            .ToArray();

        if (files is null || files.Length == 0)
        {
            throw new FileNotFoundException($"Specified migrations folder is empty ({MigrationsPath})");
        }
        if (files.Length != SqlFiles.Count)
        {
            var missingFiles = SqlFiles.Except(files.Select(f => f.Name));
            throw new FileNotFoundException($"The following specified SQL files were not found in the migrations folder ({MigrationsPath}): {string.Join(", ", missingFiles)}");
        }

        return files;
    }
}
