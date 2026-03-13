using System.Text;
using Confluent.Kafka;

namespace Testcontainers.AutoSetup.Kafka;

// TODO move this and other configuration in a separate folder and update a namespace
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
    /// The list of messages to seed the topic with.
    /// </summary>
    internal IList<Message<byte[], byte[]>>? MessagesToSeed { get; private set; } = null!;

    public KafkaTopicConfiguration(string name, int partitions = 1)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Partitions = partitions;
    }

    /// <summary>
    /// Adds a message to the list of messages to seed the topic with. The key and value are provided as strings and will be encoded as UTF-8 bytes.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>Same <see cref="KafkaTopicConfiguration"/> with added message</returns>
    public KafkaTopicConfiguration WithSeedMessage(string key, string value)
    {
        var keyBytes = key != null ? Encoding.UTF8.GetBytes(key) : null;
        var valueBytes = value != null ? Encoding.UTF8.GetBytes(value) : null;

        return WithSeedMessage(keyBytes, valueBytes);
    }

    /// <summary>
    /// Adds a message to the list of messages to seed the topic with. The key and value are provided as byte arrays.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>Same <see cref="KafkaTopicConfiguration"/> with added message</returns>
    public KafkaTopicConfiguration WithSeedMessage(byte[]? key, byte[]? value)
    {
        MessagesToSeed ??= [];
        MessagesToSeed.Add(new Message<byte[], byte[]>
        {
            Key = key!,
            Value = value!
        });
        return this;
    }
}
