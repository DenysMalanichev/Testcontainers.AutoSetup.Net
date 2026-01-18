using BenchmarkDotNet.Running;
using Testcontainers.AutoSetup.Benchmarks;

BenchmarkRunner.Run<MsSqlRestorationBenchmarks>();
BenchmarkRunner.Run<MySqlRestorationBenchmarks>();
BenchmarkRunner.Run<MongoDbRestorationBenchmarks>();
