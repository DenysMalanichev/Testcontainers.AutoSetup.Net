using System.Diagnostics;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;

namespace Testcontainers.AutoSetup.Core.DbRestoration;

public class MsSqlDbRestorer : DbRestorer
{
    private const string DefaultRestorationStateFilesPath = "/var/opt/mssql/Restoration";

    private ILogger _logger {get;}

    public MsSqlDbRestorer(
        DbSetup dbSetup,
        IContainer container,
        string containerConnectionString,
        string restorationStateFilesDirectory = DefaultRestorationStateFilesPath,
        ILogger logger = null!)
        : base(
            dbSetup,
            container,
            containerConnectionString,
            restorationStateFilesDirectory ?? DefaultRestorationStateFilesPath)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc/>
    public override async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateMasterConnectionAsync(_containerConnectionString);
        await connection.OpenAsync(cancellationToken);

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
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex)
        {
            await connection.DisposeAsync();
            _logger.LogError($"Restore failed: {ex.Message}");
            throw;
        }

        SqlConnection.ClearPool(connection);
    }

    /// <inheritdoc/>
    public override async Task SnapshotAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRestorationDirectoryExistsAsync();

        _restorationSnapshotName = $"{_dbSetup.DbName}_snapshot_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        await using var connection = CreateMasterConnectionAsync(_containerConnectionString);
        await connection.OpenAsync(cancellationToken);

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

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a connection to 'master' with Pooling ENABLED.
    /// We rely on ADO.NET internal pooling, which is efficient and thread-safe.
    /// </summary>
    private static SqlConnection CreateMasterConnectionAsync(string containerConnStr)
    {
        var masterBuilder = new SqlConnectionStringBuilder(containerConnStr)
        {
            InitialCatalog = "master",
            Pooling = true,
            ConnectTimeout = 30,
            ApplicationName = "IntTests_Restorer",
            Encrypt = false,
        };

        return new SqlConnection(masterBuilder.ConnectionString);
    }
}