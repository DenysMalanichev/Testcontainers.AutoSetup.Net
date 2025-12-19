using Testcontainers.AutoSetup.Core.Common;

namespace Testcontainers.AutoSetup.Core;

public abstract class GenericTestBase
{
    public readonly TestEnvironment Environment;

    protected GenericTestBase(TestEnvironment environment)
    {
        Environment = environment;
    }

    // The method to call before every test
    protected async Task OnTestStartAsync()
    {
        // Optional: Reflection check for [DbReset] attribute goes here
        // var shouldReset = CheckAttribute(this.GetType());
        // TODO implement an attribute and its usage

        if (ShouldReset())
        {
            await Environment.ResetAsync();
        }
    }

    // Helper for attribute checking
    private static bool ShouldReset()
    {
        // Basic reflection to see if class/method has [DbReset(None)]
        // This is framework-agnostic because it uses standard .NET Reflection
        return true; 
    }
}