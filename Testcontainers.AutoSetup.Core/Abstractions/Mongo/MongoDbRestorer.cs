using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;

namespace Testcontainers.AutoSetup.Core.Abstractions.Mongo;


/// <summary>
/// Abstraction of a restorer for Mongo
/// </summary>
public abstract class MongoDbRestorer(DbSetup dbSetup, IContainer container, ILogger logger)
    : DbRestorer(dbSetup, container, logger)
{
}