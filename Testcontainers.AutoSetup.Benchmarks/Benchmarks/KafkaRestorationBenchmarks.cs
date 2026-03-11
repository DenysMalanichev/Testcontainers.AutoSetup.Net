using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Helpers;
using Testcontainers.AutoSetup.Kafka;
using Testcontainers.AutoSetup.Tests.UnitTests.Extensions;
using Testcontainers.Kafka;

namespace Testcontainers.AutoSetup.Benchmarks;

[SimpleJob(RunStrategy.Monitoring, launchCount: 3, warmupCount: 5, iterationCount: 10)]
[MemoryDiagnoser]
public class KafkaRestorationBenchmarks
{
    private KafkaContainer _container = null!;
    private IInstanceStrategy _strategy = null!;
    private KafkaSetupConfiguration _kafkaConfig = null!;

    [Params(1, 10, 100, 1000)] 
    public int SeedTopicsCount { get; set; }

    [Params(true, false)] 
    public bool UseTmpfs { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {        
        // A. Generate a Heavy json data file dynamically based on the param
        var topics = new List<KafkaTopicConfiguration>();

        for (int i = 0; i < SeedTopicsCount; i++)
        {
            topics.Add(new KafkaTopicConfiguration($"topic_{i}", 1, 1));
        }

        _container = new KafkaBuilder("confluentinc/cp-kafka:7.5.0")
            .WithKafkaAutoSetupDefaults(containerName: "Perfromance-MongoDB-testcontainer", UseTmpfs)
            .Build();

        await _container.StartAsync();

        _kafkaConfig = new KafkaSetupConfiguration(
            _container.GetBootstrapAddress(),
            topics
        );

        _strategy = new KafkaSeeder(_kafkaConfig, NullLogger.Instance);

        await _strategy.InitializeGlobalAsync();
    }

    [Benchmark]
    public async Task Restore_KafkaTopicsCreation()
    {
        await _strategy.ResetAsync();
    }
}
