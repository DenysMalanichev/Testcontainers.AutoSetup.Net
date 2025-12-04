using DotNet.Testcontainers.Containers;

namespace TestcontainersAutoSetup.Core.Abstractions;

public interface IDbManager<TContainer>
    where TContainer : IContainer
{
    /// <summary>
    /// Applies the database migrations (e.g., runs Liquibase update, EF Core migrations).
    /// </summary>
    Task ApplyMigrationsAsync();

    /// <summary>
    /// Gets the timestamp of the most recently modified migration script.
    /// </summary>
    /// <returns>A UTC DateTime of the last modification.</returns>
    Task<DateTime> GetLastMigrationTimeUtcAsync();

    /// <summary>
    /// Creates a database snapshot inside the container.
    /// </summary>
    /// <param name="container">The running database container.</param>
    /// <param name="snapshotFileName">The name of the file for the snapshot.</param>
    Task CreateSnapshotAsync(TContainer container, string snapshotFileName);

    /// <summary>
    /// Gets the information about the latest snapshot file available.
    /// </summary>
    /// <param name="container">The running database container.</param>
    /// <returns>A tuple containing the snapshot name and its creation time, or null if none exist.</returns>
    Task<(string? Name, DateTime? Timestamp)> GetLatestSnapshotInfoAsync(TContainer container);

    /// <summary>
    /// Removes all existing snapshots.
    /// </summary>
    /// <param name="container">The running database container.</param>
    Task ClearSnapshotsAsync(TContainer container);
}
