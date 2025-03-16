using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Envy.Tests;

[SuppressMessage( "ReSharper", "ClassNeverInstantiated.Local" )]
public class EnvTests
{
    private class TestModel
    {
        public string String { get; set; }
        public bool   Bool   { get; set; }
        public int    Int    { get; set; }
        public double Double { get; set; }
    }

    private class RequiredFieldsModel
    {
        [Required] public string Required { get; set; }

        public string Optional { get; set; }
    }

    [Test]
    public void add_custom_parser()
    {
        var added = Env.AddParser( (Func<string, DateTime>)DateParser );
        Assert.Multiple(
            () => {
                Assert.That( added,                                                        Is.True );
                Assert.That( Env.Parsers.Any( p => p.CanParseInto( typeof( DateTime ) ) ), Is.True );
            }
        );

        return;

        DateTime DateParser( string s ) => DateTime.ParseExact( s, "yyyy-MM-dd", CultureInfo.InvariantCulture );
    }

    [Test]
    public void parse_environment_variables_into_primitive_types()
    {
        Environment.SetEnvironmentVariable( "TEST_STRING", "hello" );
        Environment.SetEnvironmentVariable( "TEST_BOOL",   "true" );
        Environment.SetEnvironmentVariable( "TEST_INT",    "42" );
        Environment.SetEnvironmentVariable( "TEST_DOUBLE", "3.14" );

        try
        {
            var model = Env.ToModel<TestModel>( "TEST" );
            Assert.Multiple(
                () => {
                    // Assert
                    Assert.That( model.String, Is.EqualTo( "hello" ) );
                    Assert.That( model.Bool,   Is.EqualTo( true ) );
                    Assert.That( model.Int,    Is.EqualTo( 42 ) );
                    Assert.That( model.Double, Is.EqualTo( 3.14 ) );
                }
            );
        }
        finally
        {
            Environment.SetEnvironmentVariable( "TEST_STRING", null );
            Environment.SetEnvironmentVariable( "TEST_BOOL",   null );
            Environment.SetEnvironmentVariable( "TEST_INT",    null );
            Environment.SetEnvironmentVariable( "TEST_DOUBLE", null );
        }
    }

    // Missing required environment variables should throw InvalidOperationException
    [Test]
    public void missing_required_environment_variable_throws_exception()
    {
        try
        {
            Environment.SetEnvironmentVariable( "TEST_OPTIONAL", "value" );
            var exception = Assert.Throws<InvalidOperationException>(
                () =>
                    Env.ToModel<RequiredFieldsModel>( "TEST" )
            );

            Assert.That( exception.Message, Does.Contain( "Required environment variable" ) );
            Assert.That( exception.Message, Does.Contain( "is not set" ) );
        }
        finally { Environment.SetEnvironmentVariable( "TEST_OPTIONAL", null ); }
    }
}
