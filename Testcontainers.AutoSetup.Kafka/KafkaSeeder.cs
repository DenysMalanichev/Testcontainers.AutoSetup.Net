using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using DotNet.Testcontainers;
using Microsoft.Extensions.Logging;
using Testcontainers.AutoSetup.Core.Abstractions;

namespace Testcontainers.AutoSetup.Kafka;

// TODO add a specific interface for kafka seeder(?)
/// <summary>
/// Implements the IInstanceStrategy interface to provide Kafka-specific seeding logic.
/// </summary>
public class KafkaSeeder : IInstanceStrategy
{
    private readonly KafkaSetupConfiguration _kafkaSetup;
    private readonly ILogger _logger;

    public KafkaSeeder(KafkaSetupConfiguration kafkaSetup, ILogger logger)
    {
        _kafkaSetup = kafkaSetup ?? throw new ArgumentNullException(nameof(kafkaSetup));
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

        // Use the testable helper method to get ALL current topic names
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

        await CreateTopicsAsync(adminClient);
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
        _logger.LogInformation("Building Kafka AdminClient with bootstrap server: {BootstrapServer}", _kafkaSetup.BootstrapServer);
        var config = new AdminClientConfig { BootstrapServers = _kafkaSetup.BootstrapServer };
        return new AdminClientBuilder(config).Build();
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
        _logger.LogInformation("Initializing Kafka topics for bootstrap server: {BootstrapServer}", _kafkaSetup.BootstrapServer);
        
        var topicSpecifications = _kafkaSetup.Topics.Select(topic => new TopicSpecification
        {
            Name = topic.Name,
            NumPartitions = topic.Partitions,
            ReplicationFactor = topic.ReplicationFactor
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
            // We can gracefully handle topics that already exist
            // TODO recreate existing topics(?)
            var realErrors = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists).ToList();
            
            if (realErrors.Count > 0)
            {
                _logger.LogError("Failed to create Kafka topics. Errors: {ErrorMessages}", string.Join("; ", realErrors.Select(e => $"{e.Topic}: {e.Error.Reason}")));
                throw new Exception("Failed to create Kafka topics.");
            }
        }

        _logger.LogInformation("Kafka topics initialization complete.");
    }

    private async Task DeleteTopicsAsync(IReadOnlyList<string> userTopicsToDelete, IAdminClient adminClient, CancellationToken ct)
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
        await WaitForTopicsDeletionAsync(adminClient, userTopicsToDelete, TimeSpan.FromSeconds(15), ct);
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

            // Use the testable helper method here as well
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
}