using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Testcontainers.AutoSetup.Core.Abstractions.Mongo;

/// <summary>
/// Abstraction of a seeder for Mongo
/// </summary>
/// <param name="logger"></param>
public abstract class MongoDbSeeder(ILogger? logger) : DbSeeder(logger)
{
}