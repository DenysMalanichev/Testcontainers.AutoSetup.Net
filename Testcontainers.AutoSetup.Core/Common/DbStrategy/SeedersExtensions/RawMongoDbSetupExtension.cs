using System.IO.Abstractions;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.DbSeeding;

namespace Testcontainers.AutoSetup.Core.Common.DbStrategy;

public partial class DbSetupStrategyBuilder
{
    /// <summary>
    /// Sets up <see cref="RawMongoDbSeeder"/> to restore a DB.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public DbSetupStrategyBuilder WithRawMongoDbSeeder()
    {
        if(_dbSetup is not RawMongoDbSetup)
            throw new ArgumentException($"Cannot use {typeof(RawMongoDbSeeder)} on {_dbSetup.GetType()}. Must be {typeof(RawMongoDbSetup)}.");
        if(_seeder is not null)
            throw new ArgumentException("Seeder is already initialized.");

        _seeder = new RawMongoDbSeeder(_logger);

        return this;
    }
}