using DotNet.Testcontainers.Containers;

namespace Testcontainers.AutoSetup.Core.Common.Exceptions;

public class InvalidContainerStateException : Exception
{
    public InvalidContainerStateException() : base() { }

    public InvalidContainerStateException(string message) : base(message) { }

    public InvalidContainerStateException(IContainer container, TestcontainersStates expectedState)
        : base($"Expected Container {container.Id} to be {expectedState}, but found {container.State}") { }

    public InvalidContainerStateException(string message, Exception innerException)
        : base(message, innerException) { }

    public InvalidContainerStateException(IContainer container, TestcontainersStates expectedState, Exception innerException)
        : base($"Expected Container {container.Id} to be {expectedState}, but found {container.State}", innerException) { }

}
