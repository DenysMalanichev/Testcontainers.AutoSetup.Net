# Testcontainers.AutoSetup.Net ![DotnetVersion](https://img.shields.io/badge/version-10.0-orange?style=flat&logo=.NET) [![CI](https://github.com/DenysMalanichev/TestcontaienrsAutoSetup/actions/workflows/ci.yaml/badge.svg)](https://github.com/DenysMalanichev/TestcontaienrsAutoSetup/actions/workflows/ci.yaml) ![Code Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/DenysMalanichev/014183e2f85a69201c0d39313d2065eb/raw/coverage.json)

A lightweight library to automate database setup, seeding and reset workflows for integration tests that use ***Testcontainers.NET***. It provides a pluggable strategy-based model for preparing database state before tests run, restoring snapshots, and seeding data (including Entity Framework-specific helpers). Designed to integrate with .NET test frameworks and container-based test environments.

Testcontainers.AutoSetup.Net provides the functionality of automatic migrations of your  EF Core, Liquibase, Flyway and raw SQL scripts.

## Support
<table>
    <tr>
        <th>Database</th>
        <th>Support</th>
        <th>Container type</th>
        <th>Restoration strategy*</th>
        <th>Supported schema management tools</th>
    </tr>
    <tr>
        <td>MS SQL</td>
        <td> ✅ </td>
        <td>Both with official Testcontainers MsSqlBuilder and generic builds</td>
        <td>From snapshot</td>
        <td>EF Core <br> Raw SQL</td>
    </tr>
    <tr>
        <td>MySQL</td>
        <td> ✅ </td>
        <td>Both with official Testcontainers MySqlBuilder and generic builds</td>
        <td>From "golden state DB"</td>
        <td>EF Core <br> Raw SQL</td>
    </tr>
    <tr>
        <td>MongoDB</td>
        <td> ✅ </td>
        <td>Both with official Testcontainers MongoDbBuilder and generic builds</td>
        <td>Dump</td>
        <td> Raw data files </td>
    </tr>
    <tr>
        <td>PostreSQL</td>
        <td>❌ (Comming soon)</td>
        <td> - </td>
        <td>Filesystem Snapshot</td>
        <td> - </td>
    </tr>
    <tr>
        <td>Oracle</td>
        <td>❌ (Comming soon)</td>
        <td> - </td>
        <td>Snapshot Standby</td>
        <td> - </td>
    </tr>
    <tr>
        <td colspan="5" style="text-align: center; vertical-align: middle;"><strong>Other containers comming soon</strong></td>
    </tr>
    <tr>
        <td>Reddis</td>
        <td>❌ (Comming soon)</td>
        <td> - </td>
        <td> - </td>
        <td> - </td>
    </tr>
    <tr>
        <td>Elasticksearch</td>
        <td>❌ (Comming soon)</td>
        <td> - </td>
        <td> - </td>
        <td> - </td>
    </tr>
    <tr>
        <td>Kafka</td>
        <td>❌ (Comming soon)</td>
        <td> - </td>
        <td> - </td>
        <td> - </td>
    </tr>
</table>

> *restoration time depends on a DB size - see benchmarks

## Benchmark results
<table>
    <tr>
        <th>Database</th>
        <th>Restore strategy</th>
        <th colspan="6" style="text-align: center; vertical-align: middle;">Time (ms) to restore N rows</th>
        <th>Note</th>
    </tr>
    <tr>
        <td></td>
        <td></td>
        <td>1</td>
        <td>10</td>
        <td>100</td>
        <td>1.000</td>
        <td>10.000</td>
        <td>50.000</td>
        <td></td>
    </tr>
    <tr>
        <td>MS SQL</td>
        <td>Snapshot</td>
        <td>305.8</td>
        <td>315.9</td>
        <td>303.9</td>
        <td>335.1</td>
        <td>344.0</td>
        <td>332.3</td>
        <td>All fluctautions are neglictable and occure only due to the system internal proccesses. The restoration always takes ~300ms </td>
    </tr>
    <tr>
        <td>MySQL</td>
        <td>Golden State DB</td>
        <td>5.637</td>
        <td>5.432</td>
        <td>7.358</td>
        <td>27.044</td>
        <td>118.545</td>
        <td>548.7</td>
        <td>O(n) operation</td>
    </tr>
    <tr>
        <td>MongoDB</td>
        <td>mongorestore + golden state</td>
        <td>125.3</td>
        <td>130.0</td>
        <td>128.3</td>
        <td>133.7</td>
        <td>275.6</td>
        <td>1129.1</td>
        <td>O(n) operation</td>
    </tr>
</table>

> See the Testcontainers.AutoSetup.Benchmarks project for more info

