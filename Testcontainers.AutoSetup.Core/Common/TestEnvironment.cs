using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.Common;

public class TestEnvironment
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
    /// <param name="resetStrategy">The strategy defining how the database should be initialized or reset.</param>
    public void Register<TSeeder, TRestorer>(
        DbSetup dbSetup,
        IContainer container,
        bool tryInitialRestoreFromSnapshot = true,
        string? restorationStateFilesPath = null!)
            where TSeeder : IDbSeeder, new()
            where TRestorer : DbRestorer
    {
        var resetStrategy = new DbSetupStrategy<TSeeder, TRestorer>(
            dbSetup,
            container,
            tryInitialRestoreFromSnapshot,
            restorationStateFilesPath);
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
        await Task.WhenAll(_initializeTasks.Select(x => x(ct)));
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
        await Task.WhenAll(_resetTasks.Select(x => x(ct)));
    }
}
