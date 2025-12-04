using System.Runtime.InteropServices;
using DotNet.Testcontainers.Containers;
using Testcontainers.Core.Abstractions;
using TestcontainersAutoSetup.Core.Abstractions;

namespace TestcontainersAutoSetup.Core.Implementation;
public class AutoSetupContainerBuilder : IAutoSetupContainerBuilder
{
    private readonly List<IContainerSetup> _containerSetups = new();
    private static readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public string DockerEndpoint { get; private set; }
    private bool IsRunningInCI { get; set; }

    public AutoSetupContainerBuilder(string dockerEndpoint)
    {
        DockerEndpoint = dockerEndpoint;   
    }

    public AutoSetupContainerBuilder()
    {
        DockerEndpoint = string.Empty;
    }

    /// <summary>
    /// The creation of DB snapshot will be skipped and containers will be automatically destroyed,
    /// if tests are running in CI.
    /// </summary>
    /// <param name="isLocalRun">Boolean representing if a run is on local machine</param>
    /// <returns></returns>
    public AutoSetupContainerBuilder IsLocalRun(bool isLocalRun)
    {
        IsRunningInCI = !isLocalRun;
        return this;
    }

    public void AddContainerSetup(IContainerSetup setup)
    {
        _containerSetups.Add(setup);
    }

    public async Task<List<IContainer>> BuildAsync()
    {
        var containers = new List<IContainer>();
        foreach (var setup in _containerSetups)
        {
            var container = await setup.BuildAndInitializeAsync();
            containers.Add(container);
        }
        return containers;
    }

    public IAutoSetupContainerBuilder WithMySqlContainer()
    {
        throw new NotImplementedException();
    }

    public IAutoSetupContainerBuilder WithMongoDbContainer()
    {
        throw new NotImplementedException();
    }

    IAutoSetupContainerBuilder IAutoSetupContainerBuilder.IsLocalRun(bool isLocalRun)
    {
        return IsLocalRun(isLocalRun);
    }
}
