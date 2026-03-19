using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.Kafka;

namespace Testcontainers.AutoSetup.Kafka;

/// <summary>
/// Encapsulates a <see cref="KafkaContainer"/> and related containers and network.
/// </summary>
public class KafkaTestEnvironment
{
    public readonly KafkaContainer KafkaContainer = null!;
    public readonly IContainer? KafkaUiContainer = null;
    public readonly IContainer? SchemaRegistryContainer = null;
    public readonly INetwork? KafkaNetwork = null;

    internal KafkaTestEnvironment(
        INetwork? network, KafkaContainer kafkaContainer,
        IContainer? kafkaUiContainer, IContainer? schemaRegistryContainer)
    {
        if((kafkaUiContainer is not null || schemaRegistryContainer is not null) && network is null)
            throw new InvalidOperationException("KafkaUI/Schema registry cannot be started with null network");
        
        KafkaContainer = kafkaContainer ?? throw new ArgumentNullException(nameof(kafkaContainer));

        KafkaNetwork = network;
        KafkaUiContainer = kafkaUiContainer;
        SchemaRegistryContainer = schemaRegistryContainer;
    }

    /// <summary>
    /// Starts Kafka container and other related containers (e.g. KafkaUI) along with network creation.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if(KafkaNetwork is not null)
            await KafkaNetwork.CreateAsync(cancellationToken);

        // List<Task> containersToStart = [KafkaContainer.StartAsync(cancellationToken)];
        // if(KafkaUiContainer is not null)
        //     containersToStart.Add(KafkaUiContainer.StartAsync(cancellationToken));

        // if(SchemaRegistryContainer is not null)
        //     containersToStart.Add(SchemaRegistryContainer.StartAsync(cancellationToken));

        // await Task.WhenAll(containersToStart);

        await KafkaContainer.StartAsync(cancellationToken);
        if(KafkaUiContainer is not null)
            await KafkaUiContainer.StartAsync(cancellationToken);

        if(SchemaRegistryContainer is not null)
            await SchemaRegistryContainer.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Returns a string URL to a registry server
    /// </summary>
    public string? GetRegistryServer()
    {
        if (SchemaRegistryContainer is null)
            return null;
// TODO move port to const
        return $"http://{SchemaRegistryContainer.Hostname}:{SchemaRegistryContainer.GetMappedPublicPort(8081)}";
    }
}
