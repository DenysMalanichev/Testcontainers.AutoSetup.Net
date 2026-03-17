using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.AutoSetup.Core.Helpers;
using Testcontainers.Kafka;

namespace Testcontainers.AutoSetup.Kafka;

/// <summary>
/// Builds <see cref="KafkaTestEnvironment"/> with fluent API.
/// If configured with additional containers (e.g. KafkaUI) - creates a docker network for these related containers.
/// </summary>
public class KafkaTestEnvironmentBuilder
{
    private KafkaBuilder _kafkaBuilder = null!;
    private IContainer? _kafkaUiContainer = null!;
    private INetwork? _kafkaNetwork = null!;

    private const int KafkaUiPort = 8080;
    private const string KafkaNetworkAliace = "kafka-broker";
    private readonly string _kafkaNetworkAliase = $"Testcontainers-kafka-network-{Guid.NewGuid()}";

    /// <summary>
    /// Adds a Kafka container to the <see cref="KafkaTestEnvironment"/>
    /// </summary>
    /// <param name="kafkaBuilder"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"> thrown if kafka is already configured </exception>
    public KafkaTestEnvironmentBuilder WithKafkaBuilder(KafkaBuilder kafkaBuilder)
    {
        if(_kafkaBuilder is not null)
            throw new InvalidOperationException("Kafka builder is already set up");
        ArgumentNullException.ThrowIfNull(kafkaBuilder);

        _kafkaBuilder = kafkaBuilder.WithNetworkAliases(KafkaNetworkAliace);
        return this;
    }

    /// <summary>
    /// Adds a KafkaUI container to <see cref="KafkaTestEnvironment"/>.
    /// A Docker network will be created for Kafka - KafkaUI communication.
    /// KafkaUI container will not be created in CI. 
    /// </summary>
    /// <param name="hostPort">Custom port on the host machine to map default KafkaUI's 8080 port to</param>
    /// <param name="image">Custom KafkaUI image</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public KafkaTestEnvironmentBuilder WithKafkaUI(
        int hostPort = 8080, string image = "provectuslabs/kafka-ui:latest")
    {
        if(_kafkaUiContainer is not null)
            throw new InvalidOperationException("Kafka UI is already configured");

        var dockerEndpoint = EnvironmentHelper.GetDockerEndpoint();

        // No need to create a KafkaUI container in CI
        if(EnvironmentHelper.IsCiRun())
            return this;

        if(_kafkaNetwork is null)
            BuildNetwork();

        _kafkaUiContainer = new ContainerBuilder(image)
            .WithNetwork(_kafkaNetworkAliase)
            .WithDockerEndpoint(dockerEndpoint)
            .WithPortBinding(KafkaUiPort, hostPort)
            .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "AutoSetup-Local")
            .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", "kafka-broker:9092")
            .WithReuse(true)
            .Build();

        return this;
    }

    /// <summary>
    /// Builds the <see cref="KafkaTestEnvironment"/>
    /// </summary>
    /// <returns></returns>
    public KafkaTestEnvironment Build()
    {
        if(_kafkaUiContainer is not null)
        {
            _kafkaBuilder = _kafkaBuilder.WithNetwork(_kafkaNetworkAliase);
        }

        var kafkaContainer = _kafkaBuilder.Build();

        return new KafkaTestEnvironment(_kafkaNetwork, kafkaContainer, _kafkaUiContainer);   
    }

    private void BuildNetwork()
    {
        if(_kafkaNetwork is not null)
            return;

        var dockerEndpoint = EnvironmentHelper.GetDockerEndpoint();

        _kafkaNetwork = new NetworkBuilder()
            .WithLabel("reuse-id", $"{_kafkaNetworkAliase}-reuse-hash")
            .WithDockerEndpoint(dockerEndpoint)
            .WithName(_kafkaNetworkAliase)
            .WithReuse(true)
            .Build();
    }
}
