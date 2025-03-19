// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace Envy.Tests;

public class EnvTests
{
    // Simple test class for the test
    // ReSharper disable once ClassNeverInstantiated.Local
    private class TestModel
    {
        public string StringProperty { get; init; } = String.Empty;
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

    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed record OptionalParamsModel( string Foo = "foo", string Baz = "baz" );

    [Test]
    public void constructor_with_all_optional_parameters()
    {
        var model = Env.Bind<OptionalParamsModel>();
        Assert.Multiple(
            () => {
                Assert.That( model.Foo, Is.EqualTo( "foo" ) );
                Assert.That( model.Baz, Is.EqualTo( "baz" ) );
            }
        );
    }

    // Converting environment variables to a model with a simple class
    [Test]
    public void to_model_converts_environment_variables_to_simple_class()
    {
        var env = Env.Vars.WithPrefix( "TestModel" );

        // Arrange
        env["StringProperty"] = "test value";
        env["IntProperty"]    = "42";
        env["BoolProperty"]   = "true";

        // Act
        var model = Env.Bind<TestModel>( "TestModel" );

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
        env["StringProperty"] = null;
        env["IntProperty"]    = null;
        env["BoolProperty"]   = null;
    }

    // Handling missing required environment variables
    [Test]
    public void to_model_throws_exception_when_required_environment_variable_is_missing()
    {
        var env = Env.Vars.WithPrefix( "RequiredModel" );

        // Arrange
        env["OptionalProperty"] = "optional value";
        // Deliberately not setting the required property

        // Act & Assert
        var exception = Assert.Throws<KeyNotFoundException>( () => Env.Bind<RequiredModel>( "RequiredModel" ) );

        Assert.That( exception.Message, Does.Contain( "REQUIRED_PROPERTY" ) );

        // Cleanup
        env["OptionalProperty"] = null;
    }
}
