namespace Envy.Tests;

public class EnvVarsTests
{
    // Get environment variable using indexer
    [Test]
    public void get_existing_environment_variable_using_indexer_returns_correct_value()
    {
        // Arrange
        const string expectedValue = "test_value";
        const string variableName  = "TEST_VAR";
        Environment.SetEnvironmentVariable( variableName, expectedValue, EnvironmentVariableTarget.Process );
        var envVars = new EnvVars();

        // Act
        var actualValue = envVars[variableName];

        // Assert
        Assert.That( actualValue, Is.EqualTo( expectedValue ) );

        // Cleanup
        Environment.SetEnvironmentVariable( variableName, null, EnvironmentVariableTarget.Process );
    }

    // Get non-existent environment variable
    [Test]
    public void get_non_existent_environment_variable_returns_null()
    {
        // Arrange
        var nonExistentVarName = "NON_EXISTENT_VAR_" + Guid.NewGuid().ToString( "N" );
        var envVars            = new EnvVars();

        // Act
        var result = envVars[nonExistentVarName];

        // Assert
        Assert.That( result, Is.Null );
    }
}
