using System.Reflection;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Extensions;
using Testcontainers.AutoSetup.Tests.TestCollections;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Testcontainers.MySql;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Extensions;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class AutoSetupExtensionsTests
{
    private const string ContainerName = "test-container";

    #region MSSQL Coverage

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
        builder = builder.WithMSSQLAutoSetupDefaultsInternal(containerName, isCiRun: false, isWslDocker: false, useTmpfs: true);

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
        builder = builder.WithMSSQLAutoSetupDefaultsInternal(containerName, isCiRun: false, isWslDocker: false, useTmpfs: false);

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
    public void WithAutoSetupDefaultsInternal_WithMSSQLAutoSetupDefaultsInternal_DoesntCreateTmpfsIfInCi()
    {
        // Arrange
        const string containerName = "MsSqlContainer-unit-test";
        const string dockerEndpoint = "tcp://127.0.0.1:2375";
        var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04");
        builder = builder.WithDockerEndpoint(dockerEndpoint);
        builder = builder.WithMSSQLAutoSetupDefaultsInternal(containerName, isCiRun: true, isWslDocker: false, useTmpfs: true);

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
    public void WithAutoSetupDefaultsInternal_WithMSSQLAutoSetupDefaultsInternal_DoesntCreateTmpfsIfInCiEvenIfSetup()
    {
        // Arrange
        const string containerName = "MsSqlContainer-unit-test";
        const string dockerEndpoint = "tcp://127.0.0.1:2375";
        var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04");
        builder = builder.WithDockerEndpoint(dockerEndpoint);
        builder = builder.WithMSSQLAutoSetupDefaultsInternal(containerName, isCiRun: true, isWslDocker: false, useTmpfs: true);

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
    public void WithAutoSetupDefaultsInternal_WithMSSQLAutoSetupDefaultsInternal_CreatesTmpfsIf()
    {
        // Arrange
        const string containerName = "MsSqlContainer-unit-test";
        const string dockerEndpoint = "tcp://127.0.0.1:2375";
        var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04");
        builder = builder.WithDockerEndpoint(dockerEndpoint);
        builder = builder.WithMSSQLAutoSetupDefaultsInternal(containerName, isCiRun: false, isWslDocker: true);

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

    [Theory]
    [InlineData(false, false, null, true)]  // Local, Native -> Tmpfs
    [InlineData(false, true, null, false)]  // Local, WSL -> No Tmpfs
    [InlineData(true, false, null, false)]  // CI -> No Tmpfs
    [InlineData(false, true, true, true)]   // Local, WSL, Explicit Override -> Tmpfs
    [InlineData(false, false, false, false)]// Local, Native, Explicit Disable -> No Tmpfs
    public void WithMSSQLAutoSetupDefaultsInternal_FlowLogic(bool isCiRun, bool isWslDocker, bool? useTmpfs, bool expectTmpfs)
    {
        // Arrange
        const string dockerEndpoint = "tcp://127.0.0.1:2375";
        var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04");

        // Act
        builder = builder
            .WithDockerEndpoint(dockerEndpoint)
            .WithMSSQLAutoSetupDefaultsInternal(ContainerName, isCiRun, isWslDocker, useTmpfs);
        var config = builder.Build().GetConfiguration();

        // Assert
        if (expectTmpfs)
            Assert.Contains(config.Mounts, m => m.Type.Type == MountType.Tmpfs.Type && m.Target == Constants.MsSQL.DefaultRestorationDataFilesPath);
        else
            Assert.DoesNotContain(config.Mounts, m => m.Type.Type == MountType.Tmpfs.Type);

        Assert.Contains(config.Mounts, m => m.Source == $"{ContainerName}-Restoration");
        
        var paramsMod = new CreateContainerParameters();
        foreach (var mod in config.ParameterModifiers) mod(paramsMod);
        Assert.Equal("root", paramsMod.User);
    }

    #endregion

    #region MySQL Coverage

    [Theory]
    [InlineData(false, false, null, true)]  // Local, Native -> Tmpfs
    [InlineData(false, true, null, false)]  // Local, WSL -> No Tmpfs
    [InlineData(true, false, null, false)]  // CI -> No Tmpfs
    [InlineData(false, true, true, true)]   // Local, WSL, Explicit Override -> Tmpfs
    [InlineData(false, false, false, false)]// Local, Native, Explicit Disable -> No Tmpfs
    public void WithMySQLAutoSetupDefaultsInternal_FlowLogic(bool isCiRun, bool isWslDocker, bool? useTmpfs, bool expectTmpfs)
    {
        // Arrange
        const string dockerEndpoint = "tcp://127.0.0.1:2375";
        var builder = new MySqlBuilder("mysql:8.0.44-debian");

        // Act
        builder = builder
            .WithDockerEndpoint(dockerEndpoint)
            .WithMySQLAutoSetupDefaultsInternal(isCiRun, isWslDocker, useTmpfs);
        var config = builder.Build().GetConfiguration();

        // Assert
        if (expectTmpfs)
        {
            Assert.Contains(config.Mounts, m => m.Target == Constants.MySQL.DefaultDbDataDirectory && m.Type.Type == MountType.Tmpfs.Type);
            
            var paramsMod = new CreateContainerParameters();
            foreach (var mod in config.ParameterModifiers) mod(paramsMod);
            Assert.Contains("seccomp=unconfined", paramsMod.HostConfig.SecurityOpt);
        }
        else
        {
            Assert.DoesNotContain(config.Mounts, m => m.Type.Type == MountType.Tmpfs.Type);
        }
    }

    #endregion

    #region MongoDB Coverage

    [Theory]
    [InlineData(false, false, null, true)]  // Local, Native -> Tmpfs
    [InlineData(false, true, null, false)]  // Local, WSL -> No Tmpfs
    [InlineData(true, false, null, false)]  // CI -> No Tmpfs
    [InlineData(false, true, true, true)]   // Local, WSL, Explicit Override -> Tmpfs
    [InlineData(false, false, false, false)]// Local, Native, Explicit Disable -> No Tmpfs
    public void WithMongoAutoSetupDefaultsInternal_FlowLogic(bool isCiRun, bool isWslDocker, bool? useTmpfs, bool expectTmpfs)
    {
        // Arrange
        const string dockerEndpoint = "tcp://127.0.0.1:2375";
        var builder = new MongoDbBuilder("mongo:6.0.27-jammy");
        const string path = "/tmp/test";

        // Act
        builder = builder
            .WithDockerEndpoint(dockerEndpoint)
            .WithMongoAutoSetupDefaultsInternal(path, isWslDocker, isCiRun, useTmpfs);
        var config = builder.Build().GetConfiguration();

        // Assert
        if (expectTmpfs)
            Assert.Contains(config.Mounts, m => m.Target == Constants.MongoDB.DefaultDbDataDirectory && m.Type.Type == MountType.Tmpfs.Type);

        Assert.Contains(config.Mounts, m => m.Source == path && m.Target == Constants.MongoDB.DefaultMigrationsDataPath);
        
        var parameters = new CreateContainerParameters();
        foreach (var modifier in config.ParameterModifiers)
        {
            modifier(parameters);
        }
        Assert.Equal("root", parameters.User);
    }

    #endregion

    #region Reuse Logic Coverage

    [Fact]
    public void WithAutoSetupDefaultsInternal_WhenNotCi_AppliesReuseSettings()
    {
        // Arrange
        var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04");
        const string endpoint = "tcp://127.0.0.1:2375";

        // Act
        builder = builder.WithAutoSetupDefaultsInternal(ContainerName, endpoint, isCiRun: false);
        var config = builder.Build().GetConfiguration();

        // Assert
        Assert.True(config.Reuse);
        Assert.Equal(ContainerName, config.Name);
        Assert.Equal($"{ContainerName}-reuse-hash", config.Labels["reuse-id"]);
    }

    [Fact]
    public void WithAutoSetupDefaultsInternal_WhenCi_SkipsReuseSettings()
    {
        // Arrange
        const string dockerEndpoint = "tcp://127.0.0.1:2375";
        var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04");

        // Act
        builder = builder
            .WithDockerEndpoint(dockerEndpoint)
            .WithAutoSetupDefaultsInternal(ContainerName, null, isCiRun: true);
        var config = builder.Build().GetConfiguration();

        // Assert
        Assert.False(config.Reuse);
        Assert.NotEqual(ContainerName, config.Name);
    }

    #endregion
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