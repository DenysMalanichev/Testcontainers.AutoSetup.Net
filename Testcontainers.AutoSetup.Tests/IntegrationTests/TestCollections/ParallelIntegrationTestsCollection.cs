namespace Testcontainers.AutoSetup.Tests.IntegrationTests.TestCollections;

[CollectionDefinition(nameof(ParallelIntegrationTestsCollection), DisableParallelization = false)]
public class ParallelIntegrationTestsCollection : ICollectionFixture<ContainersFixture>
{

}
