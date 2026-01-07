using System.Data.Common;
using System.Data.SqlTypes;
using System.Runtime.CompilerServices;
using System.Text;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;

namespace Testcontainers.AutoSetup.Core.DbRestoration;

public class MySqlDbRestorer : DbRestorer
{
    // TODO think where to store this constant
    private const string DefaultRestorationStateFilesPath = "/var/lib/mysql/Restoration";

    private readonly ILogger _logger;
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public MySqlDbRestorer(
        DbSetup dbSetup,
        IContainer container,
        IDbConnectionFactory dbConnectionFactory,
        ILogger logger = null!)
        : base(dbSetup, container)
    {
        _logger = logger ?? NullLogger.Instance;
        _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
    }

    /// <inheritdoc/>
    public override async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting restoration of MySQL {DbName} database...", _dbSetup.DbName);
        await using var connection = _dbConnectionFactory.CreateDbConnection(_dbSetup.ContainerConnectionString);
        await connection.OpenAsync(cancellationToken);

        var restoreCommand = await CreateRestorationCommand(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = restoreCommand;
        var result = await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("MySQL {DbName} database restored successfully.", _dbSetup.DbName);
    }

    /// <inheritdoc/>
    public override async Task SnapshotAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting snapshot of MySQL {DbName} database...", _dbSetup.DbName);
        await using var connection = _dbConnectionFactory.CreateDbConnection(_dbSetup.ContainerConnectionString);
        await connection.OpenAsync(cancellationToken);

        await DisableRedoLogAsync(connection, cancellationToken);
        await CreateGoldenStateDbAsync(connection, cancellationToken);

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
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Redo log has been disabled.");
    }

    /// <inheritdoc/>
    public override async Task<bool> IsSnapshotUpToDateAsync(CancellationToken cancellationToken = default)
    {
        var migrationsLMD = await _dbSetup.GetMigrationsLastModificationDateAsync(cancellationToken);

        await using var connection = _dbConnectionFactory.CreateDbConnection(_dbSetup.ContainerConnectionString);
        await connection.OpenAsync(cancellationToken);
        // Gets the LMD of the first table created within golden state DB
        var checkCmd = $@"
            SELECT MIN(create_time) AS Creation_Time
            FROM information_schema.Tables
            WHERE table_schema = '{_dbSetup.DbName}_golden_state'";
        await using var command = connection.CreateCommand();
        command.CommandText = checkCmd;
        var result = await command.ExecuteScalarAsync(cancellationToken);
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
        var goldenStateDb = $"{_dbSetup.DbName}_golden_state";
        await using var createDbCmd = connection.CreateCommand();
        createDbCmd.CommandText = $"DROP DATABASE IF EXISTS `{goldenStateDb}`; CREATE DATABASE `{goldenStateDb}`;";
        var result = await createDbCmd.ExecuteNonQueryAsync(cancellationToken);

        if (result is not 1)
        {
            _logger.LogError("Failed to create golden state database.");
            // TODO create an exception type
            throw new Exception("Failed to create golden state database.");
        }

        var dataRestorationCommandBuilder = new StringBuilder();
        dataRestorationCommandBuilder.AppendLine($@"USE `{goldenStateDb}`;");
        dataRestorationCommandBuilder.Append(
            $@"SET FOREIGN_KEY_CHECKS=0;
            SET UNIQUE_CHECKS = 0;");
        await foreach (var table in GetAllDbTablesAsync(connection, cancellationToken))
        {
            dataRestorationCommandBuilder.AppendLine($"CREATE TABLE IF NOT EXISTS `{goldenStateDb}`.`{table}` LIKE `{_dbSetup.DbName}`.`{table}`;");
            dataRestorationCommandBuilder.AppendLine($"INSERT INTO `{goldenStateDb}`.`{table}` SELECT * FROM `{_dbSetup.DbName}`.`{table}`;");
        }
        dataRestorationCommandBuilder.Append($@"   
            SET FOREIGN_KEY_CHECKS=1;
            SET UNIQUE_CHECKS = 1;");
        
        await using var dataRestorationCommand = connection.CreateCommand();
        dataRestorationCommand.CommandText = dataRestorationCommandBuilder.ToString();
        await dataRestorationCommand.ExecuteNonQueryAsync(cancellationToken);
        
        _logger.LogInformation("Golden state database created successfully.");
    }

    /// <summary>
    /// Retrieves all table names from the specified database.
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
        await using var result = await command.ExecuteReaderAsync(cancellationToken);

        if(result.HasRows)
        {
            _logger.LogInformation("Successfully identified DB tables.");
            // The result must be a single column with table names. 0 identifies this single column
            while (await result.ReadAsync(cancellationToken))
                yield return result.GetString(0);
        }
        else
        {
            
        _logger.LogError("Failed to identify DB tables.");
        // TODO create an exception type
        throw new Exception("Failed to identify DB tables.");   
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
        command.AppendLine($@"USE `{_dbSetup.DbName}`;");
        command.Append($@"SET FOREIGN_KEY_CHECKS=0;
            SET UNIQUE_CHECKS = 0;
        ");

        await foreach(var tableName in GetAllDbTablesAsync(connection, cancellationToken))
        {
            command.AppendLine($@"TRUNCATE TABLE `{tableName}`;");
            command.AppendLine($@"INSERT INTO `{tableName}` SELECT * FROM `{_dbSetup.DbName}_golden_state`.`{tableName}`;");
        }

        command.Append($@"   
            SET FOREIGN_KEY_CHECKS=1;
            SET UNIQUE_CHECKS = 1;");

        return command.ToString();
    }
}
