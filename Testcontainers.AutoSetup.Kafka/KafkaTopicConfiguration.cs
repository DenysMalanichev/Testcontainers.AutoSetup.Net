namespace Testcontainers.AutoSetup.Kafka;

/// <summary>
/// Represents the configuration for setting up a Kafka topic in a Testcontainers environment.
/// </summary>
public record KafkaTopicConfiguration
{
    /// <summary>
    /// The name of the Kafka topic to be created. This is required and must be unique within the cluster.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The number of partitions for the topic. This determines how the topic's data is distributed 
    /// across the cluster and can impact performance and scalability.
    /// Default is 1.
    /// </summary>
    public int Partitions { get; init; } = 1;

    /// <summary>
    /// The replication factor for the topic. This determines how many copies of the topic's 
    /// data are maintained across the cluster for fault tolerance.
    /// </summary>
    public short ReplicationFactor { get; init; } = 1;

    // TODO add optional message seeding (logic in the seeder, setup here)

    public KafkaTopicConfiguration(string name, int partitions = 1, short replicationFactor = 1)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Partitions = partitions;
        ReplicationFactor = replicationFactor;
    }
}
