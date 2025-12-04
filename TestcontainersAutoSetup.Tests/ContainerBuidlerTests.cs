using System.ComponentModel;
using DotNet.Testcontainers.Containers;
using Testcontainers.MySql;
using TestcontainersAutoSetup.Core.Implementation;
using TestcontainersAutoSetup.MySql.Implementation;
using TestcontainersAutoSetup.SqlServer.Implementation;

namespace TestcontainersAutoSetup.Tests;

public class ContainerBuidlerTests
{
    [Fact]
    public async Task ContainerBuilder_CreatesMySqlContainer()
    {
        var builder = new AutoSetupContainerBuilder(dockerEndpoint: "tcp://localhost:2375");
        var mySqlContainer = await builder.CreateMySqlContainer()
            .WithDatabase("TestDb")
            .BuildAndInitializeAsync();

        await Task.Delay(10_000);
        Assert.NotEqual(mySqlContainer.CreatedTime, default);
        Assert.Equal(TestcontainersStates.Running, mySqlContainer.State);
    }

    [Fact]
    public async Task ContainerBuilder_CreatesSqlServerContainers()
    {
        var builder = new AutoSetupContainerBuilder(dockerEndpoint: "tcp://localhost:2375");
        var sqlServerContainer = await builder.CreateSqlServerContainer()
            // .WithDatabase("TestDb")
            .BuildAndInitializeAsync();

        await Task.Delay(10_000);
        Assert.NotEqual(default, sqlServerContainer.CreatedTime);
        Assert.Equal(TestcontainersStates.Running, sqlServerContainer.State);
    }

    [Fact]
    public async Task ContainerBuilder_CreatesBothSqlServerAndMySqlContainers()
    {
        var builder = new AutoSetupContainerBuilder(dockerEndpoint: "tcp://localhost:2375");
        var containers = await builder.CreateMySqlContainer()
            .WithDatabase("TestDb")
            .And()
            .CreateSqlServerContainer()
            .And()
            .BuildAsync();

        var sqlServerContainer = containers[0];
        var mySqlContainer = containers[1];

        await Task.Delay(10_000);
        Assert.NotEqual(mySqlContainer.CreatedTime, default);
        Assert.Equal(TestcontainersStates.Running, mySqlContainer.State);
        await Task.Delay(10_000);
        Assert.NotEqual(default, sqlServerContainer.CreatedTime);
        Assert.Equal(TestcontainersStates.Running, sqlServerContainer.State);
    }
}