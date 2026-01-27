using System.IO.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Common.Entities;

public record RawMongoDbSetup : MongoDbSetup
{
    /// <summary>
    /// Dictionary listing files' names and collections' name 
    /// of format "collection name - file name", used to seed data
    /// </summary>
    public virtual IList<RawMongoDataFile> MongoFiles { get; init; }

    public RawMongoDbSetup(
        IList<RawMongoDataFile> mongoFiles,
        string dbName,
        string migrationsPath,
        bool restoreFromDump = false,
        string? restorationStateFilesDirectory = null,
        IFileSystem? fileSystem = null)
            : base(
                dbName,
                string.Empty, // connection string - not required for MongoDB restoration from raw files 
                migrationsPath,
                DbType.MongoDB,
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
