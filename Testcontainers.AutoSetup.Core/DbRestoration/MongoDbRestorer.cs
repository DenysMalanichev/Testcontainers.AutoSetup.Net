using System.IO.Abstractions;
using System.Text;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.Common.Helpers;

namespace Testcontainers.AutoSetup.Core.DbRestoration;

public class MongoDbRestorer : Abstractions.Mongo.MongoDbRestorer
{

    public MongoDbRestorer(DbSetup dbSetup, IContainer container, ILogger logger)
        : base(dbSetup, container, logger)
    { }

    /// <inheritdoc/>
    public override async Task<bool> IsSnapshotUpToDateAsync(IFileSystem fileSystem = null!, CancellationToken cancellationToken = default)
    {
        fileSystem ??= new FileSystem();

        var originalMigrationsHash = FileLMDHelper.GetDirectoryLastModificationDate(_dbSetup.MigrationsPath, fileSystem);
        var containerMigrationsHash = await GetContainerFilesLMDAsync(cancellationToken).ConfigureAwait(false);
        return originalMigrationsHash < containerMigrationsHash;
    }

    /// <inheritdoc/>
    public override async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restoring {dbName} DB.", _dbSetup.DbName);

            var mongoDbSetup = (RawMongoDbSetup)_dbSetup;
            var result = await _container.ExecAsync([
                "mongorestore",
                "--archive=/tmp/golden.gz",
                "--gzip",
                "--drop",
                "--username", mongoDbSetup.Username,
                "--password", mongoDbSetup.Password,
                "--authenticationDatabase", mongoDbSetup.AuthenticationDatabase
            ], cancellationToken).ConfigureAwait(false);

        if(result.ExitCode != 0)
        {
            _logger.LogError("Failed to restore {dbName} DB from raw Mongo files", _dbSetup.DbName);
            throw new ExecFailedException(result);
        }
        
        _logger.LogInformation("Successfully restored {dbName} DB.", _dbSetup.DbName);
    }

    ///<inheritdoc/>
    public override async Task SnapshotAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating a snapshot for {dbName} DB", _dbSetup.DbName);
        var mongoDbSetup = (RawMongoDbSetup)_dbSetup;

        var sb = new StringBuilder();
        sb.Append("mongodump");
        sb.Append($" --db={_dbSetup.DbName}");
        sb.Append(" --archive=/tmp/golden.gz");
        sb.Append(" --gzip");
        sb.Append($" --username '{mongoDbSetup.Username}'");
        sb.Append($" --password '{mongoDbSetup.Password}'");
        sb.Append($" --authenticationDatabase '{mongoDbSetup.AuthenticationDatabase}'");
        var command = sb.ToString();
        var result = await _container.ExecAsync(["/bin/bash", "-c", command], cancellationToken).ConfigureAwait(false);

        if(result.ExitCode != 0)
        {
            _logger.LogError("Failed to create a snapshot for {dbName} DB", _dbSetup.DbName);
            throw new ExecFailedException(result);
        }

        await CreateMigrationTimeStampFile(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully created a snapshot for {dbName} DB", _dbSetup.DbName);
    }

    private async Task<DateTime> GetContainerFilesLMDAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Retrieving {dbName} DB migration time stamp.", _dbSetup.DbName);

        var command = $"cat '{$"{Constants.MongoDB.DefaultMigrationsDataPath}/{_dbSetup.DbName}_migration_time.txt"}'";

        var result = await _container.ExecAsync(
            ["/bin/bash", "-c", command], cancellationToken).ConfigureAwait(false);

        if(result.ExitCode == 1)
        {
            return DateTime.MinValue;
        }

        if(!result.Stderr.IsNullOrEmpty() || result.ExitCode != 0)
        {
            _logger.LogError("Failed to read last migration time stamp for {dbName} DB.", _dbSetup.DbName);
            throw new ExecFailedException(result);
        }

        var parseResult = DateTime.TryParse(result.Stdout, out var dbMigrationTimeStamp);

        if(!parseResult)
        {
            _logger.LogError("Failed to get last migration time stamp for {dbName} DB.", _dbSetup.DbName);
            throw new InvalidOperationException($"Failed to get last migration time stamp for {_dbSetup.DbName} DB.");
        }

        _logger.LogTrace("Retrieved {dbName} DB migration time stamp ({timeStamp}).", _dbSetup.DbName, dbMigrationTimeStamp);

        return dbMigrationTimeStamp;
    }

    private async Task CreateMigrationTimeStampFile(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Creating {dbName} DB migration time stamp file.", _dbSetup.DbName);

        // Ensures the directory exists and write the time stamp into the files
        var command = $"mkdir -p {Constants.MongoDB.DefaultMigrationsTimestampsPath} && " + 
            $"echo '{DateTime.UtcNow}' > {Constants.MongoDB.DefaultMigrationsTimestampsPath}/{_dbSetup.DbName}_migration_time_stamp.txt";

        var result = await _container.ExecAsync(
            ["/bin/bash", "-c", command], cancellationToken).ConfigureAwait(false);

        if(!result.Stderr.IsNullOrEmpty() || result.ExitCode != 0)
        {
            _logger.LogError("Failed to read last migration time stamp for {dbName} DB.", _dbSetup.DbName);
            throw new ExecFailedException(result);
        }

        _logger.LogTrace("Created {dbName} DB migration time stamp file.", _dbSetup.DbName);
    }
}
