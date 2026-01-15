namespace Testcontainers.AutoSetup.Tests.IntegrationTests;

public class ContainersFixture : IAsyncLifetime
{
    public GlobalTestSetup Setup { get; } = new GlobalTestSetup();

    // CALLED ONCE: Before the first test in the collection runs
    public async Task InitializeAsync()
    {
        await Setup.InitializeEnvironmentAsync().ConfigureAwait(false);
    }

    // CALLED ONCE: After the last test in the collection finishes
    public async Task DisposeAsync()
    {
    }
}
