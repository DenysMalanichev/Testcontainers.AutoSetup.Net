using System.IO.Abstractions;
using System.Text;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Abstractions.Mongo;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.DbSeeding;

public class RawMongoDbSeeder : MongoDbSeeder
{
    public RawMongoDbSeeder(ILogger logger) : base(logger)
    { }

    /// <inheritdoc/>
    public override async Task SeedAsync(DbSetup dbSetup, IContainer container, CancellationToken cancellationToken)
    {
        if(dbSetup is not RawMongoDbSetup)
        {
            throw new ArgumentException($"{typeof(RawMongoDbSetup)} must be provided as an argument for raw SQL seeding.");
        }

        await ExecuteMongoScriptsAsync((RawMongoDbSetup)dbSetup, container, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the Mongo files defined in the RawFilesDbSetup against the target database.
    /// </summary>
    internal async Task ExecuteMongoScriptsAsync(
        RawMongoDbSetup dbSetup,
        IContainer container,
        CancellationToken cancellationToken)
    {
        var commandBuilder = new StringBuilder();

        bool isFirstImport = true;
        foreach ((string collectionName, string fileName) in dbSetup.MongoFiles)
        {
            if(isFirstImport) isFirstImport = false;
            else commandBuilder.Append(" && ");

            var containerFilePath = $"{Constants.MongoDB.DefaultMigrationsDataPath}/{fileName}";
           
            commandBuilder.Append("mongoimport ");
            commandBuilder.Append("--db ").Append(dbSetup.DbName).Append(' ');
            commandBuilder.Append("--collection ").Append(collectionName).Append(' ');
            commandBuilder.Append("--file ").Append(containerFilePath).Append(' ');
            commandBuilder.Append("--numInsertionWorkers 4 ");
            commandBuilder.Append("--username '").Append(dbSetup.Username).Append("' ");
            commandBuilder.Append("--password '").Append(dbSetup.Password).Append("' ");
            commandBuilder.Append("--authenticationDatabase '").Append(dbSetup.AuthenticationDatabase).Append("' ");
            commandBuilder.Append("--jsonArray");
        }
        var commandText = commandBuilder.ToString();
        _logger.LogInformation("Executing Mongo files against database '{Database}'", dbSetup.DbName);
        var result = await container.ExecAsync([
                "/bin/bash",
                "-c",
                commandText],
            cancellationToken
        ).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            _logger.LogError("Failed to migrate MongoDB files to {DbName} database", dbSetup.DbName);
            throw new ExecFailedException(result);
        }
    }
}
