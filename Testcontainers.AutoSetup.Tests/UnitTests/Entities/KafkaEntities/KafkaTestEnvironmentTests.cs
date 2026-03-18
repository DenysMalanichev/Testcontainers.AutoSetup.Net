using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Win32;
using Moq;
using Testcontainers.AutoSetup.Kafka;
using Testcontainers.AutoSetup.Tests.TestCollections;
using Testcontainers.Kafka;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Entities.KafkaEntities;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class KafkaTestEnvironmentTests
{
    [Fact]
    public void Ctor_ThrowsArgumentNullException_IfKafkaContainerIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new KafkaTestEnvironment(null!, null!, null!, null!)); 
        Assert.Contains("kafkaContainer", exception.Message);
    }

    [Fact]
    public void Ctor_ThrowsInvalidOperationException_IfKafkaUIIsSetWithoutNetwork()
    {
        // Arrange
        var dummyKafkaContainer = (KafkaContainer)RuntimeHelpers.GetUninitializedObject(typeof(KafkaContainer));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => new KafkaTestEnvironment(network: null, dummyKafkaContainer, kafkaUiContainer: Mock.Of<IContainer>(), schemaRegistryContainer: null)); 
    } 

    [Fact]
    public void Ctor_ThrowsInvalidOperationException_IfKafkaSchemaRegistryIsSetWithoutNetwork()
    {
        // Arrange
        var dummyKafkaContainer = (KafkaContainer)RuntimeHelpers.GetUninitializedObject(typeof(KafkaContainer));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => new KafkaTestEnvironment(network: null, dummyKafkaContainer, kafkaUiContainer: null, schemaRegistryContainer: Mock.Of<IContainer>())); 
    } 

    [Fact]
    public void Ctor_ThrowsInvalidOperationException_IfBothKafkaUIAndSchemaRegistryAreSetWithoutNetwork()
    {
        // Arrange
        var dummyKafkaContainer = (KafkaContainer)RuntimeHelpers.GetUninitializedObject(typeof(KafkaContainer));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => new KafkaTestEnvironment(network: null, dummyKafkaContainer, kafkaUiContainer: Mock.Of<IContainer>(), schemaRegistryContainer: Mock.Of<IContainer>())); 
    } 

    public static TheoryData<(INetwork?, IContainer?, IContainer?)> CtorParamsSets => new()
    {
        (Mock.Of<INetwork>(), Mock.Of<IContainer>(), Mock.Of<IContainer>()),
        (Mock.Of<INetwork>(), Mock.Of<IContainer>(), null),
        (Mock.Of<INetwork>(), null, Mock.Of<IContainer>()),
        (null, null, null),
    };

    [Theory]
    [MemberData(nameof(CtorParamsSets))]
    public void Ctor_CreatesInstance_IfConfigured((INetwork network, IContainer kafkaUi, IContainer schemaRegistry) ctorArgs)
    {
        // Arrange
        var dummyKafkaContainer = (KafkaContainer)RuntimeHelpers.GetUninitializedObject(typeof(KafkaContainer));

        // Act 
        var env = new KafkaTestEnvironment(ctorArgs.network, dummyKafkaContainer, ctorArgs.kafkaUi, ctorArgs.schemaRegistry); 

        // Assert
        Assert.NotNull(env);
        Assert.Same(ctorArgs.network, env.KafkaNetwork);
        Assert.Same(dummyKafkaContainer, env.KafkaContainer);
        Assert.Same(ctorArgs.kafkaUi, env.KafkaUiContainer);
        Assert.Same(ctorArgs.schemaRegistry, env.SchemaRegistryContainer);
    } 

    [Fact]
    public void GetRegistryServer_ReturnsNull_IfRegistryIsNotConfigured()
    {
        // Arrange
        IContainer? registry = null;
        var dummyKafkaContainer = (KafkaContainer)RuntimeHelpers.GetUninitializedObject(typeof(KafkaContainer));
        var kafkaTstEnv = new KafkaTestEnvironment(Mock.Of<INetwork>(), dummyKafkaContainer, Mock.Of<IContainer>(), registry); 

        // Act
        var registryServer = kafkaTstEnv.GetRegistryServer();

        // Assert
        Assert.Null(registryServer);
    } 

    [Fact]
    public void GetRegistryServer_ReturnsCorrectUrl_IfRegistryIsConfigured()
    {
        // Arrange
        const string registryHostname = "Kafka-registry-hostname";
        var registryMock = new Mock<IContainer>();
        registryMock.Setup(r => r.Hostname).Returns(registryHostname);
        registryMock.Setup(r => r.GetMappedPublicPort(8081)).Returns(8081); // TODO use const port
        var dummyKafkaContainer = (KafkaContainer)RuntimeHelpers.GetUninitializedObject(typeof(KafkaContainer));
        var kafkaTstEnv = new KafkaTestEnvironment(Mock.Of<INetwork>(), dummyKafkaContainer, null!, registryMock.Object); 

        // Act
        var registryServer = kafkaTstEnv.GetRegistryServer();

        // Assert
        Assert.NotNull(registryServer);
        Assert.Equal($"http://{registryHostname}:8081", registryServer);
    } 
}
