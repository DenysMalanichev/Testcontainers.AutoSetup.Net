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
    /// <typeparam name="TContainer">The type of the container implementing <see cref="IContainer"/>.</typeparam>
    /// <param name="dbSetup">The database configuration and schema setup definition.</param>
    /// <param name="container">The specific container instance to operate on.</param>
    /// <param name="resetStrategy">The strategy defining how the database should be initialized or reset.</param>
    /// <param name="connectionStringProvider">A function to extract the valid connection string from the container.</param>
    public void Register<TContainer>(
        DbSetup dbSetup,
        TContainer container, 
        IDbStrategy resetStrategy, 
        Func<TContainer, string> connectionStringProvider) 
        where TContainer : IContainer
    {
        _initializeTasks.Add((ct) => 
            resetStrategy.InitializeGlobalAsync(
                dbSetup, container, connectionStringProvider(container), ct));
        _resetTasks.Add((ct) => 
            resetStrategy.ResetAsync(
                container, connectionStringProvider(container), ct));
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
    public async Task InitializeAsync(CancellationToken ct = default)
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
    public async Task ResetAsync(CancellationToken ct = default)
    {
        await Task.WhenAll(_resetTasks.Select(x => x(ct)));
    }
}
