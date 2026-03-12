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
        // Containers setup and seeding are done within the GlobalTestSetup
        // Arrange
        const string initialMessage = "Initial message";
        var bootstrapAddress = Setup.KafkaContainerFromSpecificBuilder.GetBootstrapAddress();
        var producer = new ProducerBuilder<Null, string>(
            new ProducerConfig { BootstrapServers = bootstrapAddress }).Build();
        var consumer = new ConsumerBuilder<Null, string>(
            new ConsumerConfig
            {
                BootstrapServers = bootstrapAddress,
                GroupId = "test-group",
                AutoOffsetReset = AutoOffsetReset.Latest
            }
        ).Build();
        var topic = Setup.KafkaContainer_FromSpecificBuilder_SetupConfig!.Topics[0].Name;

        // Act & Assert
        // Assure that the initial message is sent
        consumer.Subscribe([topic]);
        consumer.Consume(TimeSpan.FromMilliseconds(200));
        await producer.ProduceAsync(topic, new Message<Null, string> { Value = initialMessage });
        var message = consumer.Consume(TimeSpan.FromSeconds(2));
        Assert.NotNull(message);
        Assert.Equal(initialMessage, message.Message.Value);

        // Reset the env
        await Setup.ResetEnvironmentAsync(this.GetType());

        var messagesAfterReset = consumer.Consume(TimeSpan.FromSeconds(1));
        Assert.Null(messagesAfterReset);
    }
}
