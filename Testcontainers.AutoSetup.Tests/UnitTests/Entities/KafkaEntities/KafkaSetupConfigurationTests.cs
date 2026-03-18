using Testcontainers.AutoSetup.Kafka;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Entities.KafkaEntities;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class KafkaSetupConfigurationTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        // Arrange
        var kafkaServer = "localhost:9092";
        var registryServer = "localhost:8081";
        var topics = new List<KafkaTopicConfiguration>
            {
                new("test-topic")
            };

        // Act
        var config = new KafkaSetupConfiguration(kafkaServer, registryServer, topics);

        // Assert
        Assert.Equal(kafkaServer, config.BootstrapServer);
        Assert.Equal(registryServer, config.RegistryServer);
        Assert.Same(topics, config.Topics);
    }

    [Fact]
    public void Constructor_WithNullBootstrapServer_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>("bootstrapServers", () => new KafkaSetupConfiguration(null!, "localhost:8081", []));
        Assert.Contains("bootstrapServers", ex.Message);
    }

    [Fact]
    public void Constructor_WithNullTopics_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>("topics", () => new KafkaSetupConfiguration("localhost:9092", "localhost:8081", null!));
        Assert.Contains("topics", ex.Message);
    }

    [Fact]
    public void RecordEquality_IdenticalConfigurations_AreEqual()
    {
        // Arrange
        var topics = new List<KafkaTopicConfiguration>();
        var config1 = new KafkaSetupConfiguration("localhost:9092", "localhost:8081", topics);
        var config2 = new KafkaSetupConfiguration("localhost:9092", "localhost:8081", topics);

        // Act & Assert
        Assert.Equal(config1, config2);
        Assert.True(config1 == config2);
    }
}
