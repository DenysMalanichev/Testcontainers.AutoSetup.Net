using System.Reflection.Metadata.Ecma335;
using Testcontainers.AutoSetup.Core.Attributes;
using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core;

public abstract class GenericTestBase
{
    private const string ScopePropertyName = "Scope"; 

    protected readonly TestEnvironment Environment;

    protected GenericTestBase()
    {
        Environment = new TestEnvironment();
    }

    /// <summary>
    /// Performs a preparations before each test execution
    /// </summary>
    /// <param name="testClassType">The type of a test class that is about to be executed</param>
    /// <param name="testResetAction">The <see cref="Action"/> that will be executed before DB <see cref="TestEnvironment"/> reset</param>
    /// <returns></returns>
    protected async Task OnTestStartAsync(Type testClassType, Action? testResetAction = null!)
    {
        // User's defined reset logic
        testResetAction?.Invoke(); 

        if (ShouldReset(testClassType))
        {
            await Environment.ResetAsync();
        }
    }

    /// <summary>
    /// Checks if the class has <see cref="DbResetAttribute"/> 
    /// and whether it has a <see cref="ResetScope.None"/> 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="TypeAccessException"></exception>
    private static bool ShouldReset(Type testClassType)
    {
        var attribute = Attribute.GetCustomAttribute(testClassType, typeof(DbResetAttribute));
        if(attribute is null)
        {
            return false;
        }

        var resetScopePropertyInfo = attribute.GetType().GetProperty(ScopePropertyName)
            ?? throw new TypeAccessException($"Cannot find a '{ScopePropertyName}' property of {typeof(DbResetAttribute)}");
        var scopeValue = (ResetScope)resetScopePropertyInfo.GetValue(attribute)!;

        return scopeValue is ResetScope.BeforeExecution;
    }
}