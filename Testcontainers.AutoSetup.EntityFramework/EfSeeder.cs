using System.Globalization;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.EntityFramework.Entities;

namespace Testcontainers.AutoSetup.EntityFramework;

public class EfSeeder : IDbSeeder
{
    private const string SnapshotsDirectory = "Snapshots";
    
    private readonly bool _tryRecreateFromDump;
    private readonly List<EfDbSetup> _dbSetups;

    public EfSeeder(bool tryRecreateFromDump = false, params EfDbSetup[] setups)
    {
        _tryRecreateFromDump = tryRecreateFromDump;
        _dbSetups = [.. setups];
    }

    public async Task SeedAsync(IContainer container, string connectionString, CancellationToken cancellationToken)
    {
        if (_tryRecreateFromDump)
        {
            await InitializeDatabaseWithDumpAsync(container, connectionString, cancellationToken);
        }

        await ApplyEFMigrationsAsync(connectionString, cancellationToken).ConfigureAwait(false);
    }

    private async Task InitializeDatabaseWithDumpAsync(IContainer container, string connectionString, CancellationToken cancellationToken)
    {
        // var latestSnapshotFile = await GetLatestSnapshotFileAsync(container).ConfigureAwait(false);

        // if (!isLocalRun)
        // {
        //     await ApplyEFMigrationsAsync(connection).ConfigureAwait(false);
        //     return;
        // }

        // bool areScriptsUpdated = AreLiquibaseScriptsUpdated(latestSnapshotFile.LastWriteTime);

        // if (latestSnapshotFile.Name is not null && areScriptsUpdated)
        // {
        //     await RemoveDeprecatedDbSnapshotsAsync(container, latestSnapshotFile.Name!).ConfigureAwait(false);
        // }

        // if (latestSnapshotFile.Name is null || areScriptsUpdated)
        // {
        //     await ApplyEFMigrationsAsync(connection).ConfigureAwait(false);
        //     await CreateDatabaseStateSnapshotAsync(container, connection).ConfigureAwait(false);
        // }
        // else
        // {
        //     await RestoreDatabaseStateAsync(container, latestSnapshotFile.Name).ConfigureAwait(false);
        // }
    }

    private async Task ApplyEFMigrationsAsync(string connectionString, CancellationToken cancellationToken)
    {
        foreach(var dbSetup in _dbSetups)
        {
            var finalConnectionString = dbSetup.BuildConnectionString(connectionString);

            using var dbContext = dbSetup.ContextFactory(finalConnectionString);

            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }

    private static async Task DropDatabaseIfExistsAsync(string connectionString)
    {
        // var builder = new MySqlConnectionStringBuilder(connectionString);

        // using var connection = new MySqlConnection(builder.ConnectionString);
        // await connection.OpenAsync().ConfigureAwait(false);
        // using var command = connection.CreateCommand();
        // command.CommandText = "DROP DATABASE IF EXISTS Catalog;";
        // await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    // private static bool AreLiquibaseScriptsUpdated(DateTime? snapshotUpdateDate)
    // {
    //     if (snapshotUpdateDate is null)
    //     {
    //         return true;
    //     }

    //     // Get the latest modification time of Liquibase scripts
    //     var lastModificationTime = Directory.GetFiles(MigrationsPath, "*", SearchOption.AllDirectories)
    //         .Concat(Directory.GetFiles(LiquibasePath, "*", SearchOption.AllDirectories))
    //         .Max(File.GetLastWriteTime);

    //     return lastModificationTime > snapshotUpdateDate;
    // }

    // private static async Task RemoveDeprecatedDbSnapshotsAsync(IContainer container, string snapshotFileName)
    // {
    //     var removeDumpCommand =
    //         $"rm -r {SnapshotsDirectory}/*";
    //     var result = await container.ExecAsync(new List<string> { "/bin/bash", "-c", removeDumpCommand }).ConfigureAwait(false);

    //     if (result.ExitCode != 0)
    //     {
    //         throw new InvalidOperationException($"Failed to remove a snapshot ({result.Stdout} {result.Stderr})");
    //     }
    // }

    // private static async Task CreateDatabaseStateSnapshotAsync(IContainer container, DatabaseConnection connectionData)
    // {
    //     using var connection = new MySqlConnection(connectionData.ConnectionString);
    //     await connection.OpenAsync().ConfigureAwait(false);

    //     using var command = connection.CreateCommand();
    //     command.CommandText = "FLUSH TABLES WITH READ LOCK;";
    //     await command.ExecuteNonQueryAsync().ConfigureAwait(false);

    //     // Execute commands to export the database state to a file
    //     var snapshotFileName = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.sql";
    //     var createAndDumpCommand =
    //         $"mkdir -p {SnapshotsDirectory} && cd {SnapshotsDirectory} && mysqldump -u root --password=123456Ab Catalog --result-file={snapshotFileName}";
    //     var result = await container.ExecAsync(new List<string> { "/bin/bash", "-c", createAndDumpCommand }).ConfigureAwait(false);

    //     if (result.ExitCode != 0)
    //     {
    //         throw new InvalidOperationException($"Failed to create a snapshot ({result.Stdout} {result.Stderr})");
    //     }

    //     command.CommandText = "UNLOCK TABLES;";
    //     await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    // }

    private static async Task RestoreDatabaseStateAsync(IContainer container, string snapshotFile)
    {
        // Execute commands to import the database state from the snapshot file
        var restoreCommand = $"cd {SnapshotsDirectory} && mysqladmin create Catalog ; mysql Catalog < {snapshotFile}";
        var result = await container.ExecAsync(new List<string> { "/bin/bash", "-c", restoreCommand }).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to load a dump: {result.Stdout} {result.Stderr}");
        }
    }

    private async Task<(string? Name, DateTime? LastWriteTime)> GetLatestSnapshotFileAsync(IContainer container)
    {
        var result = await container.ExecAsync(
            new List<string> { "ls", "-t", SnapshotsDirectory, }
            ).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            return (null!, null!);
        }
        var snapshots = result.Stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var currentSnapshotName = snapshots.FirstOrDefault();
        if (currentSnapshotName is null)
        {
            return (null!, null!);
        }
        var snapshotTimeStamp = currentSnapshotName.Split('.')[0]!;
        if (!DateTime.TryParseExact(snapshotTimeStamp, "yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastWriteTime))
        {
            throw new InvalidOperationException($"Failed to parse snapshot timestamp {snapshotTimeStamp}");
        }
        return (currentSnapshotName, lastWriteTime);
    }
}
