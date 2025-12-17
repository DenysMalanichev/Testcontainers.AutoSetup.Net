using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.DbRestoration;

public class MsSqlDbRestorer : DbRestorer
{
    /// <summary>
    /// Cached connections
    /// </summary>
    private static readonly ConcurrentDictionary<string, SqlConnection> _globalConnectionCache = new();

    private readonly Task _warmupTask;

    /// <inheritdoc/>
    public MsSqlDbRestorer(
        DbSetup dbSetup, 
        IContainer container,
        string containerConnectionString,
        string restorationStateFilesPath = "/var/opt/mssql/Restoration")
        : base(dbSetup, container, containerConnectionString, restorationStateFilesPath)
    {
        // Warm up connection to prevent reverse DNS timeouts
        // Runs immideately after the restorer class is constrcuted 
        Task[] warmupTasks = [
            WarmupInternalAsync(containerConnectionString),
            CreateRestorationDirectoryAsync()
        ];
        _warmupTask = Task.WhenAll(warmupTasks);
    }

    /// <inheritdoc/>
    public override async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await EnsureWarmUpIsFinishedAsync();

        // 1. Get Cached, Non-Pooled Connection
        var connection = await GetCachedMasterConnectionAsync(_containerConnectionString, cancellationToken);
        
        // 2. Kill Active Sessions & Restore
        var sql = $@"
            DECLARE @kill varchar(8000) = '';  
            SELECT @kill = @kill + 'kill ' + CONVERT(varchar(5), session_id) + ';'  
            FROM sys.dm_exec_sessions
            WHERE database_id  = db_id('{_dbSetup.DbName}') AND is_user_process = 1; 
            EXEC(@kill);

            USE master;
            RESTORE DATABASE [{_dbSetup.DbName}] FROM DATABASE_SNAPSHOT = '{_dbSetup.DbName}_snapshot';";

        try
        {
            using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // If restore fails, the connection might be tainted.
            InvalidateCache(_containerConnectionString);
            throw;
        }

        // 3. Clear the *Application's* Pool (Standard pool, not our cache)
        SqlConnection.ClearPool(new SqlConnection(_containerConnectionString));
        stopwatch.Stop();
        Console.WriteLine("[DB RESET IN]" + stopwatch.ElapsedMilliseconds);
    }

    /// <inheritdoc/>
    public override async Task SnapshotAsync(CancellationToken cancellationToken = default)
    {
        await EnsureWarmUpIsFinishedAsync();

        var snapshotName = $"{_dbSetup.DbName}_snapshot";
        var linuxPath = Path.Combine(_restorationStateFilesPath, snapshotName).Replace("\\", "/");

        var connection = await GetCachedMasterConnectionAsync(_containerConnectionString, cancellationToken);

        var sql = $@"
            USE [{_dbSetup.DbName}];
            ALTER DATABASE [{_dbSetup.DbName}] SET RECOVERY SIMPLE;
            CHECKPOINT;
            DBCC SHRINKFILE ('{_dbSetup.DbName}_log', 1);

            USE master;
            ALTER DATABASE [{_dbSetup.DbName}] SET AUTO_CLOSE OFF;

            IF EXISTS (SELECT name FROM sys.databases WHERE name = '{snapshotName}') DROP DATABASE [{snapshotName}];

            CREATE DATABASE [{snapshotName}] 
            ON ( NAME = [{_dbSetup.DbName}], FILENAME = '{linuxPath}' )
            AS SNAPSHOT OF [{_dbSetup.DbName}];";

        using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Creates and caches the sonnection to a DB
    /// </summary>
    /// <param name="containerConnStr">Connection <see cref="string"/> used to connect to a default container DB</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    private async Task<SqlConnection> GetCachedMasterConnectionAsync(string containerConnStr, CancellationToken ct)
    {
        var masterBuilder = new SqlConnectionStringBuilder(containerConnStr)
        {
            InitialCatalog = "master",
            Pooling = true,
            ConnectTimeout = 10,
            ApplicationName = "Tc_Restorer_NoPool" ,
            Encrypt = false,
        };

        var connection = _globalConnectionCache.GetOrAdd(masterBuilder.ConnectionString, _ => new SqlConnection(masterBuilder.ConnectionString));

        if (connection.State != ConnectionState.Open)
        {
            if (connection.State != ConnectionState.Closed) await connection.CloseAsync();
            await connection.OpenAsync(ct);
        }
        else
        {
            // Optional Heartbeat to detect stale connections
            try 
            {
                using var cmd = new SqlCommand("SELECT 1", connection);
                cmd.CommandTimeout = 1; 
                await cmd.ExecuteScalarAsync(ct);
            }
            catch
            {
                await connection.CloseAsync();
                await connection.OpenAsync(ct);
            }
        }

        return connection;
    }

    /// <summary>
    /// Removes a connection from cache
    /// </summary>
    /// <param name="containerConnStr">Connection <see cref="string"/> used to connect to a default container DB</param>
    private static void InvalidateCache(string containerConnStr)
    {
        var connectionBuilder = new SqlConnectionStringBuilder(containerConnStr)
        {
            InitialCatalog = "master",
            Pooling = false,
            ApplicationName = "Tc_Restorer_NoPool",
            Encrypt = false
        };
        _globalConnectionCache.TryRemove(connectionBuilder.ConnectionString, out _);
    }

    /// <summary>
    /// Ensures that the restoration directory exists 
    /// </summary>
    /// <returns></returns>
    private async Task CreateRestorationDirectoryAsync()
    {        
        var result = await _container.ExecAsync(["/bin/bash", "-c", $"mkdir -p {_restorationStateFilesPath}"]);
        if(result.ExitCode != 0 || !string.IsNullOrEmpty(result.Stderr))
        {
            throw new ExecFailedException(result);
        }
    }

    private async Task WarmupInternalAsync(string connStr)
    {
        try 
        {
            await GetCachedMasterConnectionAsync(connStr, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warmup Warning] {ex.Message}");
        }
    }

    private async Task EnsureWarmUpIsFinishedAsync()
    {
        if (_warmupTask != null)
        {
            await _warmupTask;
        }
    }
}