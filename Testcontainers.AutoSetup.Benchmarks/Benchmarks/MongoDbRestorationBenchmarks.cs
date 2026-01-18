using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Common.DbStrategy;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.MongoDb;

namespace Testcontainers.AutoSetup.Benchmarks;

[SimpleJob(RunStrategy.Monitoring, launchCount: 3, warmupCount: 5, iterationCount: 10)]
[MemoryDiagnoser]
public class MongoDbRestorationBenchmarks
{
    private MongoDbContainer _container = null!;
    private IDbStrategy _strategy = null!;
    private RawMongoDbSetup _dbSetup = null!;

    [Params(1, 10, 100, 1000, 10_000, 50_000)] 
    public int SeedRowCount { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        const string MigrationsPath = "./BenchmarkData/MongoDB/";
        const string DbName = "HeavyCatalog";
        const string CollectionName = "HeavyCollection";
        const string DataFileName = "HeavyData.json";

        // A. Generate a Heavy json data file dynamically based on the param
        var data = new List<object>();
        var payload = new string('X', 1000); // 1KB payload

        for (int i = 0; i < SeedRowCount; i++)
        {
            data.Add(new 
            { 
                Name = $"Item_{i}", 
                Payload = payload,
                Description = "Benchmark item for MongoDb restoration"
            });
        }

        var jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
        await File.WriteAllTextAsync($"{MigrationsPath}/{DataFileName}", jsonContent);

        _container = new MongoDbBuilder("mongo:6.0.27-jammy")
            .WithMongoAutoSetupDefaults(containerName: "Perfromance-MongoDB-testcontainer", MigrationsPath)
            .Build();

        await _container.StartAsync();

        _dbSetup = new RawMongoDbSetup(
            dbName: DbName, 
            migrationsPath: MigrationsPath,
            mongoFiles: new Dictionary<string, string> { {CollectionName, DataFileName} }
        )
        {
            Username = "mongo",
            Password = "mongo"
        };

        _strategy = new DbSetupStrategyBuilder(_dbSetup, _container, NullLogger.Instance) // Use NullLogger in real benchmark
            .WithRawMongoDbSeeder()
            .WithMongoDbRestorer()
            .Build();

        // This creates a snapshot within a container
        await _strategy.InitializeGlobalAsync();
    }

    // This simulates a test case that inserts junk data that needs to be wiped.
    [IterationSetup]
    public void DirtyTheDatabase()
    {
        var client = new MongoClient($"mongodb://mongo:mongo@localhost:{_container.GetMappedPublicPort(27017)}/");
        var db = client.GetDatabase("HeavyCatalog");
        var collection = db.GetCollection<BsonDocument>("HeavyCollection");

        var dirtyDoc = new BsonDocument
        {
            { "Name", "DIRTY_DOCUMENT" },
            { "Payload", "Should be deleted by restore" }
        };

        collection.InsertOne(dirtyDoc);
    }

    // Measure how long it takes to revert to the clean snapshot
    [Benchmark]
    public async Task Restore_HeavyDatabase()
    {
        await _strategy.ResetAsync();
    }
}
