using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.DbRestoration;

namespace Testcontainers.AutoSetup.Core.Common.DbStrategy;

public partial class DbSetupStrategyBuilder
{
    /// <summary>
    /// Sets up <see cref="MySqlDbRestorer"/> to restore a DB.
    /// </summary>
    /// <param name="connectionFactory">Connection factory used to access the DB</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public DbSetupStrategyBuilder WithMySqlDbRestorer(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        if (_restorer is not null)
            throw new ArgumentException("Restorer is already initialized.");

        _restorer = new MySqlDbRestorer(_dbSetup, _container, connectionFactory, _logger);
        return this;
    }
}
