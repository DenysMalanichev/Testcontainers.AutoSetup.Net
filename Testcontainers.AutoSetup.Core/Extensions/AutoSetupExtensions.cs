using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Helpers;

namespace Testcontainers.AutoSetup.Core.Extensions;

public static class AutoSetupExtensions
{
    private const string RestorationPath = "/var/opt/mssql/Restoration";
    private const string DataPath = "/var/opt/mssql/data";

    /// <summary>
    /// Configures the Testcontainer builder with the essential settings required by the AutoSetup library.
    /// <para>
    /// This includes configuring Docker endpoints, volume mounts for database restoration, 
    /// file system permissions, and reuse strategies based on the current execution environment (CI vs. Local).
    /// </para>
    /// </summary>
    /// <typeparam name="TBuilder">The type of the container builder (e.g., <see cref="MsSqlBuilder"/>).</typeparam>
    /// <typeparam name="TContainer">The type of the container being built.</typeparam>
    /// <typeparam name="TConfiguration">The configuration entity for the container.</typeparam>
    /// <param name="builder">The builder instance to configure.</param>
    /// <param name="containerName">
    /// A unique identifier used to generate the reuse hash. 
    /// This ensures that the container is reused across test runs when running locally, 
    /// allowing for the "Snapshot and Restore" optimization.
    /// </param>
    /// <returns>The configured builder instance for method chaining.</returns>
    /// <remarks>
    /// <strong>Applied Configurations:</strong>
    /// <list type="bullet">
    /// <item>
    ///     <term>Docker Endpoint</term>
    ///     <description>Automatically detects and sets the Docker endpoint via <c>EnvironmentHelper</c>.</description>
    /// </item>
    /// <item>
    ///     <term>Local Execution</term>
    ///     <description>
    ///     When not running in CI:
    ///     <br/> - Enables <strong>Container Reuse</strong> using the provided <paramref name="containerName"/>.
    ///     <br/> - Mounts the restoration volume at <c>/var/opt/mssql/Restoration</c>.
    ///     <br/> - Mounts a tmpfs volume at <c>/var/opt/mssql/data</c> for performance.
    ///     <br/> - Sets the container user to <strong>root</strong> to ensure volume write permissions.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term>CI Execution</term>
    ///     <description>Skips reuse strategies and volume mounting to ensure fresh containers for isolated build pipelines.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public static TBuilder WithAutoSetupDefaults<TBuilder, TContainer, TConfiguration>(
        this ContainerBuilder<TBuilder, TContainer, TConfiguration> builder, 
        string containerName)
        where TBuilder : ContainerBuilder<TBuilder, TContainer, TConfiguration>
        where TContainer : IContainer
        where TConfiguration : IContainerConfiguration
    {
        var dockerEndpoint = EnvironmentHelper.GetDockerEndpoint();
        var isCiRun = EnvironmentHelper.IsCiRun();

        return WithAutoSetupDefaultsInternal(builder, containerName, dockerEndpoint, isCiRun);
    }

    // "Internal Seam" 
    internal static TBuilder WithAutoSetupDefaultsInternal<TBuilder, TContainer, TConfiguration>(
        this ContainerBuilder<TBuilder, TContainer, TConfiguration> builder, 
        string containerName,
        string? dockerEndpoint,
        bool isCiRun)
        where TBuilder : ContainerBuilder<TBuilder, TContainer, TConfiguration>
        where TContainer : IContainer
        where TConfiguration : IContainerConfiguration
    {
        // 1. Auto-detect Docker Endpoint
        if (dockerEndpoint != null)
        {
            builder = builder.WithDockerEndpoint(dockerEndpoint);
        }

        // 2. Apply "AutoSetup" Logic (CI checks, Reuse, Volumes)
        if (!isCiRun)
        {
            builder = builder
                .WithReuse(true)
                .WithName(containerName)
                .WithLabel("reuse-id", $"{containerName}-reuse-hash")
                .WithVolumeMount($"{containerName}-Restoration", RestorationPath, AccessMode.ReadWrite)
                .WithTmpfsMount(DataPath, AccessMode.ReadWrite)
                .WithCreateParameterModifier(config => 
                { 
                    // This works for any container that respects the User param
                    config.User = "root"; 
                });
        }

        // Cast back to the specific TBuilder type to keep the fluent API working
        return (TBuilder)builder;   
    }
}