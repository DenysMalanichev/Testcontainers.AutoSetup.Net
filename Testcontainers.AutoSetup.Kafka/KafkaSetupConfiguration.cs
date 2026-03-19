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
    /// Registry server URL. May be null, if no registry used.
    /// </summary>
    public string? RegistryServer { get; init; }

    /// <summary>
    /// The list of topics to be created in the Kafka cluster.
    /// </summary>
    public IReadOnlyList<KafkaTopicConfiguration> Topics { get; init; }

    /// <summary>
    /// Predefined schemas that would be seeded into the Schema Registry
    /// </summary>
    public IReadOnlyList<SchemaSeedConfiguration>? SchemasToSeed { get; init; }

    public KafkaSetupConfiguration(
        string bootstrapServer, 
        string? registryServer, 
        IReadOnlyList<KafkaTopicConfiguration> topics, 
        IReadOnlyList<SchemaSeedConfiguration>? schemasToSeed = null)
    {
        BootstrapServer = bootstrapServer ?? throw new ArgumentNullException(nameof(bootstrapServer));
        RegistryServer = registryServer;
        Topics = topics ?? [];
        SchemasToSeed = schemasToSeed;
    }
}