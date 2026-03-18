using DotNet.Testcontainers.Containers;
using MongoDB.Driver;
using Testcontainers.AutoSetup.Core.Attributes;
using Testcontainers.AutoSetup.Tests.IntegrationTests.TestCollections;
using Xunit.Abstractions;
using Confluent.Kafka;
using Testcontainers.AutoSetup.Tests.UnitTests.Extensions;

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
            { BootstrapServers = Setup.KafkaTestEnvironment.KafkaContainer.GetBootstrapAddress() }
        ).Build();
        var expectedTopics = Setup.KafkaContainer_FromSpecificBuilder_SetupConfig!.Topics.Select(t => t.Name).ToList();

        // Act
        var metadata = await Task.Run(() => adminClient.GetMetadata(TimeSpan.FromSeconds(5)));
        var existingTopics = metadata.Topics.Select(t => t.Topic).ToList();

        // Assert
        Assert.NotNull(Setup.KafkaTestEnvironment);
        Assert.Equal(TestcontainersStates.Running, Setup.KafkaTestEnvironment.KafkaContainer.State);

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
            { BootstrapServers = Setup.KafkaTestEnvironment.KafkaContainer.GetBootstrapAddress() }
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
        var bootstrapAddress = Setup.KafkaTestEnvironment.KafkaContainer.GetBootstrapAddress();
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

    [Fact]
    public async Task KafkaTestEnvironment_CreatesKafkaUIContainer_IfConfigured()
    {
        // Containers setup and seeding are done within the GlobalTestSetup
        // Arrange
        var kafkaUiContainer = Setup.KafkaTestEnvironment.KafkaUiContainer;
        var kafkaNetwork = Setup.KafkaTestEnvironment.KafkaNetwork;

        // Assert
        Assert.NotNull(kafkaUiContainer);
        Assert.Equal(TestcontainersStates.Running, kafkaUiContainer.State);  
        Assert.NotNull(kafkaNetwork);  
        var networks = kafkaUiContainer.GetConfiguration().Networks.Select(n => n.Name);
        Assert.Contains(kafkaNetwork.Name, networks);
    }

    [Fact]
    public async Task KafkaTestEnvironment_CreatesKafkaNetwork_IfConfigured()
    {
        // Containers setup and seeding are done within the GlobalTestSetup
        Assert.NotNull(Setup.KafkaTestEnvironment.KafkaNetwork);
    }

    [Fact]
    public async Task KafkaTestEnvironment_CreatesKafkaSchemaRegistryContainer_IfConfigured()
    {
        // Containers setup and seeding are done within the GlobalTestSetup 
        // Arrange
        var schemaRegistryContainer = Setup.KafkaTestEnvironment.SchemaRegistryContainer;
        var kafkaNetwork = Setup.KafkaTestEnvironment.KafkaNetwork;

        // Assert
        Assert.NotNull(schemaRegistryContainer);
        Assert.Equal(TestcontainersStates.Running, schemaRegistryContainer.State);  
        Assert.NotNull(kafkaNetwork);  
        var networks = schemaRegistryContainer.GetConfiguration().Networks.Select(n => n.Name);
        Assert.Contains(kafkaNetwork.Name, networks);
    }
}
