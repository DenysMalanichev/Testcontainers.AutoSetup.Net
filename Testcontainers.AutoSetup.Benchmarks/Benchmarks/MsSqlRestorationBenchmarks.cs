using BenchmarkDotNet.Attributes;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.MsSql;
using Testcontainers.AutoSetup.Core.Common.DbStrategy;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.AutoSetup.Tests.IntegrationTests.TestHelpers;
using BenchmarkDotNet.Engines;
using Testcontainers.AutoSetup.Tests.UnitTests.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.AutoSetup.Core.Helpers;

namespace Testcontainers.AutoSetup.Benchmarks;

[SimpleJob(RunStrategy.Monitoring, launchCount: 3, warmupCount: 5, iterationCount: 10)]
[MemoryDiagnoser]
public class MsSqlRestorationBenchmarks
{
    private MsSqlContainer _container = null!;
    private IDbStrategy _strategy = null!;
    private MsSqlDbConnectionFactory _dbConnectionFactory = new(); 

    [Params(1, 10, 100, 1000, 10_000, 50_000)] 
    public int SeedRowCount { get; set; }
    
    [Params(true, false)] 
    public bool UseTmpfs { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // A. Generate a Heavy SQL Script dynamically based on the param
        var heavyScript = $@"
            -- CLEANUP: Drop all snapshots linked to HeavyCatalog
            USE master;
            DECLARE @KillSnapsSql NVARCHAR(MAX) = '';

            SELECT @KillSnapsSql = @KillSnapsSql + 'DROP DATABASE [' + name + ']; '
            FROM sys.databases
            WHERE source_database_id = DB_ID('HeavyCatalog');

            EXEC(@KillSnapsSql);

            DROP DATABASE IF EXISTS [HeavyCatalog];
            CREATE DATABASE [HeavyCatalog];
            GO
            USE [HeavyCatalog];
            CREATE TABLE Catalog (
                Id INT PRIMARY KEY IDENTITY(1,1),
                Name NVARCHAR(100) NOT NULL,
                Payload NVARCHAR(MAX) NULL -- Heavy column to increase size
            );
            GO
            
            -- FAST DATA GENERATION LOOP
            SET NOCOUNT ON;
            DECLARE @i INT = 0;
            BEGIN TRANSACTION;
            WHILE @i < {SeedRowCount}
            BEGIN
                INSERT INTO Catalog (Name, Payload) 
                VALUES (CONCAT('Item_', @i), REPLICATE('X', 1000)); -- 1KB payload per row
                SET @i = @i + 1;
            END
            COMMIT TRANSACTION;
            PRINT 'Seeding Complete';
        ";

        await File.WriteAllTextAsync("./BenchmarkData/MsSql/ParamsScript.sql", heavyScript);

        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04")
            .WithMSSQLAutoSetupDefaults(containerName: "Perfromance-MsSQL-testcontainer", UseTmpfs)
            .WithPassword("#AdminPass123")
            .Build();

        await _container.StartAsync();

        var dbSetup = new RawSqlDbSetup(
            dbName: "HeavyCatalog", 
            dbType: Core.Common.Enums.DbType.MsSQL,
            containerConnectionString: _container.GetConnectionString(),
            migrationsPath: "./BenchmarkData/MsSql/",
            sqlFiles: ["ParamsScript.sql"]
        );

        _strategy = new DbSetupStrategyBuilder(dbSetup, _container, NullLogger.Instance) // Use NullLogger in real benchmark
            .WithRawSqlDbSeeder(_dbConnectionFactory)
            .WithMsSqlRestorer(_dbConnectionFactory)
            .Build();

        // This creates a snapshot within a container
        await _strategy.InitializeGlobalAsync();
    }

    // This simulates a test case that inserts junk data that needs to be wiped.
    [IterationSetup]
    public void DirtyTheDatabase()
    {
        var connectionString = _container.GetConnectionString()
            .Replace("Database=master", "Database=HeavyCatalog");
            
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
    public void RemoveContainer()
    {
        var mounts = _container.GetConfiguration().Mounts;
        var containerId = _container.Id;

        Console.WriteLine($"[Cleanup] Attempting to kill container {containerId}...");

        _container.DisposeAsync()
            .GetAwaiter()
            .GetResult();

        // Fully removes the container ensuring that the next benchmark runs clean 
        if (!string.IsNullOrEmpty(containerId))
        {
            Console.WriteLine($"[Cleanup] Attempting to kill container {containerId}...");

            var isWslDocker = EnvironmentHelper.IsWslDocker();
            var containerRemoveProcstartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = isWslDocker ? "wsl" : "docker",
                Arguments = isWslDocker ? "docker " : " " + $"rm -f {containerId}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(containerRemoveProcstartInfo);
            if (process != null)
            {
                process.WaitForExitAsync().GetAwaiter().GetResult();

                // Log if something went wrong
                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEndAsync().GetAwaiter().GetResult();
                    Console.WriteLine($"[Cleanup Error] Failed to remove container: {error}");
                }
                else
                {
                    Console.WriteLine($"[Cleanup] Container {containerId} removed successfully.");
                }
            }
        }

        foreach (var mount in mounts)
        {
            mount.DeleteAsync().GetAwaiter().GetResult();
        }
    }
}
