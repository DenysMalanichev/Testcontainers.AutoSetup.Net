using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.EntityFramework.Entities;

public record EfDbSetup : DbSetup
{
    /// <summary>
    /// A <see cref="Func<>"/> taking a connection string and 
    /// returning an instance of <see cref="DbContext"/>
    /// </summary>
    public required Func<string, DbContext> ContextFactory { get; init; }

    /// <inheritdoc/>
    public override string BuildConnectionString(string containerConnStr)
    {
        if(DbName is not null)
        {
            containerConnStr = containerConnStr.Replace("Database=master", $"Database={DbName}");            
        }

        return containerConnStr;
    }

    /// <inheridoc />
    public override Task<DateTime> GetMigrationsLastModificationDateAsync(CancellationToken cancellationToken = default)
    {
        var dirInfo = new DirectoryInfo(MigrationsPath);

        if (!dirInfo.Exists)
        {
            throw new FileNotFoundException($"Specified migrations folder does not exist ({MigrationsPath})");
        }

        var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);

        if (files.Length == 0)
        {
            throw new FileNotFoundException($"Specified migrations folder is empty ({MigrationsPath})");
        }

        var newestFileTime = files.Max(f => f.LastWriteTimeUtc);

        // In case a file was deleted recently, which updates the folder but leaves no file "newer"
        if (dirInfo.LastWriteTimeUtc > newestFileTime)
        {
            return Task.FromResult(dirInfo.LastWriteTimeUtc);
        }

        return Task.FromResult(newestFileTime);
    }
}