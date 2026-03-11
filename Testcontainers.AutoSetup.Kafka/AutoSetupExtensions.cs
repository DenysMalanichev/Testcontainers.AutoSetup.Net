using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.AutoSetup.Core.Helpers;
using Testcontainers.Kafka;

namespace Testcontainers.AutoSetup.Kafka;

public static class AutoSetupExtensions
{
    private const string DefaultConfluentImageDataPath = "/var/lib/kafka/data";
    private const string DefaultApacheKRaftImageDataPath = "/tmp/kraft-combined-logs";

    /// <summary>
    /// Applies highly optimized defaults to a Kafka Testcontainer for local development and testing.
    /// </summary>
    /// <remarks>
    /// <para><b>Performance Optimizations:</b></para>
    /// <list type="bullet">
    /// <item><b>KRaft Mode:</b> Enables KRaft (Kafka Raft) mode, bypassing the need for a separate ZooKeeper container.</item>
    /// <item><b>Tmpfs Data Volumes:</b> Mounts the Kafka data directories to in-memory <c>tmpfs</c> volumes. This forces all log segments and metadata writes into RAM, drastically reducing topic creation and message processing latency. 
    /// <i>Note: Tmpfs is automatically disabled in CI environments to prevent memory limit crashes, and defaults to disabled in WSL unless explicitly requested.</i></item>
    /// <item><i>Note: Container is running under root</i></item>
    /// <item><b>Zero Rebalance Delay:</b> Sets <c>KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS=0</c> so consumer groups form instantly without the standard 3-second production wait time.</item>
    /// <item><b>Single-Node Replication:</b> Forces offset and transaction state replication factors to 1, preventing the broker from waiting for non-existent replicas.</item>
    /// <item><b>Relaxed Disk Flushing:</b> Increases the log flush intervals, reducing I/O bottleneck overhead during heavy test suites.</item>
    /// </list>
    /// </remarks>
    /// <param name="builder">The KafkaBuilder instance.</param>
    /// <param name="containerName">The persistent name to apply to the container for reuse.</param>
    /// <param name="useTmpfs">Explicitly enable or disable tmpfs mounts. If null, defaults to true on standard hosts, and false on WSL.</param>
    /// <returns><see cref="KafkaBuilder"/></returns>
    public static KafkaBuilder WithKafkaAutoSetupDefaults(
        this KafkaBuilder builder, string containerName, bool? useTmpfs = null)
    {
        var isCiRun = EnvironmentHelper.IsCiRun();
        var isWslDocker = EnvironmentHelper.IsWslDocker();
        return builder
            .WithKafkaAutoSetupDefaultsInternal(containerName, isCiRun, isWslDocker, useTmpfs);
    }

    internal static KafkaBuilder WithKafkaAutoSetupDefaultsInternal(
        this KafkaBuilder builder, string containerName,
        bool isCiRun, bool isWslDocker, bool? useTmpfs = null)
    {
        if(!isCiRun && ((useTmpfs is null && !isWslDocker) || (useTmpfs is not null && useTmpfs.Value == true)))
        {
            builder = builder
                .WithCreateParameterModifier(config =>
                {
                    config.User = "root"; // Using root user to ensure permissions for tmpfs mounts
                })
                .WithTmpfsMount(DefaultConfluentImageDataPath)
                .WithTmpfsMount(DefaultApacheKRaftImageDataPath);
        }

        return builder
            // Rebalance & Cluster Optimizations
            .WithEnvironment("KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
            .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1")
            
            // Disk Flush Optimizations (Speeds up processing, especially when tmpfs is off)
            .WithEnvironment("KAFKA_LOG_FLUSH_INTERVAL_MESSAGES", "10000")
            .WithEnvironment("KAFKA_LOG_FLUSH_INTERVAL_MS", "1000")

            // KRaft mode only
            .WithKRaft()
            .WithAutoSetupReuseDefaults(containerName);
    }
}
