using System.Data.Common;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Abstractions.Sql;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.DbSeeding;

public sealed class RawSqlDbSeeder : SqlDbSeeder
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IFileSystem _fileSystem;
    // Regex explanation:
    // ^\s* -> Start of a line, allow optional whitespace
    // GO        -> The literal word GO
    // \s* -> Allow optional whitespace after
    // $         -> End of the line
    // Multiline -> Treat string as lines, not just one long input
    // IgnoreCase-> Allow "go", "GO", "Go"
    private static readonly Regex GoSplitter = new(@"^\s*GO\s*$", 
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public RawSqlDbSeeder(IDbConnectionFactory dbConnectionFactory, IFileSystem fileSystem, ILogger logger)
        : base(logger)
    {
        _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <inheridoc />
    public override async Task SeedAsync(DbSetup dbSetup, IContainer container, CancellationToken cancellationToken = default)
    {
        if(dbSetup is not RawSqlDbSetup)
        {
            throw new ArgumentException($"{typeof(RawSqlDbSetup)} must be provided as an argument for raw SQL seeding.");
        }

        await ExecuteSqlFilesAsync((RawSqlDbSetup)dbSetup, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the SQL files defined in the RawSqlDbSetup against the target database.
    /// </summary>
    internal async Task ExecuteSqlFilesAsync(RawSqlDbSetup dbSetup, CancellationToken cancellationToken)
    {
        var migrationsDirectory = _fileSystem.Path.GetFullPath(dbSetup.MigrationsPath);
        await using var connection = _dbConnectionFactory.CreateDbConnection(dbSetup.ContainerConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        
        await using var sqlQuery = connection.CreateCommand();
        
        foreach (var sqlFile in dbSetup.SqlFiles)
        {
            _logger.LogInformation("Executing SQL file '{SqlFile}' against database '{Database}'", sqlFile, dbSetup.DbName);
            var fullFilePath = _fileSystem.Path.Combine(migrationsDirectory, sqlFile);
            if(dbSetup.DbType == Common.Enums.DbType.MsSQL)
            {
                await ExecuteSplittingOnGoBatchesAsync(fullFilePath, sqlQuery, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                sqlQuery.CommandText = await _fileSystem.File.ReadAllTextAsync(fullFilePath, cancellationToken).ConfigureAwait(false);
                await sqlQuery.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Executes the SQL batches split on "GO" statements.
    /// Must be used to execute only against MS SQL databases.
    /// </summary>
    /// <param name="fullFilePath"></param>
    /// <param name="sqlQuery"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    internal async Task ExecuteSplittingOnGoBatchesAsync(string fullFilePath, DbCommand sqlQuery, CancellationToken cancellationToken)
    {
        var commandSql = await _fileSystem.File.ReadAllTextAsync(fullFilePath, cancellationToken).ConfigureAwait(false);
        var batches = GoSplitter.Split(commandSql);
        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            sqlQuery.CommandText = batch;
            await sqlQuery.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}