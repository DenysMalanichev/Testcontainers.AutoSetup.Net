using Testcontainers.AutoSetup.Tests.IntegrationTests;

namespace Testcontainers.AutoSetup.Tests.TestCollections;

[CollectionDefinition(nameof(ParallelUnitTestsCollection), DisableParallelization = false)]
public class ParallelUnitTestsCollection
{
    // This class is just a marker for the collection definition
    // Parallelization ENABLED
}