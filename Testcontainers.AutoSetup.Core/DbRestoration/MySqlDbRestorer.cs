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

    /// <inheritdoc/>
    public override async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting restoration of MySQL {DbName} database...", _dbSetup.DbName);
        var stopwatch = Stopwatch.StartNew();
        await using var connection = _dbConnectionFactory.CreateDbConnection(_dbSetup.ContainerConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var restoreCommand = await CreateRestorationCommand(connection, cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = restoreCommand;
        var result = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        _logger.LogInformation("Restored MySQL DB in {time}", stopwatch.ElapsedMilliseconds);

        _logger.LogInformation("MySQL {DbName} database restored successfully.", _dbSetup.DbName);
    }

    /// <inheritdoc/>
    public override async Task SnapshotAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting snapshot of MySQL {DbName} database...", _dbSetup.DbName);
        var stopwatch = Stopwatch.StartNew();
        await using var connection = _dbConnectionFactory.CreateDbConnection(_dbSetup.ContainerConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await DisableRedoLogAsync(connection, cancellationToken).ConfigureAwait(false);
        await CreateGoldenStateDbAsync(connection, cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        _logger.LogInformation("Snapshoted MySQL DB in {time}", stopwatch.ElapsedMilliseconds);

        _logger.LogInformation("MySQL {DbName} database snapshot created successfully.", _dbSetup.DbName);
    }

    /// <summary>
    /// Disables the InnoDB redo log to speed up the restoration process.
    /// NOTE: Supported for MySQL 8.0 and later versions only.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DisableRedoLogAsync(DbConnection connection,  CancellationToken cancellationToken = default)
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
    /// Creates a golden state database as a template for future restorations.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task CreateGoldenStateDbAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating golden state database...");
        var goldenStateDbName = $"{_dbSetup.DbName}_golden_state";
        await using var createDbCmd = connection.CreateCommand();
        createDbCmd.CommandText = $"DROP DATABASE IF EXISTS `{goldenStateDbName}`; CREATE DATABASE `{goldenStateDbName}`;";
        var result = await createDbCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        if (result is not 1)
        {
            _logger.LogError("Failed to create golden state database.");
            throw new DbSetupException(goldenStateDbName);
        }

        var dataRestorationCommandBuilder = new StringBuilder();
        dataRestorationCommandBuilder.Append(
            $@"SET FOREIGN_KEY_CHECKS=0;
            SET UNIQUE_CHECKS = 0;");
        await foreach (var table in GetAllDbTablesAsync(connection, cancellationToken))
        {
            dataRestorationCommandBuilder.AppendLine($"CREATE TABLE IF NOT EXISTS `{goldenStateDbName}`.`{table}` LIKE `{_dbSetup.DbName}`.`{table}`;");
            dataRestorationCommandBuilder.AppendLine($"INSERT INTO `{goldenStateDbName}`.`{table}` SELECT * FROM `{_dbSetup.DbName}`.`{table}`;");
        }
        dataRestorationCommandBuilder.Append($@"   
            SET FOREIGN_KEY_CHECKS=1;
            SET UNIQUE_CHECKS = 1;");
        
        await using var dataRestorationCommand = connection.CreateCommand();
        dataRestorationCommand.CommandText = dataRestorationCommandBuilder.ToString();
        await dataRestorationCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation("Golden state database created successfully.");
    }

    /// <summary>
    /// Retrieves all tables' names from the specified database.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async IAsyncEnumerable<string> GetAllDbTablesAsync(
        DbConnection connection, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var getAllTablesCommand = @$"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = '{_dbSetup.DbName}'
                AND table_type = 'BASE TABLE';";
        await using var command = connection.CreateCommand();
        command.CommandText = getAllTablesCommand;
        await using var result = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if(result.HasRows)
        {
            _logger.LogInformation("Successfully identified DB tables.");
            // The result must be a single column with table names. 0 identifies this single column
            while (await result.ReadAsync(cancellationToken).ConfigureAwait(false))
                yield return result.GetString(0);
        }
        else
        {   
            _logger.LogError("Failed to identify DB tables.");
            throw new DbSetupException($"Failed to identify {_dbSetup.DbName} DB tables", $"{_dbSetup.DbName}_golden_state");   
        }
    }

    /// <summary>
    /// Creates the SQL command to restore the database from the golden state.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<string> CreateRestorationCommand(DbConnection connection, CancellationToken cancellationToken = default)
    {
        var command = new StringBuilder();
        // command.AppendLine($@"USE `{_dbSetup.DbName}`;");
        command.Append($@"
            SET FOREIGN_KEY_CHECKS=0;
            SET UNIQUE_CHECKS = 0;
        ");

        await foreach(var tableName in GetAllDbTablesAsync(connection, cancellationToken))
        {
            command.AppendLine($@"TRUNCATE TABLE `{_dbSetup.DbName}`.`{tableName}`;");
            command.AppendLine($@"INSERT INTO `{_dbSetup.DbName}`.`{tableName}` SELECT * FROM `{_dbSetup.DbName}_golden_state`.`{tableName}`;");
        }

        command.Append($@"   
            SET FOREIGN_KEY_CHECKS=1;
            SET UNIQUE_CHECKS = 1;");

        return command.ToString();
    }
}
