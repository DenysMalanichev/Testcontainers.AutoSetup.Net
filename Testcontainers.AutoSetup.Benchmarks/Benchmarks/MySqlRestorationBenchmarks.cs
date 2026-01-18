using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Tests.IntegrationTests.TestHelpers;
using Testcontainers.MySql;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.Common.DbStrategy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Testcontainers.AutoSetup.Benchmarks;

[SimpleJob(RunStrategy.Monitoring, launchCount: 3, warmupCount: 5, iterationCount: 10)]
[MemoryDiagnoser]
public class MySqlRestorationBenchmarks
{
    private MySqlContainer _container = null!;
    private IDbStrategy _strategy = null!;
    private MySqlDbConnectionFactory _dbConnectionFactory = new(); 

    [Params(1, 10, 100, 1000, 10_000, 50_000)] 
    public int SeedRowCount { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // A. Generate a Heavy SQL Script dynamically based on the param
       var heavyScript = $@"
            DROP DATABASE IF EXISTS `HeavyCatalog`;
            CREATE DATABASE `HeavyCatalog`;
            USE `HeavyCatalog`;

            CREATE TABLE Catalog (
                Id INT PRIMARY KEY AUTO_INCREMENT,
                Name VARCHAR(100) NOT NULL,
                Payload LONGTEXT NULL -- Heavy column (approx 4GB max in MySQL)
            );
            
            -- MySQL requires a Stored Procedure to run WHILE loops
            DROP PROCEDURE IF EXISTS LoadData;
            
            CREATE PROCEDURE LoadData()
            BEGIN
                DECLARE i INT DEFAULT 0;
                
                -- Start Transaction for speed
                START TRANSACTION;
                
                WHILE i < {SeedRowCount} DO
                    INSERT INTO Catalog (Name, Payload) 
                    VALUES (CONCAT('Item_', i), REPEAT('X', 1000)); -- REPEAT is MySQL's REPLICATE
                    SET i = i + 1;
                END WHILE;
                
                COMMIT;
            END;

            -- Execute and Cleanup
            CALL LoadData();
            DROP PROCEDURE LoadData;
            
            SELECT 'Seeding Complete' AS Msg;
        ";

        await File.WriteAllTextAsync("./BenchmarkData/MySql//ParamsScript.sql", heavyScript);

        _container = new MySqlBuilder("mysql:8.0.44-debian")
            .WithMySQLAutoSetupDefaults(containerName: "Perfromance-MySQL-testcontainer")
            .WithUsername("root")
            .WithCommand("--skip-name-resolve")
            .Build();

        await _container.StartAsync();

        var dbSetup = new RawSqlDbSetup(
            dbName: "HeavyCatalog", 
            dbType: Core.Common.Enums.DbType.MsSQL,
            containerConnectionString: _container.GetConnectionString(),
            migrationsPath: "./BenchmarkData/MySql/",
            sqlFiles: ["ParamsScript.sql"]
        );

        _strategy = new DbSetupStrategyBuilder(dbSetup, _container, NullLogger.Instance) // Use NullLogger in real benchmark
            .WithRawSqlDbSeeder(_dbConnectionFactory)
            .WithMySqlDbRestorer(_dbConnectionFactory)
            .Build();

        // This creates a snapshot within a container
        await _strategy.InitializeGlobalAsync();
    }

    // This simulates a test case that inserts junk data that needs to be wiped.
    [IterationSetup]
    public void DirtyTheDatabase()
    {
        var connectionString = _container.GetConnectionString()
            .Replace("Database=test", "Database=HeavyCatalog");
            
        using var conn = _dbConnectionFactory.CreateDbConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Catalog (Name, Payload) VALUES ('DIRTY_ROW', 'Should be deleted by restore');";
        cmd.ExecuteNonQuery();
    }

    // Measure how long it takes to revert to the clean snapshot
    [Benchmark]
    public async Task Restore_HeavyDatabase()
    {
        await _strategy.ResetAsync();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _container.DisposeAsync();
    }
}
