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
        string migrationsPath,
        DbType dbType = DbType.Other, // TODO set db type automatically
        bool restoreFromDump = false,
        string? restorationStateFilesDirectory = null,
        IFileSystem? fileSystem = null)
            : base(
                dbName,
                string.Empty, // connection string - not required for MongoDB restoration from raw files 
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
}