## Usage
You are free to use the default syntax of Testcontainers.NET to crate and configure a container as you need. In order for AutoSetup to work correctly it utilizes the reusable functionality with some aditional configuration, available in `WithAutoSetupDefaults(containerName)` extension method. It configures the container with required params like `.WithReuse(true)`, adds reuse labels, configures a required user and sets up mounts (Volume for snapshots and Tmpfs for DB internal data). For a user it is enough to simply call the method:
```CSharp
var msSqlContainer = new MsSqlBuilder();
var container = builder
    .WithAutoSetupDefaults(containerName: "MsSQL-testcontainer")
    .WithPassword("#AdminPass123")
    .Build();
await msSqlContainer.StartAsync(); 
```
> NOTE: for DBs that use "golden state DB" restore strategy a default user must have rights to create databases. (e.g. use `.WithUsername("root")` for MySQL,  or similar, for supported builders).
 
With a running container we can set up the Database(s). To do it we create an `DbSetup` record, in this case an `EfDbSetup`, since we are going to use EF Core migrations. This record defines the DB configuration - a DB type, name, either an absolute or a relative path to migrations folder - `MigrationsPath` and a factory method to instantiate a `DbContext`: 
```CSharp
private static EfDbSetup MsSqlDbSetup => new(
    dbType: DbType.MsSQL,
    dbName: "CatalogTest", 
    contextFactory: connString => new CatalogContext(
        new DbContextOptionsBuilder<CatalogContext>()
        .UseSqlServer(connString)
        .Options),
    migrationsPath: "./IntegrationTests/Migrations",
);
```
**Existing DB setup records:**
<table>
    <tr>
        <th>Restoration type</th>
        <th>Corresponding DbSetup record</th>
    </tr>
    <tr>
        <td>From Entity Framework mmigrations</td>
        <td> EfDbSetup </td>
    </tr>
    <tr>
        <td>From Raw SQL Files</td>
        <td> RawSqlDbSetup </td>
    </tr>
    <tr>
        <td>From Raw MongoDB data Files</td>
        <td> RawMongoDbSetup </td>
    </tr>
</table>

Next we register this DB setup and container within the `TestEnvironment`. You also have to provide an instance of DB connection factory, which implements an `IDbConnectionFactory`. This allows you to configure the connection used to set up DB. Note the `DbSetupStrategyBuilder` used to configurethe future strategy, identifying a seeder and restorer, specific for a EF Core and MS SQL database:
```CSharp
TestEnvironment.RegisterDbSetupStrategy(
    new DbSetupStrategyBuilder(
        MsSqlContainer_SpecificBuilder_EfDbSetup,
        MsSqlContainerFromSpecificBuilder,
        Logger!) // optional ILogger instance, can be omitted
    .WithEfSeeder()
    .WithMsSqlRestorer(new MsSqlDbConnectionFactory())
    .Build());
```
Optionally, you can provide an `ILogger` instance. When the logger is not specified, `Testcontainers.AutoSetup` would reuse a default `Testcontainers` logger.

In the same way we can set up multiple DBs within one container.

Finally, by calling an initialize method all registered DBs would be created and populated.
```CSharp
await TestEnvironment.InitializeAsync();
```

Each test class must be decorated with `[DbReset]` attribute. For example:
```CSharp
[DbReset]
[Trait("Category", "Integration")]
[Collection(nameof(ParallelIntegrationTestsCollection))]
public class MsSqlRestorationTests(ContainersFixture fixture) : IntegrationTestsBase(fixture)
```
The attribute has one argument `ResetScope` with two possible values: `None` and `BeforeExecution`, with `BeforeExecution` being a default value. Classes without this attribute would be reset automatically. Nevertheless, it is recommended to mark classes explicitly.

### Test environments
The Testcontainers.AutoSetup.Net library is designed to work with any test framework, like xUnit, NUnit, or MSTest. It provides an abstract `TestEnvironment` class which encapsulates all containers that are required to be reset, as well as executes the Init and Reset operations for each of them in parallel. The class also provides abstract methods `ConfigureSetupAsync` and `ResetEnvironmentAsync`. The intended usage of this class would be a creation a common class (e.g. `GlobalTestSetup`) which inherits from the `TestEnvironment`, creates and configures containers before the test.
Then this `GlobalTestSetup` may be used as a property in a base class for integration tests. In case of xUnit:
```CSharp
public abstract class IntegrationTestsBase : IAsyncLifetime
{
    public readonly GlobalTestSetup Setup;

    public IntegrationTestsBase(ContainersFixture fixture)
    {
        Setup = fixture.Setup;
    }

    public async Task InitializeAsync()
    {
        await Setup.ResetEnvironmentAsync(this.GetType());
    }

    public async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }
}
```

### Logging
By default **Testcontainers.AutoSetup.Net** library utilizes a default `ConsoleLogger` from **Testcontainers.Net**. Nevertheless, just as with **Testcontainers.Net** users are able to provide a custom logger into `GenericTestBase(ILogger? logger)` constructor.

## Docker under WSL
In case your Docker is running under WSL2 do not forget to 
expose the docker port:
``` bash
sudo mkdir -p /etc/systemd/system/docker.service.d
sudo vim /etc/systemd/system/docker.service.d/override.conf

# add the below to the override.conf file
[Service]
ExecStart=
ExecStart=/usr/bin/dockerd --host=tcp://0.0.0.0:2375 --host=unix:///var/run/docker.sock
```

