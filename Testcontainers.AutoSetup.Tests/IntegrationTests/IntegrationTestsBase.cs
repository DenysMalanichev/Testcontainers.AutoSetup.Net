namespace Testcontainers.AutoSetup.Tests.IntegrationTests;

[Collection("IntegrationTests")]
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
