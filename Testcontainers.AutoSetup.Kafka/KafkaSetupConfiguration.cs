namespace Testcontainers.AutoSetup.Kafka;

/// <summary>
/// Represents the overall configuration for setting up a Kafka instance in a Testcontainers environment, 
/// including connection details and topic configurations.
/// </summary>
public record KafkaSetupConfiguration
{
    /// <summary>
    /// The bootstrap server address for the Kafka cluster (e.g., "localhost:9092").
    /// This is required to connect to the cluster and perform administrative operations such as creating and deleting topics.
    /// </summary>
    public string BootstrapServer { get; init; }

    /// <summary>
    /// The list of topics to be created in the Kafka cluster.
    /// </summary>
    public IReadOnlyList<KafkaTopicConfiguration> Topics { get; init; }

    public KafkaSetupConfiguration(string bootstrapServers, IReadOnlyList<KafkaTopicConfiguration> topics)
    {
        BootstrapServer = bootstrapServers ?? throw new ArgumentNullException(nameof(bootstrapServers));
        Topics = topics ?? throw new ArgumentNullException(nameof(topics));
    }
}