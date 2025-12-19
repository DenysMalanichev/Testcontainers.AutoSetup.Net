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
}