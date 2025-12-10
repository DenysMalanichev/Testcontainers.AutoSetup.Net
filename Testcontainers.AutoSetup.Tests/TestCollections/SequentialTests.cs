namespace Testcontainers.AutoSetup.Tests.TestCollections;

[CollectionDefinition(nameof(SequentialTests), DisableParallelization = true)]
public class SequentialTests
{
    // This class is just a marker for the collection definition
    // Parallelization DISABLED
}