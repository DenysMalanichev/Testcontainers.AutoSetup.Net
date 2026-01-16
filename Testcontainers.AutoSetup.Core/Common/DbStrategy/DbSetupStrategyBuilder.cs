using DotNet.Testcontainers;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;

namespace Testcontainers.AutoSetup.Core.Common.DbStrategy;

public partial class DbSetupStrategyBuilder
{
    internal DbSeeder _seeder = null!;
    internal DbRestorer _restorer = null!;

    internal readonly IContainer _container;
    internal readonly DbSetup _dbSetup;
    internal readonly bool _tryInitialRestoreFromSnapshot = true;
    internal readonly ILogger _logger;

    public DbSetupStrategyBuilder(
        DbSetup dbSetup,
        IContainer container,
        ILogger? logger = null,
        bool tryInitialRestoreFromSnapshot = true)
    {
        _logger = logger ?? ConsoleLogger.Instance;
        _tryInitialRestoreFromSnapshot = tryInitialRestoreFromSnapshot;
        _dbSetup = dbSetup ?? throw new ArgumentNullException(nameof(dbSetup));        
        _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    /// <summary>
    /// Builds an instance of <see cref="IDbStrategy"/> using configured params
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public IDbStrategy Build()
    {
        if(_seeder is null)
            throw new ArgumentException("Seeder is not configured.");
        if(_restorer is null)
            throw new ArgumentException("Restorer is not configured.");

        return new DbSetupStrategy(_dbSetup, _seeder, _restorer, _container, _tryInitialRestoreFromSnapshot);
    }
}
