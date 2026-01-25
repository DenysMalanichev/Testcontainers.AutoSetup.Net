using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Helpers;

namespace Testcontainers.AutoSetup.Core.Extensions;

public static class AutoSetupExtensions
{

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
    public static TBuilder WithMSSQLAutoSetupDefaults<TBuilder, TContainer, TConfiguration>(
        this ContainerBuilder<TBuilder, TContainer, TConfiguration> builder, 
        string containerName, bool? useTmpfs = null)
        where TBuilder : ContainerBuilder<TBuilder, TContainer, TConfiguration>
        where TContainer : IContainer
        where TConfiguration : IContainerConfiguration
    {
        var commonSetup = WithAutoSetupReuseDefaults(builder, containerName);
        var isCiRun = EnvironmentHelper.IsCiRun();
        var isWslDocker = EnvironmentHelper.IsWslDocker();
        return commonSetup.WithMSSQLAutoSetupDefaultsInternal(containerName, isCiRun, isWslDocker, useTmpfs);
    }

    internal static TBuilder WithMSSQLAutoSetupDefaultsInternal<TBuilder, TContainer, TConfiguration>(
        this ContainerBuilder<TBuilder, TContainer, TConfiguration> builder, 
        string containerName, bool isCiRun, bool isWslDocker, bool? useTmpfs = null)
        where TBuilder : ContainerBuilder<TBuilder, TContainer, TConfiguration>
        where TContainer : IContainer
        where TConfiguration : IContainerConfiguration
    {
        // If execution is not in CI and the Docker engine is not running under WSL Tmpfs is created even without explicit TRUE
        if(!isCiRun && ((useTmpfs is null && !isWslDocker) || (useTmpfs is not null && useTmpfs.Value == true)))
        {
            builder = builder
                .WithTmpfsMount(Constants.MsSQL.DefaultRestorationDataFilesPath, AccessMode.ReadWrite);
        }

        return builder
            .WithVolumeMount($"{containerName}-Restoration", Constants.MsSQL.DefaultRestorationStateFilesPath, AccessMode.ReadWrite)
            .WithVolumeMount($"{containerName}-Secrets", "/var/opt/mssql/secrets", AccessMode.ReadWrite)
            .WithCreateParameterModifier(config =>
            {
                config.User = "root";
            });
    }

