using System.IO.Abstractions;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Abstractions.Sql;
using Testcontainers.AutoSetup.Core.Common.Helpers;

namespace Testcontainers.AutoSetup.Core.DbRestoration;

public class MsSqlDbRestorer : SqlDbRestorer
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public MsSqlDbRestorer(
        DbSetup dbSetup,
        IContainer container,
        IDbConnectionFactory dbConnectionFactory,
        ILogger logger)
        : base(dbSetup, container, logger)
    {
        _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
    }

    /// <inheritdoc/>
    public override async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _dbConnectionFactory.CreateDbConnection(_dbSetup.ContainerConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = $@"
            USE master;

            DECLARE @TargetDbId smallint = DB_ID('{_dbSetup.DbName}');
            DECLARE @LatestSnapshot nvarchar(128);
            DECLARE @DynSql nvarchar(MAX) = '';

            -- A. Find Latest Snapshot
            SELECT TOP 1 @LatestSnapshot = name
            FROM sys.databases
            WHERE source_database_id = @TargetDbId
            ORDER BY create_date DESC;

            IF @LatestSnapshot IS NULL
            BEGIN
                RAISERROR('No snapshot found for restoration.', 16, 1);
                RETURN;
            END

            -- B. MANUAL KILL (Fixes 'Database already open' error)
            -- If the DB is stuck in SINGLE_USER, we must kill the holder before we can ALTER it.
            DECLARE @KillSql nvarchar(MAX) = '';
            SELECT @KillSql = @KillSql + 'KILL ' + CAST(session_id AS varchar(10)) + '; '
            FROM sys.dm_exec_sessions
            WHERE database_id = @TargetDbId
               AND session_id <> @@SPID
               AND is_user_process = 1; -- Don't kill ourselves
            
            EXEC(@KillSql);

            -- C. Set SINGLE_USER (Now safe to do so)
            ALTER DATABASE [{_dbSetup.DbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

            -- D. Cleanup Old Snapshots (Required before Restore)
            SET @DynSql = '';
            SELECT @DynSql = @DynSql + 'DROP DATABASE [' + name + ']; '
            FROM sys.databases
            WHERE source_database_id = @TargetDbId
              AND name != @LatestSnapshot;
            
            EXEC(@DynSql);

            -- E. RESTORE (Must use Dynamic SQL)
            RESTORE DATABASE [{_dbSetup.DbName}] FROM DATABASE_SNAPSHOT = @LatestSnapshot;

            -- F. Force MULTI_USER
            IF (SELECT user_access_desc FROM sys.databases WHERE name = '{_dbSetup.DbName}') <> 'MULTI_USER'
            BEGIN
                ALTER DATABASE [{_dbSetup.DbName}] SET MULTI_USER;
            END
        ";

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 60;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            _logger.LogError($"Restore failed: {ex.Message}");
            throw;
        }

        SqlConnection.ClearPool((SqlConnection)connection);
    }

    /// <inheritdoc/>
    public override async Task SnapshotAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRestorationDirectoryExistsAsync().ConfigureAwait(false);

        _restorationSnapshotName = $"{_dbSetup.DbName}_snapshot_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        await using var connection = _dbConnectionFactory.CreateDbConnection(_dbSetup.ContainerConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = $@"
            USE [{_dbSetup.DbName}];
            ALTER DATABASE [{_dbSetup.DbName}] SET RECOVERY SIMPLE;
            CHECKPOINT;
            DBCC SHRINKFILE ('{_dbSetup.DbName}_log', 1);

            USE master;
            ALTER DATABASE [{_dbSetup.DbName}] SET AUTO_CLOSE OFF;

            -- Create the snapshot
            CREATE DATABASE [{_restorationSnapshotName}] 
            ON ( NAME = [{_dbSetup.DbName}], FILENAME = '{RestorationStateFilesPath}' )
            AS SNAPSHOT OF [{_dbSetup.DbName}];";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task<bool> IsSnapshotUpToDateAsync(IFileSystem fileSystem = null!, CancellationToken cancellationToken = default)
    {
        fileSystem ??= new FileSystem();

        return await IsSnapshotValidAsync(fileSystem, cancellationToken).ConfigureAwait(false) && 
               await IsMountExistsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if current snapshot is up to date with migrations
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ExecFailedException"></exception>
    private async Task<bool> IsSnapshotValidAsync(IFileSystem fileSystem, CancellationToken cancellationToken)
    {
        // COMMAND EXPLANATION:
        // Checks if at least one snapshot exists in the directory
        // "ls ... > /dev/null 2>&1" silence the output, returns true (0) if files exist, false (2) if not - masked.
        // find ... -newermt ...  -> Looks for files in the dir newer than the snapshot LMD
        // -print -quit         -> Stop searching as soon as we find ONE new file
        // test -n "..."        -> Returns Exit Code 0 (Success) if the string is NOT EMPTY (new files found)
        //                      -> Returns Exit Code 1 (Fail) if the string is EMPTY (no new files found)
        var migrationsLMD = FileLMDHelper.GetDirectoryLastModificationDate(_dbSetup.MigrationsPath, fileSystem);
        var cmd = $"ls {_dbSetup.RestorationStateFilesDirectory}/{_dbSetup.DbName}_snapshot_* > /dev/null 2>&1 && " + 
           $"test -n \"$(find {_dbSetup.RestorationStateFilesDirectory} " +
           $"-maxdepth 1 -name '{_dbSetup.DbName}_snapshot_*' " +
           $"-newermt '{migrationsLMD:yyyy-MM-dd HH:mm:ss}' -print -quit)\"";
        var result = await _container.ExecAsync( 
        [
            "/bin/bash", 
            "-c", 
            cmd
        ], cancellationToken).ConfigureAwait(false);

        if (result.ExitCode == 0 && result.Stderr.IsNullOrEmpty())
        {
            _logger.LogInformation("Snapshot is up to date (No newer migrations found).");
            return true; 
        }
        else if (result.ExitCode == 1 || result.ExitCode == 2)
        {
            _logger.LogWarning("No up-to-date snapshot exists, recreation required.");
            return false;
        }

        throw new ExecFailedException(result);
    }

    /// <summary>
    /// Ensures that the restoration directory exists 
    /// </summary>
    /// <exception cref="ExecFailedException">
    /// Thrown when failed to run a command to validate the mount
    /// </exception>
    private async Task<bool> IsMountExistsAsync(CancellationToken cancellationToken)
    {        
        var result = await _container.ExecAsync(
        [
            "/bin/bash", 
            "-c", 
            $"findmnt {_dbSetup.RestorationStateFilesDirectory}",
        ], cancellationToken).ConfigureAwait(false);

        if (result.ExitCode == 0)
        {
            _logger!.LogInformation($"Required mount found.");
            return true;
        }
        else if (result.ExitCode == 1)
        {
            _logger.LogWarning($"No mount found at {_dbSetup.RestorationStateFilesDirectory}. Skipping initial restoration.");
            return false;
        }
        
        // Unexpected errors (e.g., findmnt not installed, permission denied)
        throw new ExecFailedException(result);
    }
}