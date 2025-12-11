using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.EntityFramework.Entities;

public record EfDbSetup : DbSetup
{
    public required Func<string, DbContext> ContextFactory { get; init; }
}