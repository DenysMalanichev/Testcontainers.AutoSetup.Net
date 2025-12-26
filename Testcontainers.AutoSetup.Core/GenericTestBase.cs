using Testcontainers.AutoSetup.Core.Attributes;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core;

public abstract class GenericTestBase
{
    protected readonly TestEnvironment TestEnvironment;

    private bool _isInitialized = false;

    protected GenericTestBase()
    {
        TestEnvironment = new TestEnvironment();
    }

    /// <summary>
    /// Performs the full initialization flow: 
    /// 1. User Configuration -> 2. Environment Cold Start
    /// </summary>
    public async Task InitializeEnvironmentAsync()
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("The test environment has already been initialized.");
        }

        await ConfigureSetupAsync();

        await TestEnvironment.InitializeAsync();

        _isInitialized = true;
    }

    /// <summary>
    /// Performs an initial setup of the test environment
    /// </summary>
    public abstract Task ConfigureSetupAsync();

    /// <summary>
    /// Performs a reset of the test environment before test execution
    /// </summary>
    /// <param name="testClassType">The type of a test class that is about to be executed</param>
    /// <returns></returns>
    public abstract Task ResetEnvironmentAsync(Type testClassType);

    /// <summary>
    /// Performs a preparations before each test execution
    /// </summary>
    /// <param name="testClassType">The type of a test class that is about to be executed</param>
    /// <param name="testResetAction">The <see cref="Action"/> that will be executed before DB <see cref="Common.TestEnvironment"/> reset</param>
    /// <returns></returns>
    protected async Task OnTestStartAsync(Type testClassType, Action? testResetAction = null!)
    {
        // User's defined reset logic
        testResetAction?.Invoke(); 

        if (ShouldReset(testClassType))
        {
            await TestEnvironment.ResetAsync();
        }
    }

    /// <summary>
    /// Checks if the class has <see cref="DbResetAttribute"/> 
    /// and whether it has a <see cref="ResetScope.None"/> argument 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="TypeAccessException"></exception>
    private static bool ShouldReset(Type testClassType)
    {

        if (Attribute.GetCustomAttribute(testClassType, typeof(DbResetAttribute)) is DbResetAttribute attr)
        {
            return attr.Scope is ResetScope.BeforeExecution;
        }

        // If no attribute is found, we reset by default
        return true;
    }
}