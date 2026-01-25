using System.Reflection;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.AutoSetup.Tests.TestCollections;
using Testcontainers.MsSql;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Extensions;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class AutoSetupExtensionsTests
{
    [Fact]
    public void WithAutoSetupDefaultsInternal_BuiltWithAutoSetupDefaultsLocally_HasAllRequiredConfigurations()
    {
        // Arrange
        const string containerName = "MsSqlContainer-unit-test";
        const string dockerEndpoint = "tcp://127.0.0.1:2375";
        var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04");
        builder = builder.WithAutoSetupDefaultsInternal(containerName, dockerEndpoint, isCiRun: false);

        //Act
        var container = builder.Build();

        //Assert
        Assert.NotNull(container);
        var configuration = container.GetConfiguration();

        Assert.True(configuration.Reuse);
        Assert.Equal(containerName, configuration.Name);
        Assert.Equal($"{containerName}-reuse-hash", configuration.Labels["reuse-id"]);
        Assert.NotNull(configuration.DockerEndpointAuthConfig);
    }

    [Fact]
    public void WithAutoSetupDefaultsInternal_WithMSSQLAutoSetupDefaultsInternal_HasAllRequiredConfigurations()
    {
        // Arrange
        const string containerName = "MsSqlContainer-unit-test";
        const string dockerEndpoint = "tcp://127.0.0.1:2375";
        var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04");
        builder = builder.WithDockerEndpoint(dockerEndpoint);
        builder = builder.WithMSSQLAutoSetupDefaultsInternal(containerName, true);

        //Act
        var container = builder.Build();

        //Assert
        Assert.NotNull(container);
        var configuration = container.GetConfiguration();

        Assert.Single(
            configuration.Mounts, 
            m => 
                m.Source.Equals($"{containerName}-Restoration") && 
                m.Target.Equals(Constants.MsSQL.DefaultRestorationStateFilesPath) && 
                m.Type.Type == MountType.Volume.Type &&
                m.AccessMode == AccessMode.ReadWrite);
        Assert.Single(
            configuration.Mounts,
            m =>
                m.Type.Type == MountType.Tmpfs.Type &&
                m.AccessMode == AccessMode.ReadWrite &&
                m.Target.Equals(Constants.MsSQL.DefaultRestorationDataFilesPath)
        );

        var parameters = new CreateContainerParameters();
        foreach (var modifier in configuration.ParameterModifiers)
        {
            modifier(parameters);
        }
        Assert.Equal("root", parameters.User);
    }

    [Fact]
    public void WithAutoSetupDefaultsInternal_WithMSSQLAutoSetupDefaultsInternal_DoesntCreateTmpfsIfDisabled()
    {
        // Arrange
        const string containerName = "MsSqlContainer-unit-test";
        const string dockerEndpoint = "tcp://127.0.0.1:2375";
        var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04");
        builder = builder.WithDockerEndpoint(dockerEndpoint);
        builder = builder.WithMSSQLAutoSetupDefaultsInternal(containerName, useTmpfs: false);

        //Act
        var container = builder.Build();

        //Assert
        Assert.NotNull(container);
        var configuration = container.GetConfiguration();

        Assert.Single(
            configuration.Mounts, 
            m => 
                m.Source.Equals($"{containerName}-Restoration") && 
                m.Target.Equals(Constants.MsSQL.DefaultRestorationStateFilesPath) && 
                m.Type.Type == MountType.Volume.Type &&
                m.AccessMode == AccessMode.ReadWrite);
        Assert.DoesNotContain(configuration.Mounts, m => m.Type.Type == MountType.Tmpfs.Type);

        var parameters = new CreateContainerParameters();
        foreach (var modifier in configuration.ParameterModifiers)
        {
            modifier(parameters);
        }
        Assert.Equal("root", parameters.User);
    }

    [Fact]
    public void MsSqlContainerBuidler_BuiltWithAutoSetupDefaultsInCi_DoesntHaveConfigurations()
    {
        // Arrange
        const string containerName = "MsSqlContainer-unit-test";
        const string dockerEndpoint = "tcp://127.0.0.1:2375";
        var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04");
        builder = builder.WithAutoSetupDefaultsInternal(containerName, dockerEndpoint, isCiRun: true);

        // Act
        var container = builder.Build();

        //Assert
        Assert.NotNull(container);
        var configuration = container.GetConfiguration();

        Assert.False(configuration.Reuse);
        Assert.Null(configuration.Name);
        Assert.DoesNotContain(configuration.Labels, l => l.Key == "reuse-id");
        Assert.StartsWith(dockerEndpoint, configuration.DockerEndpointAuthConfig.Endpoint.ToString());
    }
}


public static class TestContainerExtensions
{
    // Uses reflection to grab the hidden 'Configuration' property from the container
    public static IContainerConfiguration GetConfiguration(this IContainer container)
    {
        var propInfo = typeof(DockerContainer)
            .GetField("_configuration", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        
        return (IContainerConfiguration)propInfo!.GetValue(container)!;
    }
}