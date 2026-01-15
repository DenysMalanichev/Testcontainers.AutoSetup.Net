using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Abstractions.Mongo;
using Testcontainers.AutoSetup.Core.Abstractions.Sql;

namespace Testcontainers.AutoSetup.Core.Common;

public partial class TestEnvironment
{
    // A list of "strategies" to perform on init and reset
    private readonly List<Func<CancellationToken, Task>> _initializeTasks = [];
    private readonly List<Func<CancellationToken, Task>> _resetTasks = [];

    /// <summary>
    /// Registers a specific database strategy and container pair to the initialization queue.
    /// </summary>
    /// <remarks>
    /// This method does not execute the strategy immediately. It stores a factory delegate 
    /// that will be invoked when <see cref="InitializeAsync"/> is called.
    /// </remarks>
    /// <typeparam name="TSeeder">The type of the database seeder implementing <see cref="IDbSeeder"/>.</typeparam>
    /// <typeparam name="TRestorer">The type of the database restorer implementing <see cref="IDbRestorer"/>.</typeparam>
    /// <param name="dbSetup">The database configuration and schema setup definition.</param>
    /// <param name="container">The specific container instance to operate on.</param>
    /// <param name="connectionFactory">The factory implementing <see cref="IDbConnectionFactory"/> which creates a connection to the DB.</param>
    /// <param name="tryInitialRestoreFromSnapshot">An optional flag identifying whether to try the restore from existing snapshot</param> 
    /// <param name="logger">Optional <see cref="ILogger"/> instance. Default Testcontainer's logger would be used if not provided</param> 
    public void RegisterSqlDb<TSeeder, TRestorer>(
        DbSetup dbSetup,
        IContainer container,
        IDbConnectionFactory connectionFactory,
        bool tryInitialRestoreFromSnapshot = true,
        ILogger? logger = null)
            where TSeeder : SqlDbSeeder
            where TRestorer : SqlDbRestorer
    {
        var resetStrategy = new DbSetupStrategy<TSeeder, TRestorer>(
            dbSetup,
            container,
            connectionFactory,
            tryInitialRestoreFromSnapshot: tryInitialRestoreFromSnapshot,
            logger: logger);
        _initializeTasks.Add(resetStrategy.InitializeGlobalAsync);
        _resetTasks.Add(resetStrategy.ResetAsync);
    }

    /// <summary>
    /// Registers a specific database strategy and container pair to the initialization queue.
    /// </summary>
    /// <remarks>
    /// This method does not execute the strategy immediately. It stores a factory delegate 
    /// that will be invoked when <see cref="InitializeAsync"/> is called.
    /// </remarks>
    /// <typeparam name="TSeeder">The type of the database seeder implementing <see cref="MongoDbSeeder"/>.</typeparam>
    /// <typeparam name="TRestorer">The type of the database restorer implementing <see cref="MongoDbRestorer"/>.</typeparam>
    /// <param name="dbSetup">The database configuration and schema setup definition.</param>
    /// <param name="container">The specific container instance to operate on.</param>
    /// <param name="tryInitialRestoreFromSnapshot">An optional flag identifying whether to try the restore from existing snapshot</param> 
    /// <param name="logger">Optional <see cref="ILogger"/> instance. Default Testcontainer's logger would be used if not provided</param> 
    public void RegisterMongoDb<TSeeder, TRestorer>(
        DbSetup dbSetup,
        IContainer container,
        bool tryInitialRestoreFromSnapshot = true,
        ILogger? logger = null)
            where TSeeder : MongoDbSeeder
            where TRestorer : MongoDbRestorer
    {
        var resetStrategy = new DbSetupStrategy<TSeeder, TRestorer>(
            dbSetup,
            container,
            tryInitialRestoreFromSnapshot: tryInitialRestoreFromSnapshot,
            logger: logger);
        _initializeTasks.Add(resetStrategy.InitializeGlobalAsync);
        _resetTasks.Add(resetStrategy.ResetAsync);
    }

    /// <summary>
    /// Executes all registered database initialization strategies concurrently.
    /// </summary>
    /// <remarks>
    /// This iterates through all registered strategies, invokes their factory delegates, 
    /// and awaits their completion. This is typically used for the "Cold Start" phase 
    /// (seeding, snapshotting) at the beginning of a test session or after a reset.
    /// </remarks>
    /// <param name="ct">A token to cancel the initialization process.</param>
    /// <returns>A task that completes when all registered strategies have finished executing.</returns>
    public virtual async Task InitializeAsync(CancellationToken ct = default)
    {
        await Task.WhenAll(_initializeTasks.Select(x => x(ct))).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes all registered database reset strategies concurrently.
    /// </summary>
    /// <remarks>
    /// This iterates through all registered strategies, invokes their factory delegates, 
    /// and awaits their completion. This is typically used for the test rest phase 
    /// (recreating a DB from created snapshotting) at the before each test execution.
    /// </remarks>
    /// <param name="ct">A token to cancel the initialization process.</param>
    /// <returns>A task that completes when all registered strategies have finished executing.</returns>
    public virtual async Task ResetAsync(CancellationToken ct = default)
    {
        await Task.WhenAll(_resetTasks.Select(x => x(ct))).ConfigureAwait(false);
    }
}
