using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Confluent.SchemaRegistry;
using DotNet.Testcontainers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Abstractions;

namespace Testcontainers.AutoSetup.Kafka;

// TODO add a specific interface for kafka seeder(?)
/// <summary>
/// Implements the IInstanceStrategy interface to provide Kafka-specific seeding logic.
/// </summary>
public class KafkaSeeder : IInstanceStrategy
{
    private readonly KafkaSetupConfiguration _kafkaConfig;
    private readonly ILogger _logger;

    protected TimeSpan TopicDeletionTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public KafkaSeeder(KafkaSetupConfiguration kafkaSetup, ILogger logger)
    {
        _kafkaConfig = kafkaSetup ?? throw new ArgumentNullException(nameof(kafkaSetup));
        _logger = logger ?? ConsoleLogger.Instance;
    }

    /// <inheritdoc/>
    public async Task InitializeGlobalAsync(CancellationToken cancellationToken = default)
    {
        // No global initialization needed for Kafka seeding
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Synchronizing Kafka topics to match desired state...");
        using var adminClient = BuildAdminClient();

        var allTopicNames = await GetAllTopicNamesAsync(adminClient, cancellationToken);

        if (allTopicNames.Count > 0)
        {
            // Use the testable helper method to filter for user topics
            var userTopicsToDelete = await GetUserTopicsToDeleteAsync(adminClient, allTopicNames, cancellationToken);

            if (userTopicsToDelete.Count > 0)
            {
                await DeleteTopicsAsync(userTopicsToDelete, adminClient, cancellationToken);
            }
        }

        await PurgeSchemaRegistryAsync(cancellationToken);

        await CreateTopicsAsync(adminClient);
        await SeedSchemasAsync(cancellationToken);
        await SeedMessagesToTopicAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves all topic names from the Kafka cluster. 
    /// This method can be overridden in tests to simulate different cluster states without needing a real Kafka instance.
    /// </summary>
    [ExcludeFromCodeCoverage]
    protected virtual async Task<List<string>> GetAllTopicNamesAsync(IAdminClient adminClient, CancellationToken cancellationToken)
    {
        var metadata = await Task.Run(() => adminClient.GetMetadata(TimeSpan.FromSeconds(5)), cancellationToken);
        return metadata.Topics.Select(t => t.Topic).ToList();
    }

    /// <summary>
    /// Determines which topics should be deleted based on the full list of existing topics.
    /// This method can be overridden in tests to simulate different filtering logic without needing a real Kafka instance.
    /// </summary>
    [ExcludeFromCodeCoverage]
    protected virtual async Task<List<string>> GetUserTopicsToDeleteAsync(IAdminClient adminClient, List<string> allTopicNames, CancellationToken cancellationToken)
    {
        var describeResults = await adminClient.DescribeTopicsAsync(TopicCollection.OfTopicNames(allTopicNames));
        return describeResults.TopicDescriptions
            .Where(t => !t.IsInternal)
            // We assume that is RegistryServer is configured - registry is configured and we need to 
            // filter the _schema topic out
            .Where(t => _kafkaConfig.RegistryServer == null || t.Name != "_schemas")
            .Select(t => t.Name)
            .ToList();
    }

    /// <summary>
    /// Builds the Kafka AdminClient. 
    /// This is a separate method to allow for easy mocking in tests, and to centralize 
    /// any configuration logic for the client.
    /// </summary>
    /// <returns></returns>
    [ExcludeFromCodeCoverage]
    protected virtual IAdminClient BuildAdminClient()
    {
        _logger.LogInformation("Building Kafka AdminClient with bootstrap server: {BootstrapServer}", _kafkaConfig.BootstrapServer);
        var config = new AdminClientConfig { BootstrapServers = _kafkaConfig.BootstrapServer };
        return new AdminClientBuilder(config).Build();
    }

    /// <summary>
    /// Builds a Kafka Producer client. This is a separate method to allow for easy mocking in tests,
    /// and to centralize any configuration logic for the producer.
    /// </summary>
    /// <returns></returns>
    [ExcludeFromCodeCoverage]
    protected virtual IProducer<byte[], byte[]> BuildProducer()
    {
        _logger.LogInformation("Building Kafka Producer with bootstrap server: {BootstrapServer}", _kafkaConfig.BootstrapServer);
        var config = new ProducerConfig { BootstrapServers = _kafkaConfig.BootstrapServer };
        return new ProducerBuilder<byte[], byte[]>(config).Build();
    }

    /// <summary>
    /// Creates the desired Kafka topics as specified in the configuration.
    /// </summary>
    /// <param name="adminClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task CreateTopicsAsync(IAdminClient adminClient)
    {
        _logger.LogInformation("Initializing Kafka topics for bootstrap server: {BootstrapServer}", _kafkaConfig.BootstrapServer);
        
        var topicSpecifications = _kafkaConfig.Topics.Select(topic => new TopicSpecification
        {
            Name = topic.Name,
            NumPartitions = topic.Partitions,
            ReplicationFactor = 1,
        }).ToList();

        if (topicSpecifications.Count == 0) return;

        try
        {
            _logger.LogInformation("Creating Kafka topics...");
            // Creates all topics in a single, highly-optimized network request
            await adminClient.CreateTopicsAsync(topicSpecifications);
        }
        catch (CreateTopicsException ex)
        {
            // We gracefully handle topics that already exist
            var realErrors = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists).ToList();
            
            if (realErrors.Count > 0)
            {
                _logger.LogError("Failed to create Kafka topics. Errors: {ErrorMessages}", string.Join("; ", realErrors.Select(e => $"{e.Topic}: {e.Error.Reason}")));
                throw new Exception("Failed to create Kafka topics.");
            }
        }

        _logger.LogInformation("Kafka topics initialization complete.");
    }

