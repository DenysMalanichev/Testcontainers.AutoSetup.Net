using DotNet.Testcontainers.Containers;
using TestcontainersAutoSetup.Core.Implementation;

namespace Testcontainers.Core.Abstractions;

public interface IContainerSetup
{
    AutoSetupContainerBuilder And();
    Task<IContainer> BuildAndInitializeAsync();
}