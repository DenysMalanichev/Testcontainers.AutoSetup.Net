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
        Assert.Equal(1, config.ReplicationFactor);
    }

    [Fact]
    public void Constructor_WithAllArguments_SetsExplicitProperties()
    {
        // Act
        var config = new KafkaTopicConfiguration("my-topic", 3, 2);

        // Assert
        Assert.Equal("my-topic", config.Name);
        Assert.Equal(3, config.Partitions);
        Assert.Equal(2, config.ReplicationFactor);
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
        var config1 = new KafkaTopicConfiguration("my-topic", 2, 2);
        var config2 = new KafkaTopicConfiguration("my-topic", 2, 2);

        // Act & Assert
        Assert.Equal(config1, config2);
        Assert.True(config1 == config2);
    }
}

