using DotNet.Testcontainers.Containers;
using MongoDB.Driver;
using Testcontainers.AutoSetup.Core.Attributes;
using Testcontainers.AutoSetup.Tests.IntegrationTests.TestCollections;
using Xunit.Abstractions;
using Confluent.Kafka;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.KafkaTests;

[DbReset]
[Trait("Category", "Integration")]
[Collection(nameof(ParallelIntegrationTestsCollection))]
public class KafkaSeederTests : IntegrationTestsBase
{
    private readonly ITestOutputHelper _output;

    public KafkaSeederTests(ContainersFixture fixture, ITestOutputHelper outputHelper)
         : base(fixture)
    {
        _output = outputHelper;
    }

    [Fact]
    public async Task KafkaSeeder_CreatesTopicsOnInitialization()
    {
        // Containers setup and seeding are done within the GlobalTestSetup
        // Arrange
        var adminClient = new AdminClientBuilder(
            new AdminClientConfig
            { BootstrapServers = Setup.KafkaContainerFromSpecificBuilder.GetBootstrapAddress() }
        ).Build();
        var expectedTopics = Setup.KafkaContainer_FromSpecificBuilder_SetupConfig!.Topics.Select(t => t.Name).ToList();

        // Act
        var metadata = await Task.Run(() => adminClient.GetMetadata(TimeSpan.FromSeconds(5)));
        var existingTopics = metadata.Topics.Select(t => t.Topic).ToList();

        // Assert
        Assert.NotNull(Setup.KafkaContainerFromSpecificBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.KafkaContainerFromSpecificBuilder.State);

        foreach (var expectedTopic in expectedTopics)
        {
            Assert.Contains(expectedTopic, existingTopics);
        }
    }

    [Fact]
    public async Task KafkaSeeder_DeletesRemovedTopic_IfNotListedInConfig()
    {
        // Containers setup and seeding are done within the GlobalTestSetup
        // Arrange
        var adminClient = new AdminClientBuilder(
            new AdminClientConfig
            { BootstrapServers = Setup.KafkaContainerFromSpecificBuilder.GetBootstrapAddress() }
        ).Build();
        var topicToDelete = "topic-to-delete";

        // Create an extra topic that is not in the configuration
        await adminClient.CreateTopicsAsync([new() { Name = topicToDelete }]);

        // Act
        await Setup.ResetEnvironmentAsync(this.GetType());

        var metadata = await Task.Run(() => adminClient.GetMetadata(TimeSpan.FromSeconds(5)));
        var existingTopics = metadata.Topics.Select(t => t.Topic).ToList();

        // Assert
        Assert.DoesNotContain(topicToDelete, existingTopics);
    }

    [Fact]
    public async Task KafkaSeeder_DeletesMessageFromTopic_AfterRecreation()
    {
        // Arrange
        const string initialMessage = "Initial message";
        var bootstrapAddress = Setup.KafkaContainerFromSpecificBuilder.GetBootstrapAddress();
        var topic = Setup.KafkaContainer_FromSpecificBuilder_SetupConfig!.Topics[0].Name;

        using var producer = new ProducerBuilder<Null, string>(
            new ProducerConfig { BootstrapServers = bootstrapAddress }).Build();

        using var consumer = new ConsumerBuilder<Ignore, string>(
            new ConsumerConfig
            {
                BootstrapServers = bootstrapAddress,
                GroupId = Guid.NewGuid().ToString(),
                EnableAutoCommit = false
            }
        ).Build();

        var topicPartition = new TopicPartition(topic, new Partition(0));

        // Ask the broker exactly where the end of the topic is right now.
        var watermarks = consumer.QueryWatermarkOffsets(topicPartition, TimeSpan.FromSeconds(5));

        consumer.Assign(new TopicPartitionOffset(topicPartition, watermarks.High));

        // Act & Assert 1: Send the message
        await producer.ProduceAsync(topic, new Message<Null, string> { Value = initialMessage });

        var message = consumer.Consume(TimeSpan.FromSeconds(3));
        Assert.NotNull(message);
        Assert.Equal(initialMessage, message.Message.Value);

        // Reset the env (Deletes, recreates, and re-seeds the topic)
        await Setup.ResetEnvironmentAsync(this.GetType());

        // Act & Assert 2: Verify the topic does NOT contain our test message
        // We MUST create a new consumer so it fetches fresh metadata for the newly created topic UUID
        using var consumerAfterReset = new ConsumerBuilder<Ignore, string>(
            new ConsumerConfig
            {
                BootstrapServers = bootstrapAddress,
                GroupId = Guid.NewGuid().ToString()
            }
        ).Build();

        consumerAfterReset.Assign(new TopicPartitionOffset(topicPartition, Offset.Beginning));

        // Loop through all the newly seeded messages
        while (true)
        {
            var msg = consumerAfterReset.Consume(TimeSpan.FromSeconds(1));
            
            if (msg is null) 
            {
                break;
            }

            Assert.NotEqual(initialMessage, msg.Message.Value);
        }
    }
}
