namespace Testcontainers.AutoSetup.Tests.TestCollections;

[CollectionDefinition(nameof(SequentialUnitTestsCollection), DisableParallelization = true)]
public class SequentialUnitTestsCollection
{
    // This class is just a marker for the collection definition
    // Parallelization DISABLED
}