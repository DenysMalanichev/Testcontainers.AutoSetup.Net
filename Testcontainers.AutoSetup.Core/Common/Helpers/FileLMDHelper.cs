using System.IO.Abstractions;

namespace Testcontainers.AutoSetup.Core.Common.Helpers;

public static class FileLMDHelper
{
    /// <summary>
    /// Returns the latest Last Modification Date among the provided files
    /// </summary>
    /// <param name="directory">Absolute or relative path to a common files directory</param>
    /// <param name="files">List of file names to check, which may be in subdirectories of a parent directory</param>
    /// <param name="fileSystem">Optional parameter used to substitute the default <see cref="FileSystem"/></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public static DateTime GetDirectoryFilesLastModificationDate(
        string directory, string[] files, IFileSystem fileSystem = null!)
    {
        var dirInfo = fileSystem is null
            ? new FileSystem().DirectoryInfo.New(directory)
            : fileSystem.DirectoryInfo.New(directory);

        if (!dirInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Specified migrations folder does not exist ({directory})");
        }

        // gets Files AND Directories recursively
        var fileSystemEntries = dirInfo.GetFileSystemInfos("*", SearchOption.AllDirectories)
            .Where(f => files.Contains(f.Name))
            .ToArray();

        if (fileSystemEntries == null || fileSystemEntries.Length == 0)
        {
            throw new FileNotFoundException($"Specified migrations folder is empty ({directory})");
        }
        if (fileSystemEntries.Length != files.Length)
        {
            throw new FileNotFoundException($"Some of the specified SQL files were not found in the migrations folder ({directory})");
        }

        var newestChange = fileSystemEntries.Max(x => x.LastWriteTimeUtc);

        if (dirInfo.LastWriteTimeUtc > newestChange)
        {
            newestChange = dirInfo.LastWriteTimeUtc;
        }

        return newestChange;
    }

    /// <summary>
    /// Returns the latest Last Modification Date of among the files and subdirectories of provided directory
    /// </summary>
    /// <param name="directory">Absolute or relative path to a target directory</param>
    /// <param name="fileSystem">Optional parameter used to substitute the default <see cref="FileSystem"/></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public static DateTime GetDirectoryLastModificationDate(string directory, IFileSystem fileSystem = null!)
    {
        var dirInfo = fileSystem is null
            ? new FileSystem().DirectoryInfo.New(directory)
            : fileSystem.DirectoryInfo.New(directory);

        if (!dirInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Specified migrations folder does not exist ({directory})");
        }

        // Use GetFileSystemInfos to get Files AND Directories recursively
        var allFileSystemEntries = dirInfo.GetFileSystemInfos("*", SearchOption.AllDirectories);

        if (allFileSystemEntries == null || allFileSystemEntries.Length == 0)
        {
            throw new FileNotFoundException($"Specified migrations folder is empty ({directory})");
        }

        // Find the max date among files AND subdirectories
        var newestChange = allFileSystemEntries.Max(x => x.LastWriteTimeUtc);
        
        // Also compare against the root directory itself (in case a direct child was deleted)
        if (dirInfo.LastWriteTimeUtc > newestChange)
        {
            newestChange = dirInfo.LastWriteTimeUtc;
        }

        return newestChange;
    }
}
