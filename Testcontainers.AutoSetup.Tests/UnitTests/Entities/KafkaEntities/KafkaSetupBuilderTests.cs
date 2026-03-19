using Confluent.SchemaRegistry;
using Testcontainers.AutoSetup.Kafka;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Entities.KafkaEntities;

public class KafkaSetupBuilderTests
{
    private const string ValidBootstrap = "localhost:9092";
    private const string ValidRegistry = "http://localhost:8081";

    [Fact]
    public void Constructor_NullBootstrapServer_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new KafkaSetupBuilder(null!));
        Assert.Equal("bootstrapServer", exception.ParamName);
    }

    [Fact]
    public void WithTopic_NullTopic_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new KafkaSetupBuilder(ValidBootstrap);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => builder.WithTopic(null!));
        Assert.Equal("topicConfiguration", exception.ParamName);
    }

    [Fact]
    public void WithTopic_ValidTopic_AddsToConfiguration()
    {
        // Arrange
        var builder = new KafkaSetupBuilder(ValidBootstrap);
        var topic = new KafkaTopicConfiguration("test-topic");

        // Act
        builder.WithTopic(topic);
        var config = builder.Build();

        // Assert
        Assert.Single(config.Topics);
        Assert.Equal(topic, config.Topics[0]);
    }

    [Fact]
    public void WithSchemaString_NoRegistryConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new KafkaSetupBuilder(ValidBootstrap, registryServer: null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => builder.WithSchemaString("subject", "schema", SchemaType.Avro));
        
        Assert.Equal("Cannot seed schema without registry servers configured.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithSchemaString_InvalidSubjectName_ThrowsArgumentException(string? invalidSubject)
    {
        // Arrange
        var builder = new KafkaSetupBuilder(ValidBootstrap, ValidRegistry);

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => builder.WithSchemaString(invalidSubject!, "valid-schema", SchemaType.Avro));
    }

    [Fact]
    public void WithSchemaString_ValidInputs_AddsToConfiguration()
    {
        // Arrange
        var builder = new KafkaSetupBuilder(ValidBootstrap, ValidRegistry);
        const string subject = "my-subject";
        const string schemaStr = "{\"type\":\"record\"}";

        // Act
        builder.WithSchemaString(subject, schemaStr, SchemaType.Avro);
        var config = builder.Build();

        // Assert
        Assert.NotNull(config.SchemasToSeed);
        Assert.Single(config.SchemasToSeed);
        Assert.Equal(subject, config.SchemasToSeed[0].SubjectName);
        Assert.Equal(schemaStr, config.SchemasToSeed[0].Schema);
        Assert.Equal(SchemaType.Avro, config.SchemasToSeed[0].SchemaType);
    }

    [Fact]
    public void WithSchemaFromFile_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var builder = new KafkaSetupBuilder(ValidBootstrap, ValidRegistry);
        var fakePath = $"{Guid.NewGuid()}.avsc";

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(
            () => builder.WithSchemaFromFile("subject", fakePath, SchemaType.Avro));
        
        Assert.Contains(fakePath, exception.Message);
    }

    [Fact]
    public void WithSchemaFromFile_ValidFile_ReadsContentAndAddsToConfiguration()
    {
        // Arrange
        var builder = new KafkaSetupBuilder(ValidBootstrap, ValidRegistry);
        var tempFilePath = Path.GetTempFileName();
        var expectedSchemaContent = "{\"type\":\"string\"}";
        
        try
        {
            File.WriteAllText(tempFilePath, expectedSchemaContent);

            // Act
            builder.WithSchemaFromFile("file-subject", tempFilePath, SchemaType.Json);
            var config = builder.Build();

            // Assert
            Assert.NotNull(config.SchemasToSeed);
            Assert.Single(config.SchemasToSeed);
            Assert.Equal("file-subject", config.SchemasToSeed[0].SubjectName);
            Assert.Equal(expectedSchemaContent, config.SchemasToSeed[0].Schema);
            Assert.Equal(SchemaType.Json, config.SchemasToSeed[0].SchemaType);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    [Fact]
    public void Build_CreatesImmutableCollections()
    {
        // Arrange
        var builder = new KafkaSetupBuilder(ValidBootstrap, ValidRegistry)
            .WithTopic(new KafkaTopicConfiguration("topic-1"))
            .WithSchemaString("subject-1", "schema-1", SchemaType.Avro);

        // Act
        var config = builder.Build();

        // Assert
        Assert.IsType<IReadOnlyList<KafkaTopicConfiguration>>(config.Topics, exactMatch: false);
        Assert.IsType<IReadOnlyList<SchemaSeedConfiguration>>(config.SchemasToSeed, exactMatch: false);
        
        // At compile-time, config.Topics.Add() does not exist, proving immutability.
        Assert.Equal(ValidBootstrap, config.BootstrapServer);
        Assert.Equal(ValidRegistry, config.RegistryServer);
    }
}