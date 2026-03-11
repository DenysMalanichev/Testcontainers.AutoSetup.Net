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
        var server = "localhost:9092";
        var topics = new List<KafkaTopicConfiguration>
            {
                new("test-topic")
            };

        // Act
        var config = new KafkaSetupConfiguration(server, topics);

        // Assert
        Assert.Equal(server, config.BootstrapServer);
        Assert.Same(topics, config.Topics);
    }

    [Fact]
    public void Constructor_WithNullBootstrapServer_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>("bootstrapServers", () => new KafkaSetupConfiguration(null!, []));
        Assert.Contains("bootstrapServers", ex.Message);
    }

    [Fact]
    public void Constructor_WithNullTopics_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>("topics", () => new KafkaSetupConfiguration("localhost:9092", null!));
        Assert.Contains("topics", ex.Message);
    }

    [Fact]
    public void RecordEquality_IdenticalConfigurations_AreEqual()
    {
        // Arrange
        var topics = new List<KafkaTopicConfiguration>();
        var config1 = new KafkaSetupConfiguration("localhost:9092", topics);
        var config2 = new KafkaSetupConfiguration("localhost:9092", topics);

        // Act & Assert
        Assert.Equal(config1, config2);
        Assert.True(config1 == config2);
    }
}
