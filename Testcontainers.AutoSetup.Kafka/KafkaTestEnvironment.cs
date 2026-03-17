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
    public readonly IContainer? KafkaUiContainer = null!;
    public readonly INetwork? KafkaNetwork = null!;

    public KafkaTestEnvironment(
        INetwork? network, KafkaContainer kafkaContainer, IContainer? kafkaUiContainer)
    {
        if(kafkaUiContainer is not null && network is null)
            throw new InvalidOperationException("KafkaUI cannot be started with with null network");
        
        KafkaNetwork = network;
        KafkaContainer = kafkaContainer ?? throw new ArgumentNullException(nameof(kafkaContainer));
        KafkaUiContainer = kafkaUiContainer;
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

        List<Task> containersToStart = [KafkaContainer.StartAsync(cancellationToken)];
        if(KafkaUiContainer is not null)
            containersToStart.Add(KafkaUiContainer.StartAsync(cancellationToken));

        await Task.WhenAll(containersToStart);
    }
}
