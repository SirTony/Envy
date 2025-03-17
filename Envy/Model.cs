using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Serialization;

namespace Envy;

internal sealed class Model( ConstructorInfo constructor, ImmutableDictionary<string, ModelMember> members )
{
    public ConstructorInfo                          Constructor { get; } = constructor;
    public ImmutableDictionary<string, ModelMember> Members     { get; } = members;

    /// <summary>
    ///     Reflects a type and creates a model from it
    ///     for deserialization later.
    /// </summary>
    public static Model FromType( Type type )
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

        var activator = Model.FindConstructor( type );

        var fields =
            from field in type.GetFields( flags )
            where !field.IsSpecialName
            where field.FieldType == typeof( string ) || Env.CanParse( field.FieldType )
            select new ModelMember.Field( field );

        var props =
            from prop in type.GetProperties( flags )
            where !prop.IsSpecialName
            where prop.CanWrite
            where prop.PropertyType == typeof( string ) || Env.CanParse( prop.PropertyType )
            select new ModelMember.Property( prop );

        var members =
            from member in fields.Cast<ModelMember>().Concat( props )
            where !member.GetCustomAttributes().Any( x => x is IgnoreDataMemberAttribute )
            select member;

        var dict = members.ToImmutableDictionary( x => x.Name, x => x );
        return new( activator, dict );
    }

    /// <summary>
    ///     Finds the most appropriate constructor to use when activating an instance of the given type.
    /// </summary>
    private static ConstructorInfo FindConstructor( Type type )
    {
        // we will only consider public constructors
        var constructors = type.GetConstructors( BindingFlags.Public | BindingFlags.Instance );
        if( constructors.Length == 0 ) Model.ThrowNoSuitableConstructors( type );

        // parameterless constructor are always preferred
        var parameterless = constructors.FirstOrDefault( x => x.GetParameters().Length == 0 );
        if( parameterless is not null ) return parameterless;

        // a candidate constructor is one where all required parameters are parsable by Envy
        var candidates =
            from ctor in constructors
            let ps = ctor.GetParameters()
            from p in ps
            where p.IsOptional || Env.CanParse( p.ParameterType )
            select ctor;

        // just grab the first one, if present. we don't have any other criteria to select from
        var selected = candidates.FirstOrDefault();
        if( selected is not null ) return selected;

        // if we get here, we have no suitable constructors
        Model.ThrowNoSuitableConstructors( type );
        return null!; // unreachable
    }

    private static void ThrowNoSuitableConstructors( Type type )
        => throw new InvalidOperationException( $"No suitable constructors found for {type.FullName}." );
}
