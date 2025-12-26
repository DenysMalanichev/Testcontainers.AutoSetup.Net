namespace Testcontainers.AutoSetup.Tests.IntegrationTests.TestCollections;

[CollectionDefinition(nameof(SequentialIntegrationTestsCollection), DisableParallelization = true)]
public class SequentialIntegrationTestsCollection : ICollectionFixture<ContainersFixture>
{

}