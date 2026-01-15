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
}