## EnvironmentHelper
**EnvironmentHelper** is a static utility class designed to simplify Docker connectivity configuration for Testcontainers.AutoSetup.

It automatically handles networking quirks—specifically resolving the correct IP address when running Docker inside WSL2 on Windows—while providing safe defaults for Linux, macOS, and CI/CD environments.

Features
Auto-Detect WSL2 IP: Automatically resolves the internal IP of the WSL2 VM using 
```bash 
wsl hostname -I.
```

CI/CD Aware: Detects if code is running in a CI pipeline (GitHub Actions, Azure DevOps, Jenkins, etc.) and disables manual TCP resolution, letting Testcontainers fall back to Unix sockets or Named Pipes.

* Extensible: Allows injection of custom logic for CI detection.

* Zero Config: Works out-of-the-box with standard Docker defaults (Port 2375).

### API Reference

#### `GetDockerEndpoint()`
Returns the full TCP connection string (e.g., `tcp://172.18.xxx.xxx:2375`). This is a string? - The endpoint URL, or null if a CI environment is detected.

#### `SetCustomDockerEndpoint(string dockerEndpoint)`
**Highest Priority.** Manually sets the Docker connection string, bypassing all other discovery logic.

#### `SetDockerPort(int port)`
Overrides the default Docker daemon port default: 2375.

#### `SetCustomCiCheck(Func<bool> customCiCheck)`
Injects custom logic to determine if the current environment is a CI environment.

Useful if you use a proprietary CI tool or need to force a specific behavior based on custom environment variables or file existence.

---

#### Logic Flow

1.  **Custom Endpoint:** If set via `SetCustomDockerEndpoint`, use it.
2.  **CI Check:** If running in CI, return `null` (let Testcontainers auto-discover).
3.  **Windows Pipe:** If `\\.\pipe\docker_engine` exists, return `null`.
4.  **Unix Socket:** If `/var/run/docker.sock` exists, return `unix:///var/run/docker.sock`.
5.  **Fallback:** Calculate WSL IP and return `tcp://{WSL_IP}:{Port}`.

#### Usage Examples
1. Default Usage (Zero Config)
In most local development scenarios (especially on Windows with WSL2), you simply use the endpoint getter. If running locally, it returns the TCP address; if in CI, it returns null.

```C#
using Testcontainers.AutoSetup.Core.Helpers;
using DotNet.Testcontainers.Builders;

var endpoint = EnvironmentHelper.GetDockerEndpoint();

var builder = new ContainerBuilder()
    .WithImage("postgres:15-alpine")
    // If endpoint is null (CI), Testcontainers uses its default discovery
    // If endpoint is set (Local), it forces the TCP connection
    .WithDockerEndpoint(endpoint) 
    .Build();
```
2. using a Custom Docker Port
If your team uses a non-standard port (e.g., 5000) or the secure Docker port (2376), configure it once at the start of your test run (e.g., in a Global Setup or Assembly Fixture).

```C#
// GlobalSetup.cs
public class GlobalSetup
{
    public GlobalSetup()
    {
        // Override default 2375
        EnvironmentHelper.SetDockerPort(5000);
    }
}
```
3. Custom CI Detection Logic
If you need to detect a specific custom environment (e.g., a local containerized build agent) that isn't covered by standard environment variables:

```C#
// Force CI mode if a specific file exists on disk
EnvironmentHelper.SetCustomCiCheck(() => File.Exists("/.dockerenv"));

// OR: Force CI mode based on a custom company variable
EnvironmentHelper.SetCustomCiCheck(() => Environment.GetEnvironmentVariable("MY_COMPANY_BUILD_AGENT") == "true");
```
#### How It Works
WSL2 Resolution
On Windows, Docker Desktop often runs inside a hidden WSL2 VM. Validating localhost often fails for TCP connections. EnvironmentHelper executes the command wsl hostname -I to fetch the actual IP address of the VM bridging the connection.

CI Detection strategy
The library checks for CI environments in the following order:

Custom Check: Any logic provided via SetCustomCiCheck.

Standard Standard: Checks if CI=true (GitHub Actions, GitLab, Travis).

Vendor Specific: Checks for existence of variables like TF_BUILD (Azure), JENKINS_URL, TEAMCITY_VERSION, etc.

If any of these are true, GetDockerEndpoint() returns null. This is the desired behavior, as CI agents typically mount the Docker socket (/var/run/docker.sock) directly, which Testcontainers handles automatically without needing a TCP address.

## DB Restore Logic
### "Golden state"
On startup, the initial database is created and migrated. Immediately after, an exact replica named `{DBName}_golden_state` is created to serve as the reference. To reset after each test, the target database is truncated and repopulated using data directly from this golden state copy. If migration file changes are detected, both databases are dropped and recreated from scratch.

> NOTE: if you experience delays (5-10 seconds) between tests with MS SQL DB - most likely it is an issue with Reverse DNS lookup. While some DBs allow skipping Reverse DNS lookup, like `--skip-name-resolve` flag in MySQL, some, like MS SQL -do not. The easiest workaround here is to register the IP of the WSL machine in Windows hosts file.
