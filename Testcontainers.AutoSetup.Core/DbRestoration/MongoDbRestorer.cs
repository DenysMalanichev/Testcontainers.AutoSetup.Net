using System.IO.Abstractions;
using System.Text;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.DbRestoration;

public class MongoDbRestorer : Abstractions.Mongo.MongoDbRestorer
{
    private const string DefaultMongoMigrationPath = "/var/tmp/migrations/data";
    private const string DefaultMongoMigrationStamps = "/var/tmp/migrations/time_staps";

    public MongoDbRestorer(DbSetup dbSetup, IContainer container, ILogger logger)
        : base(dbSetup, container, logger)
        { }

    public override async Task<bool> IsSnapshotUpToDateAsync(CancellationToken cancellationToken = default)
    {
        // TODO calculate hash of provided files in DbSetup and those stored in container. 
        // Return true if equal
        var originalMigrationsHash = await _dbSetup.GetMigrationsLastModificationDateAsync(cancellationToken);
        var containerMigrationsHash = await GetContainerFilesLMDAsync(cancellationToken);
        return originalMigrationsHash < containerMigrationsHash;
    }

    public override async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restoring {dbName} DB.", _dbSetup.DbName);

        var mongoDbSetup = (RawMongoDbSetup)_dbSetup;
        var sb = new StringBuilder();
        sb.Append("mongosh test_db --eval 'db.dropDatabase()' --quiet ");
        sb.Append($"--username '{mongoDbSetup.Username}' ");
        sb.Append($"--password '{mongoDbSetup.Password}' ");
        sb.Append($"--authenticationDatabase '{mongoDbSetup.AuthenticationDatabase}' && ");
        sb.Append("mongorestore --archive=/tmp/golden.gz --gzip --noIndexRestore "); // TODO no index restore - do I need to restore them manually?
        sb.Append($"--username '{mongoDbSetup.Username}' ");
        sb.Append($"--password '{mongoDbSetup.Password}' ");
        sb.Append($"--authenticationDatabase '{mongoDbSetup.AuthenticationDatabase}'");
        var command = sb.ToString();

        var result = await _container.ExecAsync(["/bin/bash", "-c", command], cancellationToken)
            .ConfigureAwait(false);

        // TODO stderr
        if(/*!result.Stderr.IsNullOrEmpty() ||*/ result.ExitCode != 0)
        {
            _logger.LogError("Failed to restore {dbName} DB from raw Mongo files", _dbSetup.DbName);
            throw new ExecFailedException(result);
        }
        
        _logger.LogInformation("Successfully restored {dbName} DB.", _dbSetup.DbName);
    }

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
        var result = await _container.ExecAsync(["/bin/bash", "-c", command], cancellationToken);
        
        // TODO check Stderr
        if(/*!result.Stderr.IsNullOrEmpty() ||*/ result.ExitCode != 0)
        {
            _logger.LogError("Failed to create a snapshot for {dbName} DB", _dbSetup.DbName);
            throw new ExecFailedException(result);
        }
        
        // TODO do I need to call .ConfigureAwait(false)??
        await CreateMigrationTimeStampFile(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully created a snapshot for {dbName} DB", _dbSetup.DbName);
    }

    private async Task<DateTime> GetContainerFilesLMDAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Retrieving {dbName} DB migration time stamp.", _dbSetup.DbName);

        var command = $"cat '{$"{DefaultMongoMigrationPath}/{_dbSetup.DbName}_migration_time.txt"}'";

        var result = await _container.ExecAsync(
            ["/bin/bash", "-c", command], cancellationToken);

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
        var command = $"mkdir -p {DefaultMongoMigrationStamps} && echo '{DateTime.UtcNow}' > {DefaultMongoMigrationStamps}/{_dbSetup.DbName}_migration_time_stamp.txt";

        var result = await _container.ExecAsync(
            ["/bin/bash", "-c", command], cancellationToken);

        if(!result.Stderr.IsNullOrEmpty() || result.ExitCode != 0)
        {
            _logger.LogError("Failed to read last migration time stamp for {dbName} DB.", _dbSetup.DbName);
            throw new ExecFailedException(result);
        }

        _logger.LogTrace("Created {dbName} DB migration time stamp file.", _dbSetup.DbName);
    }
}
