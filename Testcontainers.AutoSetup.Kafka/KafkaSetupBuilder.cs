using Confluent.SchemaRegistry;

namespace Testcontainers.AutoSetup.Kafka;
/// <summary>
/// A fluent builder for creating an immutable <see cref="KafkaSetupConfiguration"/>.
/// </summary>
public class KafkaSetupBuilder
{
    private readonly string _bootstrapServer;
    private readonly string? _registryServer;
    
    private readonly List<KafkaTopicConfiguration> _topics = [];
    private List<SchemaSeedConfiguration>? _schemasToSeed;

    public KafkaSetupBuilder(string bootstrapServer, string? registryServer = null)
    {
        _bootstrapServer = bootstrapServer ?? throw new ArgumentNullException(nameof(bootstrapServer));
        _registryServer = registryServer;
    }

    /// <summary>
    /// Adds a provided topic's configuration into a setup for seeding.
    /// </summary>
    /// <param name="topicConfiguration">A configuration for a topic to be added.</param>
    /// <returns></returns>
    public KafkaSetupBuilder WithTopic(KafkaTopicConfiguration topicConfiguration)
    {
        ArgumentNullException.ThrowIfNull(topicConfiguration);
        _topics.Add(topicConfiguration);
        return this;
    }

    /// <summary>
    /// Seeds a schema into the Schema Registry using a raw schema string.
    /// </summary>
    /// <param name="subjectName">The subject name (e.g., 'my-topic-value' or 'com.company.User').</param>
    /// <param name="schemaString">The raw string representation of the schema.</param>
    /// <param name="schemaType">The type of the schema (Avro, Protobuf, or Json).</param>
    public KafkaSetupBuilder WithSchemaString(string subjectName, string schema, SchemaType schemaType)
    {
        if (_registryServer is null)
            throw new InvalidOperationException("Cannot seed schema without registry servers configured.");

        ArgumentException.ThrowIfNullOrWhiteSpace(subjectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        _schemasToSeed ??= [];
        _schemasToSeed.Add(new SchemaSeedConfiguration(subjectName, schema, schemaType));

        return this;
    }

    
    /// <summary>
    /// Reads a schema from a local file and seeds it into the Schema Registry.
    /// </summary>
    /// <param name="subjectName">The subject name (e.g., 'my-topic-value' or 'com.company.User').</param>
    /// <param name="filePath">The relative or absolute path to the schema file (e.g., '.avsc', '.proto').</param>
    /// <param name="schemaType">The type of the schema (Avro, Protobuf, or Json).</param>
    public KafkaSetupBuilder WithSchemaFromFile(string subjectName, string filePath, SchemaType schemaType)
    {
        if (_registryServer is null)
            throw new InvalidOperationException("Cannot seed schema without registry servers configured.");

        ArgumentException.ThrowIfNullOrWhiteSpace(subjectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Schema file could not be found at path: {filePath}", filePath);
        }

        var schemaString = File.ReadAllText(filePath);
        return WithSchemaString(subjectName, schemaString, schemaType);
    }

    /// <summary>
    /// Constructs the final, immutable configuration.
    /// </summary>
    public KafkaSetupConfiguration Build()
    {
        return new KafkaSetupConfiguration(
            _bootstrapServer,
            _registryServer,
            _topics.AsReadOnly(),
            _schemasToSeed?.AsReadOnly()
        );
    }
}