using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.AutoSetup.Core.Abstractions;
using Testcontainers.AutoSetup.Core.Common.Exceptions;

namespace Testcontainers.AutoSetup.Core.Extensions
{
    public static class ContainerExtensions
    {
        /// <summary>
        /// Executes the provided database seeder on a running container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the container (must implement <see cref="IContainer"/>).</typeparam>
        /// <param name="container">The started container instance to seed.</param>
        /// <param name="seeder">The <see cref="IDbSeeder"/> implementation responsible for migrating or populating the database.</param>
        /// <param name="connectionStringProvider">A function to resolve the connection string from the running container instance.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous start and seed operation.</returns>
        /// <remarks>
        /// <para>
        /// <strong>Important:</strong> This method executes the seeder <em>after</em> <see cref="IContainer.StartAsync"/> completes. 
        /// To prevent connection errors, ensure your container configuration uses a robust <see cref="IWaitUntil"/> strategy 
        /// (e.g., executing a "SELECT 1" query) rather than relying solely on log output or port availability.
        /// </para>
        /// <para>
        /// <strong>Concurrency Note:</strong> When using <c>.WithReuse(true)</c>, multiple tests may call this method on the same 
        /// running container simultaneously. The provided <paramref name="seeder"/> implementation must be <strong>idempotent</strong> 
        /// (safe to run multiple times) and handle potential concurrency to avoid race conditions (e.g., use transactions or checks like "IF NOT EXISTS").
        /// </para>
        /// </remarks>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
        /// <exception cref="InvalidContainerStateException">If the container is not in a <see cref="TestcontainersStates.Running"/> state</exception>
        public static async Task SeedAsync<TContainer>(
            this TContainer container,
            IDbSeeder seeder,
            Func<TContainer, string> connectionStringProvider,
            CancellationToken cancellationToken = default)
            where TContainer : IContainer
        {
            if(container.State != TestcontainersStates.Running)
            {
                throw new InvalidContainerStateException(container, TestcontainersStates.Running);
            }

            var connectionString = connectionStringProvider(container);

            await seeder.SeedAsync(container, connectionString, cancellationToken);
        }
    }
}