    /// <summary>
    /// Configures the Testcontainer builder with the essential settings required by the AutoSetup library.
    /// <para>
    /// This includes configuring Docker endpoints, file system permissions, 
    /// and reuse strategies based on the current execution environment (CI vs. Local).
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
    ///     <br/> - Enables <strong>Container Reuse</strong> using the provided <paramref name="containerName"/>,
    ///     utilizing a reuse hash.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term>CI Execution</term>
    ///     <description>Skips reuse strategies to ensure fresh containers for isolated build pipelines.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public static TBuilder WithMySQLAutoSetupDefaults<TBuilder, TContainer, TConfiguration>(
        this ContainerBuilder<TBuilder, TContainer, TConfiguration> builder, 
        string containerName, bool? useTmpfs = null)
        where TBuilder : ContainerBuilder<TBuilder, TContainer, TConfiguration>
        where TContainer : IContainer
        where TConfiguration : IContainerConfiguration
    {
        var isCiRun = EnvironmentHelper.IsCiRun();
        var isWslDocker = EnvironmentHelper.IsWslDocker();
        return builder.WithAutoSetupReuseDefaults(containerName)
                      .WithMySQLAutoSetupDefaultsInternal(isCiRun, isWslDocker, useTmpfs);
    }

    internal static TBuilder WithMySQLAutoSetupDefaultsInternal<TBuilder, TContainer, TConfiguration>(
        this ContainerBuilder<TBuilder, TContainer, TConfiguration> builder,
            bool isCiRun, bool isWslDocker, bool? useTmpfs = null)
        where TBuilder : ContainerBuilder<TBuilder, TContainer, TConfiguration>
        where TContainer : IContainer
        where TConfiguration : IContainerConfiguration
    {
        // If execution is not in CI and the Docker engine is not running under WSL Tmpfs is created even without explicit TRUE
        if(!isCiRun && ((useTmpfs is null && !isWslDocker) || (useTmpfs is not null && useTmpfs.Value == true)))
        {
            builder = builder.WithTmpfsMount(Constants.MySQL.DefaultDbDataDirectory, AccessMode.ReadWrite)
            .WithCreateParameterModifier(modifier =>
            {
                modifier.HostConfig ??= new Docker.DotNet.Models.HostConfig();
                modifier.HostConfig.SecurityOpt ??= new List<string>();
                
                // This is the "Master Key" for syscall permissions
                modifier.HostConfig.SecurityOpt.Add("seccomp=unconfined");
            });
        }
        return (TBuilder)builder;
    }

    /// <summary>
    /// Configures the Testcontainer builder with the essential settings required by the AutoSetup library.
    /// <para>
    /// This includes configuring Docker endpoints, file system permissions, 
    /// and reuse strategies based on the current execution environment (CI vs. Local).
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
    ///     <br/> - Enables <strong>Container Reuse</strong> using the provided <paramref name="containerName"/>,
    ///     utilizing a reuse hash.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term>CI Execution</term>
    ///     <description>Skips reuse strategies to ensure fresh containers for isolated build pipelines.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public static TBuilder WithAutoSetupReuseDefaults<TBuilder, TContainer, TConfiguration>(
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

    internal static TBuilder WithAutoSetupDefaultsInternal<TBuilder, TContainer, TConfiguration>(
        this ContainerBuilder<TBuilder, TContainer, TConfiguration> builder, 
        string containerName, string? dockerEndpoint, bool isCiRun)
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
                .WithLabel("reuse-id", $"{containerName}-reuse-hash");
        }

        // Cast back to the specific TBuilder type to keep the fluent API working
        return (TBuilder)builder;
    }

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
    public static TBuilder WithMongoAutoSetupDefaults<TBuilder, TContainer, TConfiguration>(
        this ContainerBuilder<TBuilder, TContainer, TConfiguration> builder, 
            string containerName, string migrationsPath, bool? useTmpfs = null)
        where TBuilder : ContainerBuilder<TBuilder, TContainer, TConfiguration>
        where TContainer : IContainer
        where TConfiguration : IContainerConfiguration
    {
        var commonSetup = WithAutoSetupReuseDefaults(builder, containerName);
        var isCiRun = EnvironmentHelper.IsCiRun();
        var isWslDocker = EnvironmentHelper.IsWslDocker();
        if(isWslDocker)
        {
            migrationsPath = EnvironmentHelper.ConvertToWslPath(migrationsPath);   
        }
        else
        {
            migrationsPath = Path.GetFullPath(migrationsPath);
        }
        
        return commonSetup.WithMongoAutoSetupDefaultsInternal(migrationsPath, isCiRun, isWslDocker, useTmpfs);
    }

    internal static TBuilder WithMongoAutoSetupDefaultsInternal<TBuilder, TContainer, TConfiguration>(
        this ContainerBuilder<TBuilder, TContainer, TConfiguration> builder, 
            string migrationsPath, bool isWslDocker, bool isCiRun, bool? useTmpfs = null)
        where TBuilder : ContainerBuilder<TBuilder, TContainer, TConfiguration>
        where TContainer : IContainer
        where TConfiguration : IContainerConfiguration
    {
        // If execution is not in CI and the Docker engine is not running under WSL Tmpfs is created even without explicit TRUE
        if(!isCiRun && ((useTmpfs is null && !isWslDocker) || (useTmpfs is not null && useTmpfs.Value == true)))
        {
            builder = builder.WithTmpfsMount(Constants.MongoDB.DefaultDbDataDirectory, AccessMode.ReadWrite);
        }
        
        return builder
            .WithBindMount(migrationsPath, Constants.MongoDB.DefaultMigrationsDataPath, AccessMode.ReadOnly)
            .WithCreateParameterModifier(config =>
            {
                config.User = "root";
            });
    }
}