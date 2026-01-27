using System.IO.Abstractions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.EntityFramework.Abstractions;

namespace Testcontainers.AutoSetup.EntityFramework.Entities;

public record MongoEfDbSetup : MongoDbSetup, IEfContextFactory
{
    public Func<string, DbContext> _contextFactoryDelegate { get; init; }

    public MongoEfDbSetup(
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
        _contextFactoryDelegate = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public DbContext ContextFactory(string connectionString)
    {
        return _contextFactoryDelegate(connectionString);
    }
}
