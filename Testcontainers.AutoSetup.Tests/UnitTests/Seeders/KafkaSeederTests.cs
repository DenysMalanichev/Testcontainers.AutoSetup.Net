using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Testcontainers.AutoSetup.Kafka;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Seeders;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class KafkaSeederTests
{
    private readonly NullLogger _logger;

    public KafkaSeederTests()
    {
        _logger = NullLogger.Instance; 
    }

    [Fact]
    public async Task InitializeGlobalAsync_ShouldDoNothing_AndCompleteSuccessfully()
    {
        // Arrange
        var mockAdminClient = new Mock<IAdminClient>();
        var mockProducer = new Mock<IProducer<byte[], byte[]>>();
        var config = new KafkaSetupConfiguration("localhost:9092", []);
        var seeder = new TestableKafkaSeeder(config, _logger, mockAdminClient.Object, mockProducer.Object);

        // Act
        var exception = await Record.ExceptionAsync(() => seeder.InitializeGlobalAsync());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task ResetAsync_WhenOnlyInternalTopicsExist_ShouldNotDeleteAnything_AndCreateTopics()
    {
        // Arrange
        var topicConfig = new KafkaTopicConfiguration("new-topic", 1);
        var config = new KafkaSetupConfiguration("localhost:9092", [topicConfig]);
        
        var mockAdminClient = new Mock<IAdminClient>();
        mockAdminClient.Setup(x => x.GetMetadata(It.IsAny<TimeSpan>())).Returns(new Metadata
        (
            brokers: [],
            topics: [], // Internal topics are not returned in GetMetadata()
            originatingBrokerId: 0,
            originatingBrokerName: "mock-broker"
        ));
        var mockProducer = new Mock<IProducer<byte[], byte[]>>();
        var seeder = new TestableKafkaSeeder(config, _logger, mockAdminClient.Object, mockProducer.Object)
        {
            // Simulate the broker reporting only internal topics
            MockGetAllTopicNames = () => new List<string> { "__consumer_offsets" },
            MockGetUserTopicsToDelete = (all) => new List<string>() // None of them are user topics
        };

        // Act
        await seeder.ResetAsync();

        // Assert
        mockAdminClient.Verify(x => x.DeleteTopicsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<DeleteTopicsOptions>()), Times.Never);
        mockAdminClient.Verify(x => x.CreateTopicsAsync(It.Is<IEnumerable<TopicSpecification>>(t => t.First().Name == "new-topic"), It.IsAny<CreateTopicsOptions>()), Times.Once);
    }

    [Fact]
    public async Task ResetAsync_WhenUserTopicsExist_ShouldDeleteThem_Wait_AndCreateNewTopics()
    {
        // Arrange
        var mockAdminClient = new Mock<IAdminClient>();
        var mockProducer = new Mock<IProducer<byte[], byte[]>>();
        
        // We do NOT mock GetMetadata or DescribeTopicsAsync here.
        // Moq can't mock the extension methods, and our Testable subclass 
        // bypasses them anyway using the delegates below!

        var config = new KafkaSetupConfiguration("localhost:9092", [new KafkaTopicConfiguration("desired-topic")]);
        
        var pollingQueue = new Queue<List<string>>();
        pollingQueue.Enqueue(new List<string> { "old-user-topic" }); // 1st check (pre-deletion)
        pollingQueue.Enqueue(new List<string> { "old-user-topic" }); // 2nd check (during wait loop)
        pollingQueue.Enqueue(new List<string>());                    // 3rd check (deletion finished)

        var seeder = new TestableKafkaSeeder(config, _logger, mockAdminClient.Object, mockProducer.Object)
        {
            // THIS is our clean abstraction. It perfectly fakes the broker responses.
            MockGetAllTopicNames = () => pollingQueue.Dequeue(),
            MockGetUserTopicsToDelete = (all) => all // Simulate that all reported topics are user topics
        };

        // Act
        await seeder.ResetAsync();

        // Assert
        mockAdminClient.Verify(x => x.DeleteTopicsAsync(
            It.Is<IEnumerable<string>>(t => t.Contains("old-user-topic")), 
            It.IsAny<DeleteTopicsOptions>()), 
            Times.Once);

        mockAdminClient.Verify(x => x.CreateTopicsAsync(
            It.Is<IEnumerable<TopicSpecification>>(t => t.First().Name == "desired-topic"), 
            It.IsAny<CreateTopicsOptions>()), 
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullSetup_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new KafkaSeeder(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_DefaultsToConsoleLogger_DoesNotThrow()
    {
        var config = new KafkaSetupConfiguration("localhost:9092", []);
        var seeder = new KafkaSeeder(config, null!);
        Assert.NotNull(seeder);
    }

    [Fact]
    public async Task InitializeGlobalAsync_DoesNothing_AndCompletes()
    {
        // Arrange
        var mockAdminClient = new Mock<IAdminClient>();
        var mockProducer = new Mock<IProducer<byte[], byte[]>>();
        var config = new KafkaSetupConfiguration("localhost:9092", []);
        var seeder = new TestableKafkaSeeder(config, _logger, mockAdminClient.Object, mockProducer.Object);
        
        // Act
        var exception = await Record.ExceptionAsync(() => seeder.InitializeGlobalAsync());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task ResetAsync_NoTopicsInConfig_ReturnsEarlyWithoutCreating()
    {
        // Arrange
        var mockAdminClient = new Mock<IAdminClient>();
        var mockProducer = new Mock<IProducer<byte[], byte[]>>();
        var config = new KafkaSetupConfiguration("localhost:9092", []); // Empty topics
        var seeder = new TestableKafkaSeeder(config, _logger, mockAdminClient.Object, mockProducer.Object);

        // Act
        await seeder.ResetAsync();

        // Assert
        mockAdminClient.Verify(x => x.CreateTopicsAsync(It.IsAny<IEnumerable<TopicSpecification>>(), It.IsAny<CreateTopicsOptions>()), Times.Never);
    }

    [Fact]
    public async Task ResetAsync_OnlyInternalTopicsExist_SkipsDeletion_CreatesNewTopics()
    {
        // Arrange
        var mockAdminClient = new Mock<IAdminClient>();
        var mockProducer = new Mock<IProducer<byte[], byte[]>>();
        var config = new KafkaSetupConfiguration("localhost:9092", [new KafkaTopicConfiguration("new-topic")]);
        var seeder = new TestableKafkaSeeder(config, _logger, mockAdminClient.Object, mockProducer.Object)
        {
            MockGetAllTopicNames = () => ["__consumer_offsets"],
            MockGetUserTopicsToDelete = _ => [] // None to delete
        };

        // Act
        await seeder.ResetAsync();

        // Assert
        mockAdminClient.Verify(x => x.DeleteTopicsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<DeleteTopicsOptions>()), Times.Never);
        mockAdminClient.Verify(x => x.CreateTopicsAsync(It.IsAny<IEnumerable<TopicSpecification>>(), It.IsAny<CreateTopicsOptions>()), Times.Once);
    }

    [Fact]
    public async Task ResetAsync_UserTopicsExist_DeletesThem_AndCreatesNewTopics()
    {
        // Arrange
        var mockAdminClient = new Mock<IAdminClient>();
        var mockProducer = new Mock<IProducer<byte[], byte[]>>();
        var config = new KafkaSetupConfiguration("localhost:9092", [new KafkaTopicConfiguration("desired-topic")]);
        var pollingQueue = new Queue<List<string>>(new[] { ["old-topic"], new List<string>() });
        
        var seeder = new TestableKafkaSeeder(config, _logger, mockAdminClient.Object, mockProducer.Object)
        {
            MockGetAllTopicNames = () => pollingQueue.Dequeue(),
            MockGetUserTopicsToDelete = all => all
        };

        // Act
        await seeder.ResetAsync();

        // Assert
        mockAdminClient.Verify(x => x.DeleteTopicsAsync(It.Is<IEnumerable<string>>(t => t.Contains("old-topic")), It.IsAny<DeleteTopicsOptions>()), Times.Once);
        mockAdminClient.Verify(x => x.CreateTopicsAsync(It.Is<IEnumerable<TopicSpecification>>(t => t.First().Name == "desired-topic"), It.IsAny<CreateTopicsOptions>()), Times.Once);
    }

    [Fact]
    public async Task ResetAsync_CreateTopicsThrowsTopicAlreadyExists_GracefullyIgnores()
    {
        // Arrange
        var mockAdminClient = new Mock<IAdminClient>();
        var mockProducer = new Mock<IProducer<byte[], byte[]>>();
        var config = new KafkaSetupConfiguration("localhost:9092", [new KafkaTopicConfiguration("existing-topic")]);
        var seeder = new TestableKafkaSeeder(config, _logger, mockAdminClient.Object, mockProducer.Object);

        var createException = new CreateTopicsException(
        [ 
            new CreateTopicReport { Topic = "existing-topic", Error = new Error(ErrorCode.TopicAlreadyExists) } 
        ]);

        mockAdminClient.Setup(x => x.CreateTopicsAsync(It.IsAny<IEnumerable<TopicSpecification>>(), It.IsAny<CreateTopicsOptions>()))
            .ThrowsAsync(createException);

        // Act
        var exception = await Record.ExceptionAsync(() => seeder.ResetAsync());

        // Assert
        Assert.Null(exception); // Should swallow the exception safely
    }

    [Fact]
    public async Task ResetAsync_CreateTopicsThrowsOtherError_ThrowsException()
    {
        // Arrange
        var mockAdminClient = new Mock<IAdminClient>();
        var mockProducer = new Mock<IProducer<byte[], byte[]>>();
        var config = new KafkaSetupConfiguration("localhost:9092", [new KafkaTopicConfiguration("bad-topic")]);
        var seeder = new TestableKafkaSeeder(config, _logger, mockAdminClient.Object, mockProducer.Object);

        var createException = new CreateTopicsException(
        [
            new CreateTopicReport { Topic = "bad-topic", Error = new Error(ErrorCode.BrokerNotAvailable) } 
        ]);

        mockAdminClient.Setup(x => x.CreateTopicsAsync(It.IsAny<IEnumerable<TopicSpecification>>(), It.IsAny<CreateTopicsOptions>()))
            .ThrowsAsync(createException);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => seeder.ResetAsync());
    }

    [Fact]
    public async Task ResetAsync_DeleteTopicsThrowsUnknownTopic_GracefullyIgnores()
    {
        // Arrange
        var mockAdminClient = new Mock<IAdminClient>();        
        var mockProducer = new Mock<IProducer<byte[], byte[]>>();
        var config = new KafkaSetupConfiguration("localhost:9092", []);
        var seeder = new TestableKafkaSeeder(config, _logger, mockAdminClient.Object, mockProducer.Object)
        {
            MockGetAllTopicNames = () => ["ghost-topic"],
            MockGetUserTopicsToDelete = all => all
        };

        var deleteException = new DeleteTopicsException(
        [
            new DeleteTopicReport { Topic = "ghost-topic", Error = new Error(ErrorCode.UnknownTopicOrPart) } 
        ]);

        mockAdminClient.Setup(x => x.DeleteTopicsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<DeleteTopicsOptions>()))
            .ThrowsAsync(deleteException);

        // Act
        var exception = await Record.ExceptionAsync(() => seeder.ResetAsync());

        // Assert
        Assert.Null(exception); // Should swallow the exception safely
    }

    [Fact]
    public async Task ResetAsync_DeleteTopicsThrowsOtherError_ThrowsException()
    {
        // Arrange
        var mockAdminClient = new Mock<IAdminClient>();
        var mockProducer = new Mock<IProducer<byte[], byte[]>>();
        var config = new KafkaSetupConfiguration("localhost:9092", []);
        var seeder = new TestableKafkaSeeder(config, _logger, mockAdminClient.Object, mockProducer.Object)
        {
            MockGetAllTopicNames = () => ["locked-topic"],
            MockGetUserTopicsToDelete = all => all
        };

        var deleteException = new DeleteTopicsException(
        [
            new DeleteTopicReport { Topic = "locked-topic", Error = new Error(ErrorCode.ClusterAuthorizationFailed) } 
        ]);

        mockAdminClient.Setup(x => x.DeleteTopicsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<DeleteTopicsOptions>()))
            .ThrowsAsync(deleteException);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => seeder.ResetAsync());
        Assert.Contains("Failed to purge Kafka topics", ex.Message);
    }

    [Fact]
    public async Task ResetAsync_WaitForDeletionTimesOut_ThrowsTimeoutException()
    {
        // Arrange
        var mockAdminClient = new Mock<IAdminClient>();
        var mockProducer = new Mock<IProducer<byte[], byte[]>>();

        var config = new KafkaSetupConfiguration("localhost:9092", []);
        var seeder = new TestableKafkaSeeder(config, _logger, mockAdminClient.Object, mockProducer.Object)
        {
            // Always return the topic, simulating a broker that never deletes it
            MockGetAllTopicNames = () => ["stubborn-topic"], 
            MockGetUserTopicsToDelete = all => all
        };

        // Act & Assert
        // NOTE: This test will physically take 5 seconds to run because of the hardcoded TimeSpan.FromSeconds(5)
        var ex = await Assert.ThrowsAsync<TimeoutException>(() => seeder.ResetAsync());
        Assert.Contains("Kafka topics were not fully deleted", ex.Message);
    }

    // --- Testable Subclass ---
    private class TestableKafkaSeeder : KafkaSeeder
    {
        private readonly IAdminClient _mockClient;
        private readonly IProducer<byte[], byte[]> _mockProducer;

        // Delegates to easily control what the protected methods return per-test
        public Func<List<string>>? MockGetAllTopicNames { get; set; }
        public Func<List<string>, List<string>>? MockGetUserTopicsToDelete { get; set; }

        public TestableKafkaSeeder(KafkaSetupConfiguration config, Microsoft.Extensions.Logging.ILogger logger,
            IAdminClient mockClient, IProducer<byte[], byte[]> mockProducer) 
            : base(config, logger)
        {
            _mockClient = mockClient;
            _mockProducer = mockProducer;
            TopicDeletionTimeout = TimeSpan.FromMilliseconds(300);
        }

        protected override IAdminClient BuildAdminClient() => _mockClient;

        protected override IProducer<byte[], byte[]> BuildProducer() => _mockProducer;
        protected override Task<List<string>> GetAllTopicNamesAsync(IAdminClient adminClient, CancellationToken ct)
        {
            return Task.FromResult(MockGetAllTopicNames?.Invoke() ?? new List<string>());
        }

        protected override Task<List<string>> GetUserTopicsToDeleteAsync(IAdminClient adminClient, List<string> allTopicNames, CancellationToken ct)
        {
            return Task.FromResult(MockGetUserTopicsToDelete?.Invoke(allTopicNames) ?? new List<string>());
        }
    }
}