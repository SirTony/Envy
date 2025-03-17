namespace Envy.Tests;

public class EnvTests
{
    // Simple test class for the test
    // ReSharper disable once ClassNeverInstantiated.Local
    private class TestModel
    {
        public string StringProperty { get; } = String.Empty;
        public int    IntProperty    { get; init; }
        public bool   BoolProperty   { get; init; }
    }

    // Test class with required property
    // ReSharper disable once ClassNeverInstantiated.Local
    private class RequiredModel
    {
        public required string RequiredProperty { get; set; } = String.Empty;

        public string OptionalProperty { get; set; } = String.Empty;
    }

    // Converting environment variables to a model with a simple class
    [Test]
    public void to_model_converts_environment_variables_to_simple_class()
    {
        // Arrange
        Environment.SetEnvironmentVariable( "TestModel_StringProperty", "test value" );
        Environment.SetEnvironmentVariable( "TestModel_IntProperty",    "42" );
        Environment.SetEnvironmentVariable( "TestModel_BoolProperty",   "true" );

        // Act
        var model = Env.ToModel<TestModel>( "TestModel" );

        // Assert
        Assert.That( model, Is.Not.Null );
        Assert.Multiple(
            () => {
                Assert.That( model.StringProperty, Is.EqualTo( "test value" ) );
                Assert.That( model.IntProperty,    Is.EqualTo( 42 ) );
                Assert.That( model.BoolProperty,   Is.True );
            }
        );

        // Cleanup
        Environment.SetEnvironmentVariable( "TestModel_StringProperty", null );
        Environment.SetEnvironmentVariable( "TestModel_IntProperty",    null );
        Environment.SetEnvironmentVariable( "TestModel_BoolProperty",   null );
    }

    // Handling missing required environment variables
    [Test]
    public void to_model_throws_exception_when_required_environment_variable_is_missing()
    {
        // Arrange
        Environment.SetEnvironmentVariable( "RequiredModel_OptionalProperty", "optional value" );
        // Deliberately not setting the required property

        // Act & Assert
        var exception = Assert.Throws<KeyNotFoundException>( () => Env.ToModel<RequiredModel>( "RequiredModel" ) );

        Assert.That( exception.Message, Does.Contain( "RequiredModel_RequiredProperty" ) );

        // Cleanup
        Environment.SetEnvironmentVariable( "RequiredModel_OptionalProperty", null );
    }
}
