using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.DbRestoration;

namespace Testcontainers.AutoSetup.Core.Common.DbStrategy;

public partial class DbSetupStrategyBuilder
{
    public DbSetupStrategyBuilder WithMsSqlRestorer(IDbConnectionFactory dbConnectionFactory)
    {
        ArgumentNullException.ThrowIfNull(dbConnectionFactory);
        if(_restorer is not null)
            throw new ArgumentException("Restorer is already initialized.");

        _restorer = new MsSqlDbRestorer(_dbSetup, _container, dbConnectionFactory, _logger);

        return this;
    }
}