    /// <summary>
    /// Seeds messages to the specified Kafka topics based on the configuration by sending them to the specified bootstrap server.
    /// </summary>
    /// <param name="kafkaConfig"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task SeedMessagesToTopicAsync(CancellationToken cancellationToken)
    {
        var producer = BuildProducer();
        foreach(var topicConfig in _kafkaConfig.Topics)
        {
            if(!topicConfig.MessagesToSeed!.IsNullOrEmpty())
            {
                foreach (var message in topicConfig.MessagesToSeed!)
                {
                    await producer.ProduceAsync(topicConfig.Name, message, cancellationToken);
                }
            }

            if(!topicConfig.CustomAsyncSeedActions.IsNullOrEmpty())
            {
                foreach (var customSeedActionAsync in topicConfig.CustomAsyncSeedActions!)
                {
                    await customSeedActionAsync(_kafkaConfig.BootstrapServer, _kafkaConfig.RegistryServer);
                }
            }            
        }
    }

    private async Task DeleteTopicsAsync(List<string> userTopicsToDelete, IAdminClient adminClient, CancellationToken ct)
    {
        List<string> unknownTopics = [];
        try
        {
            _logger.LogInformation("Purging {Count} existing user topics...", userTopicsToDelete.Count);
            await adminClient.DeleteTopicsAsync(userTopicsToDelete);
        }
        catch (DeleteTopicsException ex)
        {
            var realErrors = ex.Results.Where(r => r.Error.Code != ErrorCode.UnknownTopicOrPart).ToList();
            if (realErrors.Count > 0)
            {
                var errors = string.Join("; ", realErrors.Select(e => $"{e.Topic}: {e.Error.Reason}"));
                _logger.LogError("Failed to purge topics. Errors: {Errors}", errors);
                throw new Exception($"Failed to purge Kafka topics. {errors}");
            }

            unknownTopics = ex.Results.Where(r => r.Error.Code == ErrorCode.UnknownTopicOrPart).Select(r => r.Topic).ToList();
        }
        
        userTopicsToDelete = userTopicsToDelete.Where(t => !unknownTopics.Contains(t)).ToList();
        await WaitForTopicsDeletionAsync(adminClient, userTopicsToDelete, TopicDeletionTimeout, ct);
        _logger.LogInformation("Existing user topics purged successfully.");
    }

    /// <summary>
    /// Uses native Kafka metadata requests to verify topics are fully removed.
    /// </summary>
    private async Task WaitForTopicsDeletionAsync(IAdminClient adminClient, IReadOnlyList<string> topicsToDelete, TimeSpan timeout, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            ct.ThrowIfCancellationRequested();

            var existingTopics = await GetAllTopicNamesAsync(adminClient, ct); 
            var anyTopicStillExists = topicsToDelete.Any(existingTopics.Contains);

            if (!anyTopicStillExists)
            {
                return; // All topics have been deleted
            }

            await Task.Delay(200, ct); // Short delay, to prevent tight looping while waiting for Kafka to process deletions
        }

        throw new TimeoutException($"Kafka topics were not fully deleted within the {timeout.TotalSeconds}s timeout period.");
    }

    /// <summary>
    /// Seeds configured schemas into the Kafka Schema Registry
    /// </summary>
    private async Task SeedSchemasAsync(CancellationToken cancellationToken)
    {
        if (_kafkaConfig.RegistryServer is null || _kafkaConfig.SchemasToSeed is null || _kafkaConfig.SchemasToSeed.Count == 0)
            return;

        _logger.LogInformation("Seeding {Count} schemas to the Schema Registry...", _kafkaConfig.SchemasToSeed.Count);

        var registryConfig = new SchemaRegistryConfig { Url = _kafkaConfig.RegistryServer };
        using var registryClient = new CachedSchemaRegistryClient(registryConfig);

        foreach (var schemaConfig in _kafkaConfig.SchemasToSeed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var schema = new Schema(schemaConfig.Schema, schemaConfig.SchemaType);
            
            // This registers the schema and returns its ID, without producing any Kafka messages
            var schemaId = await registryClient.RegisterSchemaAsync(schemaConfig.SubjectName, schema);
            
            _logger.LogInformation("Successfully seeded schema for subject '{Subject}' with ID {Id}.", schemaConfig.SubjectName, schemaId);
        }
    }

    private async Task PurgeSchemaRegistryAsync(CancellationToken cancellationToken)
    {
        if (_kafkaConfig.RegistryServer is null)
            return;

        _logger.LogInformation("Purging Schema Registry state via REST API...");

        var registryConfig = new SchemaRegistryConfig { Url = _kafkaConfig.RegistryServer };
        
        using var registryClient = new CachedSchemaRegistryClient(registryConfig);
        
        using var httpClient = new HttpClient { BaseAddress = new Uri(_kafkaConfig.RegistryServer) };

        try
        {
            // 1. Get all registered subjects
            var subjects = await registryClient.GetAllSubjectsAsync();

            foreach (var subject in subjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // 2. Perform a "Soft Delete" via the REST API
                var escapeSubject = Uri.EscapeDataString(subject);
                var softDeleteResponse = await httpClient.DeleteAsync($"/subjects/{escapeSubject}", cancellationToken);
                
                // 3. Perform a "Hard Delete" to completely wipe it from the registry
                if (softDeleteResponse.IsSuccessStatusCode)
                {
                    await httpClient.DeleteAsync($"/subjects/{escapeSubject}?permanent=true", cancellationToken);
                }
            }
            
            _logger.LogInformation("Schema Registry purged successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to purge schema registry. It might already be empty.");
        }
    }
}