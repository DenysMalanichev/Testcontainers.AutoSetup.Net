using Microsoft.Extensions.Logging;

namespace Testcontainers.AutoSetup.Core.Abstractions.Sql;

/// <summary>
/// Abstraction of a seeder for SQL
/// </summary>
public abstract class SqlDbSeeder(ILogger? logger) : DbSeeder(logger)
{
}
