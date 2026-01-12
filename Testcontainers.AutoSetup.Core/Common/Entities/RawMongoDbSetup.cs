using System.IO.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Common.Entities;

public record RawMongoDbSetup : DbSetup
{
    /// <summary>
    /// Dictionary listing files' names and collections' name 
    /// of format "collection name - file name", used to seed data
    /// </summary>
    public virtual Dictionary<string, string> MongoFiles { get; init; }

    /// <summary>
    /// Username used for authentication. 
    /// Default value = 'mongo' - default value for Testcontaienrs
    /// </summary>
    public string Username { get; init; } = "mongo";

    /// <summary>
    /// Password used for authentication.
    /// Default value = 'mongo' - default value for Testcontaienrs
    /// </summary>
    public string Password { get; init; } = "mongo";

    /// <summary>
    /// Database used for authentication.
    /// Default value = 'admin'
    /// </summary>
    public string AuthenticationDatabase { get; init; } = "admin";

    public RawMongoDbSetup(
         Dictionary<string, string> mongoFiles,
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
        if(mongoFiles.IsNullOrEmpty())
        {
            throw new ArgumentNullException(nameof(mongoFiles));
        }

        MongoFiles = mongoFiles;
    }

    /// <inheritdoc/>
    public override Task<DateTime> GetMigrationsLastModificationDateAsync(CancellationToken cancellationToken = default)
    {
        // TODO consider implement a common helper to identify LMD
        var dirInfo = FileSystem.DirectoryInfo.New(MigrationsPath);
        if (!dirInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Specified migrations folder does not exist ({MigrationsPath})");
        }

        // Use GetFileSystemInfos to get Files AND Directories recursively
        var fileSystemEntries = dirInfo.GetFileSystemInfos("*", SearchOption.AllDirectories)
            .Where(f => MongoFiles.ContainsValue(f.Name))
            .ToArray();

        if (fileSystemEntries == null || fileSystemEntries.Length == 0)
        {
            throw new FileNotFoundException($"Specified migrations folder is empty ({MigrationsPath})");
        }
        if (fileSystemEntries.Length != MongoFiles.Count)
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
