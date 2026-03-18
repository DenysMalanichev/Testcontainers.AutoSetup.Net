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
    internal IList<Message<byte[], byte[]>>? MessagesToSeed { get; private set; } = null;

    /// <summary>
    /// A list of custom seed actions. 
    /// Param 1: Broker URL. Param 2: Schema Registry URL (can be null)
    /// </summary>
    internal List<Func<string, string?, Task>>? CustomAsyncSeedActions { get; private set; } = null;

    public KafkaTopicConfiguration(string name, int partitions = 1)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Partitions = partitions;
    }

    /// <summary>
    /// Adds a message to the list of messages to seed the topic with. The key and value are provided as strings and will be encoded as UTF-8 bytes.
    /// </summary>
    public KafkaTopicConfiguration WithSeedMessage(string? key, string? value,
        IDictionary<string, string?>? headers = null)
    {
        var keyBytes = key != null ? Encoding.UTF8.GetBytes(key) : null;
        var valueBytes = value != null ? Encoding.UTF8.GetBytes(value) : null;

        Dictionary<string, byte[]?>? headersByteArrays = null;

        if (headers != null && headers.Count > 0)
        {
            headersByteArrays = new Dictionary<string, byte[]?>(headers.Count);
            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header.Key))
                {
                    throw new ArgumentException("Header keys cannot be null, empty, or whitespace.", nameof(headers));
                }

                var headerValueBytes = header.Value != null ? Encoding.UTF8.GetBytes(header.Value) : null;
                headersByteArrays.Add(header.Key, headerValueBytes);
            }
        }

        return WithSeedMessage(keyBytes, valueBytes, headersByteArrays);
    }

    /// <summary>
    /// Adds a message to the list of messages to seed the topic with. The key and value are provided as byte arrays.
    /// </summary>
    public KafkaTopicConfiguration WithSeedMessage(byte[]? key, byte[]? value,
        IDictionary<string, byte[]?>? headers = null)
    {
        MessagesToSeed ??= [];

        var msg = new Message<byte[], byte[]>
        {
            Key = key!,
            Value = value!
        };

        if (headers != null && headers.Count > 0)
        {
            msg.Headers = new Headers();
            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header.Key))
                {
                    throw new ArgumentException("Header keys cannot be null, empty, or whitespace.", nameof(headers));
                }

                msg.Headers.Add(header.Key, header.Value);
            }
        }

        MessagesToSeed.Add(msg);
        return this;
    }

    /// <summary>
    /// Allows the consumer to provide a custom asynchronous action to seed strongly typed 
    /// messages (Avro, Protobuf, JSON Schema) using their own serializers.
    /// </summary>
    /// <param name="seedAction">A func that provides the Bootstrap Servers URL and Schema Registry URL</param>
    public KafkaTopicConfiguration WithCustomSeeder(Func<string, string?, Task> seedAction)
    {
        ArgumentNullException.ThrowIfNull(seedAction);
        CustomAsyncSeedActions ??= [];

        CustomAsyncSeedActions.Add(seedAction);
        return this;
    }
}
