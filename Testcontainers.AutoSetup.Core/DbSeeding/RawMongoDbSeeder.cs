using System.IO.Abstractions;
using System.Text;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Abstractions.Entities;
using Testcontainers.AutoSetup.Core.Abstractions.Mongo;
using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Core.DbSeeding;

public class RawMongoDbSeeder : MongoDbSeeder
{
    private readonly IFileSystem _fileSystem;
    public RawMongoDbSeeder(IFileSystem fileSystem, ILogger? logger) : base(logger)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

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
        // TODO enforce a TMPFS mount use
        // TODO import these files into containers mount
        var commandBuilder = new StringBuilder();
        commandBuilder.AppendLine("set -e"); // fail the whole script if any single import fails

        foreach ((string collectionName, string fileName) in dbSetup.MongoFiles)
        {
            var containerFilePath = $"/var/tmp/migrations/data/{fileName}"; // TODO move to const
            commandBuilder.Append(" && \\");
            commandBuilder.AppendLine();
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
        var commandText = commandBuilder.ToString().Replace("\r\n", "");
        _logger.LogInformation("Executing Mongo files against database '{Database}'", dbSetup.DbName);
        var result = await container.ExecAsync([
                "/bin/bash",
                "-c",
                commandText],
            cancellationToken
        );

        // TODO currently status message is returned as Strerr, 
        // test if it is possible to have errors importing files yet ExitCode = 0
        if (/*!result.Stderr.IsNullOrEmpty() ||*/ result.ExitCode != 0)
        {
            _logger.LogError("Failed to migrate MongoDB files to {DbName} database", dbSetup.DbName);
            throw new ExecFailedException(result);
        }
    }
}
