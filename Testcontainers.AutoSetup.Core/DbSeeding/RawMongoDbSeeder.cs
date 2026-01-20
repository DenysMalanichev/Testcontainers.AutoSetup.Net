using System.Globalization;
using System.Text;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Abstractions.Mongo;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;

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
        foreach (var file in dbSetup.MongoFiles)
        {
            if(isFirstImport) isFirstImport = false;
            else commandBuilder.Append(" && ");

            var containerFilePath = $"{Constants.MongoDB.DefaultMigrationsDataPath}/{file.FileName}.{file.FileExtension.ToString().ToLower(CultureInfo.InvariantCulture)}";
           
            commandBuilder.Append("mongoimport ");
            commandBuilder.Append("--db ").Append(dbSetup.DbName).Append(' ');
            commandBuilder.Append("--collection ").Append(file.CollectionName).Append(' ');
            commandBuilder.Append("--file ").Append(containerFilePath).Append(' ');
            commandBuilder.Append("--numInsertionWorkers 4 ");
            commandBuilder.Append("--username '").Append(dbSetup.Username).Append("' ");
            commandBuilder.Append("--password '").Append(dbSetup.Password).Append("' ");
            commandBuilder.Append("--authenticationDatabase '").Append(dbSetup.AuthenticationDatabase).Append("' ");
            commandBuilder.Append("--type ").Append(file.FileExtension.ToString().ToLower(CultureInfo.InvariantCulture));
            if(file.IsJsonArray.HasValue && file.IsJsonArray.Value)
            {
                commandBuilder.Append(" --jsonArray");
            }
            if(file.FileExtension is MongoDataFileExtension.CSV)
            {
                commandBuilder.Append(' ').Append(file.CsvImportFlag);
                if (!file.CsvImportParams.IsNullOrEmpty())
                {
                    commandBuilder.Append(' ').Append(file.CsvImportParams);
                }
            }
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
