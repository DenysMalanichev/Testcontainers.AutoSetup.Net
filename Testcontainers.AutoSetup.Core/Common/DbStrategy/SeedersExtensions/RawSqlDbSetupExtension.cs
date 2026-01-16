using System.IO.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.DbSeeding;

namespace Testcontainers.AutoSetup.Core.Common.DbStrategy;

public partial class DbSetupStrategyBuilder
{
    /// <summary>
    /// Sets up <see cref="RawSqlDbSeeder"/> to seed a DB.
    /// </summary>
    /// <param name="connectionFactory">Connection factory used to access the DB</param>
    /// <param name="fileSystem">Optional parameter to override the default <see cref="FileSystem"/></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public DbSetupStrategyBuilder WithRawSqlDbSeeder(IDbConnectionFactory connectionFactory, IFileSystem? fileSystem = null!)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        
        if(_seeder is not null)
            throw new ArgumentException("Seeder is already initialized.");

        fileSystem ??= new FileSystem();
        _seeder = new RawSqlDbSeeder(connectionFactory, fileSystem, _logger);

        return this;
    }
}
