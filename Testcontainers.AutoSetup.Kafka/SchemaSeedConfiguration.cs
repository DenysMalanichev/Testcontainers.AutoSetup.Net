using Confluent.SchemaRegistry;

namespace Testcontainers.AutoSetup.Kafka;

/// <summary>
/// A Kafka Schema registry configuration
/// </summary>
public record SchemaSeedConfiguration
{
    /// <summary>
    /// Subject name, whether it is rhe name of the topic or record.
    /// All three strategies (TopicNameStrategy, RecordNameStrategy, TopicRecordNameStrategy) aare supported 
    /// </summary>
    public string SubjectName { get; init; }

    /// <summary>
    /// A string representing the schema itself
    /// </summary>
    public string Schema { get; init; } 

    /// <summary>
    /// Schema Registry supported type
    /// </summary>
    public SchemaType SchemaType { get; init; } = SchemaType.Avro;

    public SchemaSeedConfiguration(string subjectName, string schema, SchemaType schemaType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectName);  
  
        SubjectName = subjectName;
        Schema = schema;
        SchemaType = schemaType;
    }
}
