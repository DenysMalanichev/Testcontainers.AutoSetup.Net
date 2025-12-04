using DotNet.Testcontainers.Containers;
using Testcontainers.Core.Abstractions;
using Testcontainers.MsSql;
using TestcontainersAutoSetup.Core.Implementation;

namespace TestcontainersAutoSetup.SqlServer.Implementation;

public class SqlServerSetup : IContainerSetup
{
    private readonly AutoSetupContainerBuilder _mainBuilder;
    private readonly MsSqlBuilder _msSqlBuilder = new MsSqlBuilder();

    private string? _migrationsPath;
    private string? _snapshotDirectory;

    public SqlServerSetup(AutoSetupContainerBuilder mainBuilder)
    {
        _mainBuilder = mainBuilder;
        // TODO move docker endpoint to a common class
        if(mainBuilder.DockerEndpoint != null)
            _msSqlBuilder = _msSqlBuilder.WithDockerEndpoint(mainBuilder.DockerEndpoint);
    }

    
    public AutoSetupContainerBuilder And()
    {
        return _mainBuilder;
    }

    public async Task<IContainer> BuildAndInitializeAsync()
    {
        var container = _msSqlBuilder.Build();
        await container.StartAsync();

        // Here you would instantiate and run the orchestrator from our previous discussion
        // var schemaManager = new LiquibaseScriptManager(...);
        // var snapshotter = new MySqlSnapshotter();
        // var orchestrator = new DatabaseContainerOrchestrator(container, schemaManager, snapshotter, ...);
        // await orchestrator.InitializeAsync();

        return container;
    }
}