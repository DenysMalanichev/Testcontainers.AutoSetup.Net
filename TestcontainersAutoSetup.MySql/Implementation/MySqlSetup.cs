using DotNet.Testcontainers.Containers;
using Testcontainers.Core.Abstractions;
using Testcontainers.MySql;
using TestcontainersAutoSetup.Core.Abstractions;
using TestcontainersAutoSetup.Core.Implementation;

namespace TestcontainersAutoSetup.MySql.Implementation;
public class MySqlSetup : IContainerSetup
{
    private readonly AutoSetupContainerBuilder _mainBuilder;
    private readonly MySqlBuilder _mySqlBuilder = new MySqlBuilder();

    private string? _migrationsPath;
    private string? _snapshotDirectory;

    public MySqlSetup(AutoSetupContainerBuilder mainBuilder)
    {
        _mainBuilder = mainBuilder;
        if(mainBuilder.DockerEndpoint != null)
            _mySqlBuilder = _mySqlBuilder.WithDockerEndpoint(mainBuilder.DockerEndpoint);
    }

    public MySqlSetup WithMigrationsPath(string path)
    {
        _migrationsPath = path;
        return this;
    }

    public MySqlSetup WithPersistentSnapshotDirectory(string path)
    {
        _snapshotDirectory = path;
        _mySqlBuilder.WithBindMount(path, "/snapshots");
        return this;
    }

    public MySqlSetup WithDatabase(string db)
    {
        //_mySqlBuilder.WithDatabase(db);
        return this;
    }

    public AutoSetupContainerBuilder And()
    {
        return _mainBuilder;
    }

    // The build logic required by the interface
    public async Task<IContainer> BuildAndInitializeAsync()
    {
        var container = _mySqlBuilder.Build();
        await container.StartAsync();

        // Here you would instantiate and run the orchestrator from our previous discussion
        // var schemaManager = new LiquibaseScriptManager(...);
        // var snapshotter = new MySqlSnapshotter();
        // var orchestrator = new DatabaseContainerOrchestrator(container, schemaManager, snapshotter, ...);
        // await orchestrator.InitializeAsync();

        return container;
    }
}
