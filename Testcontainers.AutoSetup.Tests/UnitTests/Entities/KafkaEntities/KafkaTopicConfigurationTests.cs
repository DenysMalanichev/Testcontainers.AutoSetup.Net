using System.Text;
using Testcontainers.AutoSetup.Kafka;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Entities.KafkaEntities;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class KafkaTopicConfigurationTests
{
    [Fact]
    public void Constructor_WithOnlyName_SetsDefaults()
    {
        // Act
        var config = new KafkaTopicConfiguration("my-topic");

        // Assert
        Assert.Equal("my-topic", config.Name);
        Assert.Equal(1, config.Partitions);
    }

    [Fact]
    public void Constructor_WithAllArguments_SetsExplicitProperties()
    {
        // Act
        var config = new KafkaTopicConfiguration("my-topic", 3);

        // Assert
        Assert.Equal("my-topic", config.Name);
        Assert.Equal(3, config.Partitions);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>("name", () => new KafkaTopicConfiguration(null!));
        Assert.Contains("name", ex.Message);
    }

    [Fact]
    public void RecordEquality_IdenticalConfigurations_AreEqual()
    {
        // Arrange
        var config1 = new KafkaTopicConfiguration("my-topic", 2);
        var config2 = new KafkaTopicConfiguration("my-topic", 2);

        // Act & Assert
        Assert.Equal(config1, config2);
        Assert.True(config1 == config2);
    }

    [Fact]
    public void WithSeedMessage_StringOverload_AllValidParameters_AddsMessageCorrectly()
    {
        // Arrange
        var config = new KafkaTopicConfiguration("my-topic");
        var headers = new Dictionary<string, string?> { { "correlation-id", "12345" } };

        // Act
        config.WithSeedMessage("my-key", "my-value", headers);

        // Assert
        Assert.NotNull(config.MessagesToSeed);
        var message = Assert.Single(config.MessagesToSeed);
        Assert.Equal(Encoding.UTF8.GetBytes("my-key"), message.Key);
        Assert.Equal(Encoding.UTF8.GetBytes("my-value"), message.Value);
        
        Assert.NotNull(message.Headers);
        var header = Assert.Single(message.Headers);
        Assert.Equal("correlation-id", header.Key);
        Assert.Equal(Encoding.UTF8.GetBytes("12345"), header.GetValueBytes());
    }

    [Fact]
    public void WithSeedMessage_StringOverload_NullKeyAndNullValue_AddsMessageWithNulls()
    {
        // Arrange
        var config = new KafkaTopicConfiguration("my-topic");

        // Act
        config.WithSeedMessage(key: null, value: (string?)null);

        // Assert
        Assert.NotNull(config.MessagesToSeed);
        var message = Assert.Single(config.MessagesToSeed);
        Assert.Null(message.Key);
        Assert.Null(message.Value);
        Assert.Null(message.Headers); // Headers should not be initialized if not provided
    }

    [Fact]
    public void WithSeedMessage_StringOverload_NullHeaderValue_AddsHeaderWithNullBytes()
    {
        // Arrange
        var config = new KafkaTopicConfiguration("my-topic");
        var headers = new Dictionary<string, string?> { { "empty-header", null } };

        // Act
        config.WithSeedMessage("key", "value", headers);

        // Assert
        Assert.NotNull(config.MessagesToSeed);
        var message = Assert.Single(config.MessagesToSeed);
        Assert.NotNull(message.Headers);
        
        var header = Assert.Single(message.Headers);
        Assert.Equal("empty-header", header.Key);
        Assert.Null(header.GetValueBytes());
    }

    [Fact]
    public void WithSeedMessage_ByteArrayOverload_AllValidParameters_AddsMessageCorrectly()
    {
        // Arrange
        var config = new KafkaTopicConfiguration("my-topic");
        var keyBytes = new byte[] { 1, 2, 3 };
        var valueBytes = new byte[] { 4, 5, 6 };
        var headerBytes = new byte[] { 7, 8, 9 };
        var headers = new Dictionary<string, byte[]?> { { "trace-id", headerBytes } };

        // Act
        config.WithSeedMessage(keyBytes, valueBytes, headers);

        // Assert
        Assert.NotNull(config.MessagesToSeed);
        var message = Assert.Single(config.MessagesToSeed!);
        Assert.Equal(keyBytes, message.Key);
        Assert.Equal(valueBytes, message.Value);
        
        Assert.NotNull(message.Headers);
        var header = Assert.Single(message.Headers);
        Assert.Equal("trace-id", header.Key);
        Assert.Equal(headerBytes, header.GetValueBytes());
    }

    [Fact]
    public void WithSeedMessage_ByteArrayOverload_NullValue_CreatesTombstoneMessage()
    {
        // Arrange
        var config = new KafkaTopicConfiguration("my-topic");
        var keyBytes = new byte[] { 1, 2, 3 };

        // Act
        config.WithSeedMessage(keyBytes, value: null);

        // Assert
        Assert.NotNull(config.MessagesToSeed);
        var message = Assert.Single(config.MessagesToSeed!);
        Assert.Equal(keyBytes, message.Key);
        Assert.Null(message.Value); // This verifies it successfully created a Tombstone
        Assert.Null(message.Headers);
    }

    [Fact]
    public void WithSeedMessage_ByteArrayOverload_Chaining_AddsMultipleMessages()
    {
        // Arrange
        var config = new KafkaTopicConfiguration("my-topic");

        // Act
        config.WithSeedMessage("key1", "val1")
              .WithSeedMessage("key2", "val2");

        // Assert
        Assert.NotNull(config.MessagesToSeed);
        Assert.Equal(2, config.MessagesToSeed.Count);
        Assert.Equal(Encoding.UTF8.GetBytes("key1"), config.MessagesToSeed[0].Key);
        Assert.Equal(Encoding.UTF8.GetBytes("key2"), config.MessagesToSeed[1].Key);
    }

    [Fact]
    public void WithSeedMessage_StringOverload_EmptyHeaderKey_ThrowsArgumentException()
    {
        // Arrange
        var config = new KafkaTopicConfiguration("my-topic");
        var headers = new Dictionary<string, string?> { { "", "some-value" } }; // Empty key

        // Act
        var exception = Assert.Throws<ArgumentException>(() => 
            config.WithSeedMessage("key", "value", headers));

        // Assert
        Assert.Contains("Header keys cannot be null, empty, or whitespace", exception.Message);
        Assert.Equal("headers", exception.ParamName);
    }

    [Fact]
    public void WithSeedMessage_StringOverload_WhitespaceHeaderKey_ThrowsArgumentException()
    {
        // Arrange
        var config = new KafkaTopicConfiguration("my-topic");
        var headers = new Dictionary<string, string?> { { "   ", "some-value" } }; // Whitespace key

        // Act
        var exception = Assert.Throws<ArgumentException>(() => 
            config.WithSeedMessage("key", "value", headers));

        // Assert
        Assert.Contains("Header keys cannot be null, empty, or whitespace", exception.Message);
        Assert.Equal("headers", exception.ParamName);
    }

    [Fact]
    public void WithSeedMessage_ByteArrayOverload_EmptyHeaderKey_ThrowsArgumentException()
    {
        // Arrange
        var config = new KafkaTopicConfiguration("my-topic");
        var headers = new Dictionary<string, byte[]?> { { "", new byte[] { 1, 2, 3 } } };

        // Act
        var exception = Assert.Throws<ArgumentException>(() => 
            config.WithSeedMessage(new byte[] { 1 }, new byte[] { 2 }, headers));

        // Assert
        Assert.Contains("Header keys cannot be null, empty, or whitespace", exception.Message);
        Assert.Equal("headers", exception.ParamName);
    }
}

