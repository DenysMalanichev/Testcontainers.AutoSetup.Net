using Testcontainers.AutoSetup.Core.Attributes;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Attributes;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class DbResetAttributeTests
{
    [Fact]
    public void DbResetAttribute_InitializesWithDefaultResetScope()
    {
        // Arrange & Act
        var attr = new DbResetAttribute();

        // Assert
        Assert.Equal(ResetScope.BeforeExecution, attr.Scope);
    }

    [Fact]
    public void DbResetAttribute_InitializesWithProvidedResetScope()
    {
        // Arrange & Act
        var attr = new DbResetAttribute(ResetScope.BeforeExecution);

        // Assert
        Assert.Equal(ResetScope.BeforeExecution, attr.Scope);
    }

    [Fact]
    public void DbResetAttribute_InitializesWithProvidedNoneResetScope()
    {
        // Arrange & Act
        var attr = new DbResetAttribute(ResetScope.None);

        // Assert
        Assert.Equal(ResetScope.None, attr.Scope);
    }
}
