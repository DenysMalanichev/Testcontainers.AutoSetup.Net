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
    private IContainer? _kafkaUiContainer = null;
    private IContainer? _schemaRegistryContainer = null;
    private INetwork? _kafkaNetwork = null;

    private ushort KafkaUiPort = 8080;
    private ushort SchemaRegistryPort = 8081;
    private const string KafkaAlias = "kafka-broker";
    private const string SchemaRegistryAlias = "schema-registry";
    private readonly string _kafkaNetworkAlias = "Testcontainers-kafka-network";

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

        _kafkaBuilder = kafkaBuilder.WithNetworkAliases(KafkaAlias);
        return this;
    }

    /// <summary>
    /// Adds a KafkaUI container to <see cref="KafkaTestEnvironment"/>.
    /// A Docker network will be created for Kafka - KafkaUI communication.
    /// KafkaUI container will not be created in CI. 
    /// NOTE: the provectuslabs/kafka-ui:latest image is used
    /// </summary>
    /// <param name="hostPort">Custom port on the host machine to map default KafkaUI's 8080 port to</param>
    /// <param name="image">Custom KafkaUI image</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public KafkaTestEnvironmentBuilder WithKafkaUI(
        ushort hostPort = 8080, string image = "provectuslabs/kafka-ui:latest")
    {
        if(_kafkaBuilder is null)
            throw new InvalidOperationException("Kafka builder is not set up");

        if(_kafkaUiContainer is not null)
            throw new InvalidOperationException("Kafka UI is already configured");

        var dockerEndpoint = EnvironmentHelper.GetDockerEndpoint();

        // No need to create a KafkaUI container in CI
        if(EnvironmentHelper.IsCiRun())
            return this;

        if(_kafkaNetwork is null)
            BuildNetwork();

        KafkaUiPort = hostPort;

        var kafkaUiBuilder = new ContainerBuilder(image)
            .WithNetwork(_kafkaNetworkAlias)
            .WithDockerEndpoint(dockerEndpoint)
            .WithPortBinding(hostPort, 8080)
            .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "AutoSetup-Local")
            .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", $"{KafkaAlias}:9092")
            .WithName("Testcontainers-Kafka-UI")
            .WithReuse(true); // Since we drop Kafka UI fo CI runs, it is safe to set true here
            
        if(_schemaRegistryContainer is not null)
            kafkaUiBuilder = kafkaUiBuilder
                .WithEnvironment("KAFKA_CLUSTERS_0_SCHEMAREGISTRY", $"http://{SchemaRegistryAlias}:{SchemaRegistryPort}");

        if(dockerEndpoint is not null)
            kafkaUiBuilder = kafkaUiBuilder.WithDockerEndpoint(dockerEndpoint);

        _kafkaUiContainer = kafkaUiBuilder.Build();

        return this;
    }

    /// <summary>
    /// Adds a schema registry container to the <see cref="KafkaTestEnvironment"/>
    /// NOTE: schema registry must be added BEFORE the KafkaUI.
    /// NOTE: the confluentinc/cp-schema-registry:latest image is used
    /// </summary>
    /// <param name="hostPort"></param>
    /// <param name="image"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public KafkaTestEnvironmentBuilder WithSchemaRegistry(
        ushort hostPort = 8081, string image = "confluentinc/cp-schema-registry:latest")
    {
        if(_kafkaBuilder is null)
            throw new InvalidOperationException("Kafka builder is not set up");     

        if(_schemaRegistryContainer is not null)
            throw new InvalidOperationException("Kafka schema registry is already configured");

        if(_kafkaUiContainer is not null)
            throw new InvalidOperationException("Kafka schema registry must be configured BEFORE KafkaUI container to ensure their correct configuration");

        var dockerEndpoint = EnvironmentHelper.GetDockerEndpoint();

        if(_kafkaNetwork is null)
            BuildNetwork();

        SchemaRegistryPort = hostPort;

        var registryBuilder = new ContainerBuilder(image)
            .WithNetwork(_kafkaNetworkAlias)
            .WithReuse(!EnvironmentHelper.IsCiRun())
            .WithName("Testcontainers-Kafka-schema-registry")
            .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
            .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", $"PLAINTEXT://{KafkaAlias}:9092") // TODO fix hardcoded port
            .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", $"http://0.0.0.0:{hostPort}")
            .WithNetworkAliases(SchemaRegistryAlias)
            .WithPortBinding(hostPort, 8081)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request.ForPath("/subjects").ForPort(hostPort)));
        
        if(dockerEndpoint is not null)
            registryBuilder = registryBuilder.WithDockerEndpoint(dockerEndpoint);
        
        _schemaRegistryContainer = registryBuilder.Build();

        return this;
    }

    /// <summary>
    /// Builds the <see cref="KafkaTestEnvironment"/>
    /// </summary>
    public KafkaTestEnvironment Build()
    {
        if(_kafkaUiContainer is not null || _schemaRegistryContainer is not null)
        {
            _kafkaBuilder = _kafkaBuilder.WithNetwork(_kafkaNetworkAlias);
        }

        var kafkaContainer = _kafkaBuilder.Build();

        return new KafkaTestEnvironment(_kafkaNetwork, kafkaContainer, _kafkaUiContainer, _schemaRegistryContainer);   
    }

    private void BuildNetwork()
    {
        if(_kafkaNetwork is not null)
            return;

        var dockerEndpoint = EnvironmentHelper.GetDockerEndpoint();

        var networkBuilder = new NetworkBuilder()
            .WithLabel("reuse-id", $"{_kafkaNetworkAlias}-reuse-hash")
            .WithName(_kafkaNetworkAlias)
            .WithReuse(!EnvironmentHelper.IsCiRun());

        if(dockerEndpoint is not null)
            networkBuilder = networkBuilder.WithDockerEndpoint(dockerEndpoint);

        _kafkaNetwork = networkBuilder.Build();
    }
}
