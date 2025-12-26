using DotNet.Testcontainers.Containers;
using Moq;
using Testcontainers.AutoSetup.Core.Common.Exceptions;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Exceptions;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class InvalidContainerStateExceptionTests
{
    [Fact]
    public void InvalidContainerStateException_CreatesWithDefaultConstructor()
    {
        // Act
        var ex = new InvalidContainerStateException();

        // Assert
        Assert.NotEmpty(ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void InvalidContainerStateException_CreatesWithCustomMessage()
    {
        // Arrange
        var expectedMessage = "Something went wrong";

        // Act
        var ex = new InvalidContainerStateException(expectedMessage);

        // Assert
        Assert.Equal(expectedMessage, ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void InvalidContainerStateException_CreatesWithMessageAndInnerException()
    {
        // Arrange
        var expectedMessage = "Wrapper error";
        var innerEx = new InvalidOperationException("Root cause");

        // Act
        var ex = new InvalidContainerStateException(expectedMessage, innerEx);

        // Assert
        Assert.Equal(expectedMessage, ex.Message);
        Assert.Equal(innerEx, ex.InnerException);
    }

    [Fact]
    public void InvalidContainerStateException_FormatMessageUsingContainerDetails()
    {
        // Arrange
        var expectedState = TestcontainersStates.Running;
        var actualState = TestcontainersStates.Exited;
        var containerId = "test-container-123";

        var containerMock = new Mock<IContainer>();
        containerMock.Setup(c => c.Id).Returns(containerId);
        containerMock.Setup(c => c.State).Returns(actualState);

        // Act
        var ex = new InvalidContainerStateException(containerMock.Object, expectedState);

        // Assert
        Assert.Contains(containerId, ex.Message);
        Assert.Contains(expectedState.ToString(), ex.Message);
        Assert.Contains(actualState.ToString(), ex.Message);

        Assert.Equal($"Expected Container {containerId} to be {expectedState}, but found {actualState}", ex.Message);
    }

    [Fact]
    public void InvalidContainerStateException_FormatsMessageUsingContainerDetails_WithInnerException()
    {
        // Arrange
        var expectedState = TestcontainersStates.Running;
        var actualState = TestcontainersStates.Created;
        var containerId = "test-container-999";
        var innerEx = new TimeoutException("Timed out waiting for state");

        var containerMock = new Mock<IContainer>();
        containerMock.Setup(c => c.Id).Returns(containerId);
        containerMock.Setup(c => c.State).Returns(actualState);

        // Act
        var ex = new InvalidContainerStateException(containerMock.Object, expectedState, innerEx);

        // Assert
        Assert.Equal($"Expected Container {containerId} to be {expectedState}, but found {actualState}", ex.Message);
        Assert.Equal(innerEx, ex.InnerException);
    }
}
