using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Attributes;

/// <summary>
/// Specifies the testcontainers reset scope
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DbResetAttribute(ResetScope scope = ResetScope.BeforeExecution) : Attribute
{
    public ResetScope Scope { get; } = scope;
}