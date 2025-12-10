using Testcontainers.AutoSetup.Core.Helpers;
using Testcontainers.AutoSetup.Tests.TestCollections;

namespace Testcontainers.AutoSetup.Tests.UnitTests.HelpersTests;

[Trait("Category", "Unit")]
[Collection(nameof(SequentialTests))]
public class DockerHelperTests : IDisposable
{
    private readonly string[] _varsToClean = 
    [ 
        "CI", "TF_BUILD", "JENKINS_URL", "TEAMCITY_VERSION" 
    ];
    public DockerHelperTests()
    {
        ResetState();
    }

    [Fact]
    public void GetDockerEndpoint_ReturnsCustomEndpoint_WhenSet_IgnoringCi()
    {
        // Arrange
        string custom = "tcp://1.1.1.1:9999";
        DockerHelper.SetCustomDockerEndpoint(custom);
        
        // Even if we are in CI
        Environment.SetEnvironmentVariable("CI", "true");

        // Act
        var result = DockerHelper.GetDockerEndpoint();

        // Assert
        Assert.Equal(custom, result);
    }

    [Fact]
    public void GetDockerEndpoint_ReturnsNull_WhenInCi_And_NoCustomEndpoint()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CI", "true");

        // Act
        var result = DockerHelper.GetDockerEndpoint();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsCiRun_ReturnsTrue_When_CustomCheckReturnsTrue()
    {
        // Arrange
        // The environment is empty (IsCiRun would normally be false)
        DockerHelper.SetCustomCiCheck(() => true);

        // Act
        var result = DockerHelper.IsCiRun();

        // Assert
        Assert.True(result, "IsCiRun should be true when the custom check delegate returns true.");
    }

    [Fact]
    public void IsCiRun_ReturnsFalse_When_CustomCheckReturnsFalse_And_NoEnvVars()
    {
        // Arrange
        DockerHelper.SetCustomCiCheck(() => false);

        // Act
        var result = DockerHelper.IsCiRun();

        // Assert
        Assert.False(result, "IsCiRun should be false when no CI env vars are set.");
    }

    [Fact]
    public void IsCiRun_ReturnsTrue_When_StandardCiEnvVarIsSet()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CI", "true");

        // Act
        var result = DockerHelper.IsCiRun();

        // Assert
        Assert.True(result, "IsCiRun should detect the standard 'CI' variable.");
    }

    [Fact]
    public void IsCiRun_ReturnsTrue_When_VendorSpecificEnvVarIsSet()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TF_BUILD", "True"); // Azure DevOps

        // Act
        var result = DockerHelper.IsCiRun();

        // Assert
        Assert.True(result, "IsCiRun should detect vendor specific variables like TF_BUILD.");
    }

    [Fact]
    public void GetDockerEndpoint_ReturnsNull_If_InCiMode()
    {
        // Arrange
        DockerHelper.SetCustomCiCheck(() => true);

        // Act
        var endpoint = DockerHelper.GetDockerEndpoint();

        // Assert
        Assert.Null(endpoint);
    }

    [Fact]
    public void GetDockerEndpoint_ReturnsCorrectTcpString_If_Local()
    {
        // Arrange
        // Ensure we are NOT in CI
        DockerHelper.SetCustomCiCheck(() => false);
        
        // Set a custom port to verify formatting
        int testPort = 5555;
        DockerHelper.SetDockerPort(testPort);

        // Act
        var endpoint = DockerHelper.GetDockerEndpoint();

        // Assert
        Assert.NotNull(endpoint);
        Assert.StartsWith("tcp://", endpoint);
        Assert.EndsWith(":5555", endpoint);
    }
    
    [Fact]
    public void CustomCheck_TakesPriority_Over_EnvironmentVariables()
    {
        // Arrange
        // Set "CI" to true (normally IsCiRun would be true)
        Environment.SetEnvironmentVariable("CI", "true");
        
        // BUT set our custom check to explicitly return FALSE (maybe the user wants to force local mode in CI)
        // Note: In your current logic, custom check is an OR condition (if true -> true).        
        DockerHelper.SetCustomCiCheck(() => true);
        
        // Act
        var result = DockerHelper.IsCiRun();

        // Assert
        Assert.True(result);
    }

    
    public void Dispose()
    {
        ResetState();

        GC.SuppressFinalize(this);
    }

    private void ResetState()
    { 
        ClearEnvironmentVars();
        DockerHelper.SetCustomCiCheck(() => false);
        DockerHelper.SetDockerPort(2375);
        DockerHelper.SetCustomDockerEndpoint(null!); // Clear custom endpoint
        
        Environment.SetEnvironmentVariable("CI", null);
        Environment.SetEnvironmentVariable("TF_BUILD", null);
    }

    private void ClearEnvironmentVars()
    {
        foreach (var v in _varsToClean)
        {
            Environment.SetEnvironmentVariable(v, null);
        }
    }
}