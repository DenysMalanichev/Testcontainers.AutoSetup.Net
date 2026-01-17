
using Testcontainers.AutoSetup.Core.DbRestoration;

namespace Testcontainers.AutoSetup.Core.Common.DbStrategy;

public partial class DbSetupStrategyBuilder
{
    /// <summary>
    ///  Sets up <see cref="MongoDbRestorer"/> to restore a DB.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public DbSetupStrategyBuilder WithMongoDbRestorer()
    {
        if (_restorer is not null)
            throw new ArgumentException("Restorer is already initialized.");

        _restorer = new MongoDbRestorer(_dbSetup, _container, _logger);
        return this;
    }
}