using System.IO.Abstractions;
using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Abstractions.Entities;

public abstract record MongoDbSetup : DbSetup
{
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
    
    public MongoDbSetup(
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
    }
}