using System.Reflection;
using Moq;
using Testcontainers.AutoSetup.Core;
using Testcontainers.AutoSetup.Core.Attributes;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Common.Enums;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests;

[Trait("Category", "Unit")]
[Collection(nameof(ParallelUnitTestsCollection))]
public class GenericTestBaseTests
{
    private class TestableGenericTestBase : GenericTestBase
    {
        public override Task ConfigureSetupAsync()
        {
            throw new NotImplementedException();
        }

        // Expose the protected method so we can call it from the test
        public Task InvokeOnTestStartAsync(Type testClassType, Action testResetAction)
        {
            return OnTestStartAsync(testClassType, testResetAction);
        }

        public override Task ResetEnvironmentAsync(Type testClassType)
        {
            throw new NotImplementedException();
        }

        // Helper to swap the hard-coded Environment with our Mock
        public void SetMockEnvironment(TestEnvironment mockEnv)
        {
            var envField = typeof(GenericTestBase)
                .GetField("TestEnvironment", BindingFlags.Instance | BindingFlags.NonPublic);

            if (envField == null)
                throw new InvalidOperationException("Could not find 'Environment' field on GenericTestBase");

            envField.SetValue(this, mockEnv);
        }
    }

    private class ClassWithNoAttribute { }

    [DbReset(ResetScope.None)]
    private class ClassWithResetNone { }

    [DbReset(ResetScope.BeforeExecution)]
    private class ClassWithResetBefore { }

    [Fact]
    public async Task OnTestStartAsync_Should_Execute_UserAction()
    {
        // Arrange
        var sut = new TestableGenericTestBase();
        var envMock = new Mock<TestEnvironment>();
        sut.SetMockEnvironment(envMock.Object);

        bool actionExecuted = false;
        Action userAction = () => actionExecuted = true;

        // Act
        await sut.InvokeOnTestStartAsync(typeof(ClassWithNoAttribute), userAction);

        // Assert
        Assert.True(actionExecuted, "User defined reset action should always be executed");
    }

    [Fact]
    public async Task OnTestStartAsync_Should_NOT_Reset_Environment_If_Attribute_Missing()
    {
        // Arrange
        var sut = new TestableGenericTestBase();
        var envMock = new Mock<TestEnvironment>();
        sut.SetMockEnvironment(envMock.Object);

        // Act
        await sut.InvokeOnTestStartAsync(typeof(ClassWithNoAttribute), () => { });

        // Assert
        envMock.Verify(e => e.ResetAsync(), Times.Never, "Should not reset if no DbResetAttribute is present");
    }

    [Fact]
    public async Task OnTestStartAsync_Should_NOT_Reset_Environment_If_Scope_Is_None()
    {
        // Arrange
        var sut = new TestableGenericTestBase();
        var envMock = new Mock<TestEnvironment>();
        sut.SetMockEnvironment(envMock.Object);

        // Act
        await sut.InvokeOnTestStartAsync(typeof(ClassWithResetNone), () => { });

        // Assert
        envMock.Verify(e => e.ResetAsync(), Times.Never, "Should not reset if Scope is None");
    }

    [Fact]
    public async Task OnTestStartAsync_Should_Reset_Environment_If_Scope_Is_BeforeExecution()
    {
        // Arrange
        var sut = new TestableGenericTestBase();
        var envMock = new Mock<TestEnvironment>();
        
        // Setup the mock to return a completed task so await doesn't hang
        envMock.Setup(e => e.ResetAsync()).Returns(Task.CompletedTask);
        
        // Inject the mock
        sut.SetMockEnvironment(envMock.Object);

        // Act
        await sut.InvokeOnTestStartAsync(typeof(ClassWithResetBefore), () => { });

        // Assert
        envMock.Verify(e => e.ResetAsync(), Times.Once, "Should call ResetAsync when Scope is BeforeExecution");
    }
}