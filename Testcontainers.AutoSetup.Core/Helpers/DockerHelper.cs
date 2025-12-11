using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Testcontainers.AutoSetup.Core.Helpers;

/// <summary>
/// Provides utility methods for discovering the Docker daemon's connection details.
/// This class handles specific networking quirks required to connect to Docker 
/// running inside WSL2 on Windows, while providing safe defaults for other platforms.
/// </summary>
public static class DockerHelper
{
    private const string _commonCiEnvVar = "CI";
    private static readonly ImmutableArray<string> specificCiVars = ImmutableArray.Create
        (
            "TF_BUILD",           // Azure DevOps
            "TEAMCITY_VERSION",   // TeamCity
            "JENKINS_URL",        // Jenkins
            "BAMBOO_PLANKEY",     // Bamboo
            "BITBUCKET_COMMIT",   // Bitbucket Pipelines
            "HEROKU_TEST_RUN_ID", // Heroku CI
            "CODEBUILD_BUILD_ID"  // AWS CodeBuild
        );

    private static int _dockerPort = 2375;
    private static string? _customDockerEndpoint = null;
    private static readonly Lazy<string> _dockerHostAddressLazy = new(GetDockerHostAddress);
    private static Func<bool>? _customCiCheck = null!;

    /// <summary>
    /// Gets the resolved IP address of the Docker host.
    /// <para>
    /// This value is lazily evaluated. On Windows, it attempts to resolve the WSL2 IP; 
    /// on non-Windows platforms or in case of resolution failure, it defaults to "localhost".
    /// </para>
    /// </summary>
    public static string DockerHostAddress => _dockerHostAddressLazy.Value;

    /// <summary>
    /// Sets the TCP port used to connect to the Docker daemon. 
    /// The default value is <c>2375</c>.
    /// </summary>
    /// <param name="port">The port number the Docker daemon is listening on.</param>
    public static void SetDockerPort(int port)
    {
        _dockerPort = port;
    }

    /// <summary>
    /// Manually sets the full docker endpoint string (e.g., "tcp://1.2.3.4:5555" or "unix:///var/run/docker.sock").
    /// Setting this overrides all auto-discovery logic.
    /// </summary>
    /// <param name="dockerEndpoint">The full connection string.</param>
    public static void SetCustomDockerEndpoint(string dockerEndpoint)
    {
        _customDockerEndpoint = dockerEndpoint;
    }

    /// <summary>
    /// Sets custom logic to determine if the code is running in a CI environment.
    /// This allows overriding or extending the built-in CI detection mechanisms.
    /// </summary>
    /// <param name="customCiCheck">A function that returns <c>true</c> if in CI, otherwise <c>false</c>.</param>
    public static void SetCustomCiCheck(Func<bool> customCiCheck)
    {
        _customCiCheck = customCiCheck;
    }

    /// <summary>
    /// Constructs the full connection string for the Docker daemon.
    /// </summary>
    /// <remarks>
    /// <para>The resolution order is:</para>
    /// <list type="number">
    /// <item>Check if a custom endpoint was manually set via <see cref="SetCustomDockerEndpoint"/>.</item>
    /// <item>Check if running in CI; if so, return <c>null</c> (auto-discovery).</item>
    /// <item>Check for Windows Named Pipe; if exists, return <c>null</c> (auto-discovery).</item>
    /// <item>Check for local Unix Socket; if exists, return <c>unix:///var/run/docker.sock</c>.</item>
    /// <item>Fallback: Resolve WSL2 IP and return TCP string.</item>
    /// </list>
    /// </remarks>
    /// <returns>
    /// A string in the format <c>tcp://{ip}:{port}</c>, <c>unix://...</c>, or <c>null</c>.
    /// </returns>
    public static string? GetDockerEndpoint()
    {
        // 1. Priority: Custom Manual Endpoint
        if (_customDockerEndpoint is not null)
        {
            return _customDockerEndpoint;
        }

        // 2. Priority: CI Environment (Trust Testcontainers/Environment)
        if (IsCiRun())
        {
            return null;
        }

        // 3. Priority: Windows Named Pipe (Standard Docker Desktop)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(@"\\.\pipe\docker_engine"))
        {
            return null;
        }

        // 4. Priority: Local Unix Socket (Linux/Mac Standard)
        if (File.Exists("/var/run/docker.sock"))
        {
            return "unix:///var/run/docker.sock";
        }

        // 5. Priority: TCP Connection (WSL2 Fallback)
        return $"tcp://{DockerHostAddress}:{_dockerPort}";
    }

    /// <summary>
    /// Determines if the current execution is occurring within a CI/CD environment.
    /// </summary>
    public static bool IsCiRun()
    {
        if (_customCiCheck is not null)
        {
            return _customCiCheck();
        }

        var conversionResult = bool.TryParse(
            Environment.GetEnvironmentVariable(_commonCiEnvVar), out bool env);
        if (conversionResult && env)
        {
            return true;
        }

        foreach (var variable in specificCiVars)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variable)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves the IP address of the Docker daemon.
    /// </summary>
    /// <remarks>
    /// On Windows, Docker Desktop often runs inside a WSL2 VM. To connect via TCP, 
    /// we must obtain the internal IP of that VM using <c>wsl hostname -I</c>. 
    /// </remarks>
    /// <returns>The IP address string (e.g., "172.x.x.x" or "localhost").</returns>
    private static string GetDockerHostAddress()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "localhost";
        }

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = "hostname -I",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return "localhost";

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var ip = output.Trim().Split(' ').FirstOrDefault();

            return string.IsNullOrWhiteSpace(ip) ? "localhost" : ip;
        }
        catch
        {
            return "localhost";
        }
    }
}