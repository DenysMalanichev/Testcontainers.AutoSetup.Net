using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.EntityFramework.Entities;

public record EfDbSetup : DbSetup
{
    public required Func<string, DbContext> ContextFactory { get; init; }

    public override string BuildConnectionString(string containerConnStr)
    {
        if(DbName is not null)
        {
            containerConnStr = containerConnStr.Replace("Database=master", $"Database={DbName}");            
        }

        return containerConnStr;
    }
}