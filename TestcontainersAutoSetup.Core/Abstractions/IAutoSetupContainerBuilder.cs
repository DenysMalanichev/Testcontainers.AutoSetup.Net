

using DotNet.Testcontainers.Containers;

namespace TestcontainersAutoSetup.Core.Abstractions;

public partial interface IAutoSetupContainerBuilder
{
    public IAutoSetupContainerBuilder WithMySqlContainer();

    public IAutoSetupContainerBuilder WithMongoDbContainer();

    public IAutoSetupContainerBuilder IsLocalRun(bool isLocalRun);

    public string DockerEndpoint { get; }

    public Task<List<IContainer>> BuildAsync();
}