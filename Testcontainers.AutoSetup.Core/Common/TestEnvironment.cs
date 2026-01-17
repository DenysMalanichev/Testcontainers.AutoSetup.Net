using Testcontainers.AutoSetup.Core.Abstractions;

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
    public void RegisterDbSetupStrategy(IDbStrategy setupStrategy)
    {
        _initializeTasks.Add(setupStrategy.InitializeGlobalAsync);
        _resetTasks.Add(setupStrategy.ResetAsync);
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
