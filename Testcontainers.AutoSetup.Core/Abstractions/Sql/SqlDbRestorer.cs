using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;

namespace Testcontainers.AutoSetup.Core.Abstractions.Sql;

/// <summary>
/// Abstraction of a restorer for SQL
/// </summary>
public abstract class SqlDbRestorer(DbSetup dbSetup, IContainer container, ILogger logger)
    : DbRestorer(dbSetup, container, logger)
{
}