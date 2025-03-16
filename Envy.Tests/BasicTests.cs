using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Envy.Tests;

[SuppressMessage( "ReSharper", "ClassNeverInstantiated.Local" )]
public class BasicTests
{
    private readonly record struct TestObject1( string Foo );

    private record struct TestObject2( string Foo );

    private record TestObject4( string Foo );

    private class TestObject5( string foo )
    {
        public string Foo { get; } = foo;
    }

    private struct TestObject7
    {
        public string Foo { get; init; }
    }

    private class TestObject8
    {
        public required string Foo { get; init; }
    }

    [MemberDiscovery( MemberDiscovery.OptIn )]
    private class TestObject9
    {
        [DataMember] public required string Foo { get; init; } = String.Empty;

        public string  DoesntExist1 { get; } = "bar";
        public string? DoesntExist2 { get; } = null;
    }

    [SetUp]
    public void Setup()
    {
        var vars = Env.Vars.WithPrefix( TestHelper.Prefix );

        vars["FOO"] = "foo";
    }

    [Test]
    public void TestReadonlyRecordStruct()
    {
        var obj = Env.ToModel<TestObject1>( TestHelper.Prefix );
        Assert.That( obj.Foo, Is.EqualTo( "foo" ) );
    }

    [Test]
    public void TestRecordStruct()
    {
        var obj = Env.ToModel<TestObject2>( TestHelper.Prefix );
        Assert.That( obj.Foo, Is.EqualTo( "foo" ) );
    }

    [Test]
    public void TestRecord()
    {
        var obj = Env.ToModel<TestObject4>( TestHelper.Prefix );
        Assert.That( obj.Foo, Is.EqualTo( "foo" ) );
    }

    [Test]
    public void TestClass() =>
        // no setter
        Assert.Throws<ArgumentException>(
            delegate { Env.ToModel<TestObject5>( TestHelper.Prefix ); }
        );

    [Test]
    public void TestStructWithInit()
    {
        var obj = Env.ToModel<TestObject7>( TestHelper.Prefix );
        Assert.That( obj.Foo, Is.EqualTo( "foo" ) );
    }

    [Test]
    public void TestClassWithRequired()
    {
        var obj = Env.ToModel<TestObject8>( TestHelper.Prefix );
        Assert.That( obj.Foo, Is.EqualTo( "foo" ) );
    }

    [Test]
    public void TestClassWithDefault()
    {
        var obj = Env.ToModel<TestObject9>( TestHelper.Prefix );
        Assert.Multiple(
            delegate {
                Assert.That( obj.Foo,          Is.EqualTo( "foo" ) );
                Assert.That( obj.DoesntExist1, Is.EqualTo( "bar" ) );
                Assert.That( obj.DoesntExist2, Is.Null );
            }
        );
    }
}
