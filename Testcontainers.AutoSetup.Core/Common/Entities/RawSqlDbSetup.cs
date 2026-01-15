using System.IO.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Common.Entities;

public record RawSqlDbSetup : DbSetup
{
    /// <summary>
    /// A list of SQL files that will be executed in a provided DB in listed order.
    /// </summary>
    public virtual IList<string> SqlFiles { get; init; }

    public RawSqlDbSetup(
        IList<string> sqlFiles,
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
        if(sqlFiles.IsNullOrEmpty())
        {
            throw new ArgumentException("SQL files list cannot be null or empty.", nameof(sqlFiles));
        }

        SqlFiles = sqlFiles;
    }
}
