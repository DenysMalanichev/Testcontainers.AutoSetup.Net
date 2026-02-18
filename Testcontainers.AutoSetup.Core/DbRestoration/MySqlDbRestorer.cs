using System.Data.Common;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Abstractions.Sql;
using Testcontainers.AutoSetup.Core.Common.Exceptions;
using Testcontainers.AutoSetup.Core.Common.Helpers;

namespace Testcontainers.AutoSetup.Core.DbRestoration;

public class MySqlDbRestorer : SqlDbRestorer
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public MySqlDbRestorer(
        DbSetup dbSetup,
        IContainer container,
        IDbConnectionFactory dbConnectionFactory,
        ILogger logger)
        : base(dbSetup, container, logger)
    {
        _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
    }

    public override async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting restoration of MySQL {DbName} database...", _dbSetup.DbName);
        var stopwatch = Stopwatch.StartNew();

        await using var connection = _dbConnectionFactory.CreateDbConnection(_dbSetup.ContainerConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Direction: Golden State -> Test DB
        var sourceDb = $"{_dbSetup.DbName}_golden_state";
        var targetDb = _dbSetup.DbName;

        await CloneDatabaseAsync(connection, sourceDb, targetDb, cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation("Restored MySQL DB in {time}ms", stopwatch.ElapsedMilliseconds);
    }

    public override async Task SnapshotAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting snapshot of MySQL {DbName} database...", _dbSetup.DbName);
        var stopwatch = Stopwatch.StartNew();

        await using var connection = _dbConnectionFactory.CreateDbConnection(_dbSetup.ContainerConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await DisableRedoLogAsync(connection, cancellationToken).ConfigureAwait(false);

        // Direction: Test DB -> Golden State
        var sourceDb = _dbSetup.DbName;
        var targetDb = $"{_dbSetup.DbName}_golden_state";

        await CloneDatabaseAsync(connection, sourceDb, targetDb, cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation("Snapshotted MySQL DB in {time}ms", stopwatch.ElapsedMilliseconds);
    }

    private async IAsyncEnumerable<string> GetTablesFromDbAsync(
        DbConnection connection,
        string dbName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cmdText = $@"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = '{dbName}'
                AND table_type = 'BASE TABLE';";

        await using var command = connection.CreateCommand();
        command.CommandText = cmdText;
        await using var result = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (result.HasRows)
        {
            while (await result.ReadAsync(cancellationToken).ConfigureAwait(false))
                yield return result.GetString(0);
        }
    }

    /// <summary>
    /// Disables the InnoDB redo log to speed up the restoration process.
    /// NOTE: Supported for MySQL 8.0 and later versions only.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DisableRedoLogAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        const string disableRedoLogCommand = "ALTER INSTANCE DISABLE INNODB REDO_LOG;";
        await using var command = connection.CreateCommand();
        command.CommandText = disableRedoLogCommand;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Redo log has been disabled.");
    }

    /// <inheritdoc/>
    public override async Task<bool> IsSnapshotUpToDateAsync(IFileSystem fileSystem = null!, CancellationToken cancellationToken = default)
    {
        fileSystem ??= new FileSystem();

        var migrationsLMD = FileLMDHelper.GetDirectoryLastModificationDate(_dbSetup.MigrationsPath, fileSystem);

        await using var connection = _dbConnectionFactory.CreateDbConnection(_dbSetup.ContainerConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        // Gets the LMD of the first table created within golden state DB
        var checkCmd = $@"
            SELECT MIN(create_time) AS Creation_Time
            FROM information_schema.Tables
            WHERE table_schema = '{_dbSetup.DbName}_golden_state'";
        await using var command = connection.CreateCommand();
        command.CommandText = checkCmd;
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        var isSuccess = DateTime.TryParse(result!.ToString(), out var gsDbLMD);

        return isSuccess && migrationsLMD < gsDbLMD;
    }

    /// <summary>
    /// Performs a full deep clone of a MySQL database including Tables, Data, Views, Triggers, and Routines.
    /// </summary>
    private async Task CloneDatabaseAsync(DbConnection connection, string sourceDb, string targetDb, CancellationToken cancellationToken)
    {
        var initCmdText = $@"
        DROP DATABASE IF EXISTS `{targetDb}`; 
        CREATE DATABASE `{targetDb}`;
        SET FOREIGN_KEY_CHECKS=0; 
        SET UNIQUE_CHECKS=0;";

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = initCmdText;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // Clone Tables (Structure + Data + Indexes + Foreign Keys)
        await CloneTablesAsync(connection, sourceDb, targetDb, cancellationToken);

        // Clone Views
        await CloneViewsAsync(connection, sourceDb, targetDb, cancellationToken);

        // Clone Stored Procedures & Functions
        await CloneRoutinesAsync(connection, sourceDb, targetDb, cancellationToken);

        // Clone Triggers
        await CloneTriggersAsync(connection, sourceDb, targetDb, cancellationToken);

        // Re-enable Integrity Checks
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SET FOREIGN_KEY_CHECKS=1; SET UNIQUE_CHECKS=1;";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CloneTablesAsync(DbConnection connection, string sourceDb, string targetDb, CancellationToken cancellationToken)
    {
        var tableNames = new List<string>();
        await foreach (var table in GetTablesFromDbAsync(connection, sourceDb, cancellationToken))
        {
            tableNames.Add(table);
        }

        var sb = new StringBuilder();
        // Re-ensure checks are off for this batch
        sb.AppendLine($"USE `{targetDb}`; SET FOREIGN_KEY_CHECKS=0;");

        foreach (var tableName in tableNames)
        {
            string createSql = await GetCreateStatementAsync(connection, sourceDb, tableName, "TABLE", cancellationToken);
            if (createSql == null) continue;

            // Point to Target DB
            var fixedSql = createSql.Replace($"CREATE TABLE `{tableName}`", $"CREATE TABLE `{targetDb}`.`{tableName}`");
            sb.AppendLine(fixedSql + ";");
            sb.AppendLine($"INSERT INTO `{targetDb}`.`{tableName}` SELECT * FROM `{sourceDb}`.`{tableName}`;");
        }

        if (sb.Length > 0)
        {
            await ExecuteBatchAsync(connection, sb.ToString(), cancellationToken);
        }
    }

    private async Task CloneViewsAsync(DbConnection connection, string sourceDb, string targetDb, CancellationToken cancellationToken)
    {
        var views = new List<string>();
        var cmdText = $"SELECT TABLE_NAME FROM information_schema.VIEWS WHERE TABLE_SCHEMA = '{sourceDb}'";

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = cmdText;
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) views.Add(reader.GetString(0));
        }

        foreach (var viewName in views)
        {
            string createSql = await GetCreateStatementAsync(connection, sourceDb, viewName, "VIEW", cancellationToken);
            if (createSql == null) continue;

            var fixedSql = createSql.Replace($"`{sourceDb}`.", $"`{targetDb}`.");

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"USE `{targetDb}`; {fixedSql}";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task CloneRoutinesAsync(DbConnection connection, string sourceDb, string targetDb, CancellationToken cancellationToken)
    {
        // Procedures and Functions
        var routines = new List<(string Name, string Type)>();
        var cmdText = $"SELECT ROUTINE_NAME, ROUTINE_TYPE FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA = '{sourceDb}'";

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = cmdText;
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                routines.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        foreach (var (name, type) in routines)
        {
            string createSql = await GetCreateStatementAsync(connection, sourceDb, name, type, cancellationToken);
            if (createSql == null) continue;

            // Fix definition to not point to old DB schema if it's hardcoded
            var fixedSql = createSql.Replace($"`{sourceDb}`.", $"`{targetDb}`.");

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"USE `{targetDb}`; {fixedSql}";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task CloneTriggersAsync(DbConnection connection, string sourceDb, string targetDb, CancellationToken cancellationToken)
    {
        var triggers = new List<string>();
        var cmdText = $"SELECT TRIGGER_NAME FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = '{sourceDb}'";

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = cmdText;
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) triggers.Add(reader.GetString(0));
        }

        foreach (var trigger in triggers)
        {
            string createSql = await GetCreateStatementAsync(connection, sourceDb, trigger, "TRIGGER", cancellationToken);
            if (createSql == null) continue;

            var fixedSql = createSql.Replace($"`{sourceDb}`.", $"`{targetDb}`.");

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"USE `{targetDb}`; {fixedSql}";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Helper to fetch "SHOW CREATE X" cleanly
    /// </summary>
    private async Task<string> GetCreateStatementAsync(DbConnection connection, string db, string name, string type, CancellationToken token)
    {
        await using var cmd = connection.CreateCommand();
        // Type is TABLE, VIEW, PROCEDURE, FUNCTION, or TRIGGER
        cmd.CommandText = $"SHOW CREATE {type} `{db}`.`{name}`";

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(token);
            if (await reader.ReadAsync(token))
            {
                return reader.GetString(reader.FieldCount - 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get DDL for {type} {name}", type, name);
            throw;
        }
        return null!;
    }

    private static async Task ExecuteBatchAsync(DbConnection connection, string sql, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(sql)) return;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 300;
        await cmd.ExecuteNonQueryAsync(token);
    }
}