# TestcontaienrsAutoSetup [![CI](https://github.com/DenysMalanichev/TestcontaienrsAutoSetup/actions/workflows/ci.yaml/badge.svg)](https://github.com/DenysMalanichev/TestcontaienrsAutoSetup/actions/workflows/ci.yaml)

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

## DockerHelper
**DockerHelper** is a static utility class designed to simplify Docker connectivity configuration for Testcontainers.AutoSetup.

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

var endpoint = DockerHelper.GetDockerEndpoint();

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
        DockerHelper.SetDockerPort(5000);
    }
}
```
3. Custom CI Detection Logic
If you need to detect a specific custom environment (e.g., a local containerized build agent) that isn't covered by standard environment variables:

```C#
// Force CI mode if a specific file exists on disk
DockerHelper.SetCustomCiCheck(() => File.Exists("/.dockerenv"));

// OR: Force CI mode based on a custom company variable
DockerHelper.SetCustomCiCheck(() => Environment.GetEnvironmentVariable("MY_COMPANY_BUILD_AGENT") == "true");
```
#### How It Works
WSL2 Resolution
On Windows, Docker Desktop often runs inside a hidden WSL2 VM. Validating localhost often fails for TCP connections. DockerHelper executes the command wsl hostname -I to fetch the actual IP address of the VM bridging the connection.

CI Detection strategy
The library checks for CI environments in the following order:

Custom Check: Any logic provided via SetCustomCiCheck.

Standard Standard: Checks if CI=true (GitHub Actions, GitLab, Travis).

Vendor Specific: Checks for existence of variables like TF_BUILD (Azure), JENKINS_URL, TEAMCITY_VERSION, etc.

If any of these are true, GetDockerEndpoint() returns null. This is the desired behavior, as CI agents typically mount the Docker socket (/var/run/docker.sock) directly, which Testcontainers handles automatically without needing a TCP address.