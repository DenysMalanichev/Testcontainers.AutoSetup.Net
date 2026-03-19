using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Kafka;
using Testcontainers.Kafka;

namespace Testcontainers.AutoSetup.Benchmarks;

[SimpleJob(RunStrategy.Monitoring, launchCount: 3, warmupCount: 5, iterationCount: 10)]
[MemoryDiagnoser]
public class KafkaMessageSeedBenchmarks
{
    private KafkaContainer _container = null!;
    private IInstanceStrategy _strategy = null!;
    private KafkaSetupConfiguration _kafkaConfig = null!;

    [Params(1, 10, 100, 1000)] 
    public int SeedMessagesCount { get; set; }

    [Params(true, false)] 
    public bool UseTmpfs { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {        
        // Generate a Heavy json data file dynamically based on the param
        var topicConfig = new KafkaTopicConfiguration("message-seed-benchmark");
        var payload = new string('x', 1024 * 512); // half MB payload

        for (int i = 0; i < SeedMessagesCount; i++)
        {
            topicConfig.WithSeedMessage($"testKey-{i}", payload);
        }

        _container = new KafkaBuilder("confluentinc/cp-kafka:7.5.0")
            .WithKafkaAutoSetupDefaults(containerName: "Perfromance-Kafka-testcontainer", UseTmpfs)
            .Build();

        await _container.StartAsync();

        _kafkaConfig = new KafkaSetupBuilder(_container.GetBootstrapAddress())
            .WithTopic(topicConfig)
            .Build();

        _strategy = new KafkaSeeder(_kafkaConfig, NullLogger.Instance);

        await _strategy.InitializeGlobalAsync();
    }

    [Benchmark]
    public async Task Restore_KafkaMessagesSeeding()
    {
        await _strategy.ResetAsync();
    }
}
