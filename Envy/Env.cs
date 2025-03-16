using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Envy.Parsers;

[assembly: InternalsVisibleTo( "Envy.Tests" )]

namespace Envy;

internal sealed record Model( Func<object> Factory, HashSet<ModelMember> Members );

public static class Env
{
    private static readonly  Dictionary<Type, Model> TypeCache = [];
    internal static readonly HashSet<IValueParser>   Parsers   = [];

    public static EnvVars Vars { get; } = new();

    public static string? PrefixSeparator { get; set; } = "_";

    static Env()
    {
        const NumberStyles integerStyle = NumberStyles.Integer | NumberStyles.AllowThousands;
        const NumberStyles floatStyle   = NumberStyles.Float   | NumberStyles.AllowThousands;

        Env.AddParser( x => x );
        Env.AddParser( Boolean.Parse );
        Env.AddParser( x => Byte.Parse( x, integerStyle, CultureInfo.CurrentCulture ) );
        Env.AddParser( x => SByte.Parse( x, integerStyle, CultureInfo.CurrentCulture ) );
        Env.AddParser( x => Int16.Parse( x, integerStyle, CultureInfo.CurrentCulture ) );
        Env.AddParser( x => UInt16.Parse( x, integerStyle, CultureInfo.CurrentCulture ) );
        Env.AddParser( x => Int32.Parse( x, integerStyle, CultureInfo.CurrentCulture ) );
        Env.AddParser( x => UInt32.Parse( x, integerStyle, CultureInfo.CurrentCulture ) );
        Env.AddParser( x => Int64.Parse( x, integerStyle, CultureInfo.CurrentCulture ) );
        Env.AddParser( x => UInt64.Parse( x, integerStyle, CultureInfo.CurrentCulture ) );
        Env.AddParser( x => Single.Parse( x, floatStyle, CultureInfo.CurrentCulture ) );
        Env.AddParser( x => Double.Parse( x, floatStyle, CultureInfo.CurrentCulture ) );
        Env.AddParser( x => Decimal.Parse( x, floatStyle, CultureInfo.CurrentCulture ) );
    }

    public static T ToModel<T>( string? prefix = null ) => (T)Env.ToModel( typeof( T ), prefix );

    // ReSharper disable once MemberCanBePrivate.Global
    public static object ToModel( Type type, string? prefix = null )
    {
        Env.CacheType( type );

        var vars  = Env.Vars.WithPrefix( prefix );
        var model = Env.TypeCache[type];
        var obj   = model.Factory();

        foreach( var member in model.Members )
        {
            var name  = member.Name;
            var value = vars[name];
            switch( value )
            {
                case null when member is { IsRequired: true }:
                    throw new InvalidOperationException(
                        $"Required environment variable '{vars.FullName( name )}' is not set."
                    );
                case null:
                    continue;
            }

            var parser = Env.Parsers.FirstOrDefault( x => x.CanParseInto( member.Type ) );
            if( parser is null )
            {
                throw new NotImplementedException(
                    $"No suitable parser found for type '{member.Type.FullName}'.\n"
                  + $"You can register a parser using Env.AddParser() or "
                  + $"by using the ValueParserAttribute on the property/field."
                );
            }

            var parsedValue = parser.Parse( value!, member.Type );
            if( parsedValue is null && !member.IsNullable )
            {
                throw new InvalidOperationException(
                    $"Failed to parse environment variable '{
                        vars.FullName( name )
                    }' into type '{
                        member.Type.FullName
                    }'."
                );
            }

            member.SetValue( obj, parsedValue );
        }

        return obj;
    }

    public static bool AddParser( IValueParser parser ) => Env.Parsers.Add( parser );

    public static bool AddParser<T>( Func<string, T> parser )
    {
        var p = new DelegateValueParser<T>( parser );
        return Env.Parsers.Add( p );
    }

    public static void AddParsers( params IEnumerable<IValueParser> parsers )
        => Env.Parsers.UnionWith( parsers );

    // ReSharper disable once MemberCanBePrivate.Global
    public static void CacheType( Type type )
    {
        if( Env.TypeCache.ContainsKey( type ) ) return;

        var discovery = type.GetCustomAttribute<MemberDiscoveryAttribute>()?.Discovery ?? MemberDiscovery.OptOut;
        var candidates =
            from member in type.GetMembers( BindingFlags.Instance | BindingFlags.Public )
            where member is PropertyInfo or FieldInfo
            select member switch
            {
                PropertyInfo pi => new ModelMember.Property( pi ) as ModelMember,
                FieldInfo fi    => new ModelMember.Field( fi ),
                _               => throw new NotImplementedException(),
            };

        var members = ( discovery switch
        {
            MemberDiscovery.OptOut =>
                from member in candidates
                where !member.IsOptedOut
                select member,

            MemberDiscovery.OptIn =>
                from member in candidates
                where member.IsOptedIn
                select member,
            _ => throw new NotImplementedException(),
        } ).ToArray();

        foreach( var member in members )
        {
            var parsers =
                from attr in member.GetCustomAttributes()
                where attr.GetType().IsSubclassOf( typeof( ValueParserAttribute ) )
                let vpAttr = attr as ValueParserAttribute
                select vpAttr.Parser;

            Env.Parsers.UnionWith( parsers );
        }

        Func<object>? factory = type.IsValueType ? () => Activator.CreateInstance( type )! : null;

        if( factory is null )
        {
            var ctors         = type.GetConstructors( BindingFlags.Public | BindingFlags.Instance );
            var parameterless = ctors.FirstOrDefault( x => x.GetParameters().Length == 0 );
            if( parameterless is not null )
                factory = () => parameterless.Invoke( null );

            if( factory is null )
            {
                // grab all constructors where the following criteria are true:
                // 1. constructor is public
                // 2. all required parameters are parsable
                var ctorCandidates =
                    from ctor in ctors
                    let ps = ctor.GetParameters()
                    from p in ps
                    where !p.IsOptional
                       || p.GetCustomAttribute<RequiredAttribute>() is not null
                       || p.GetCustomAttribute<RequiredMemberAttribute>() is not null
                    let vps = Env.Parsers
                    from vp in vps
                    where vp.CanParseInto( p.ParameterType )
                    select new { ctor, ps, vps };

                var selected = ctorCandidates.FirstOrDefault();
                if( selected is not null )
                {
                    factory = delegate {
                        var args = new object[selected.ps.Length];
                        for( var i = 0; i < args.Length; i++ )
                        {
                            var t      = selected.ps[i].ParameterType;
                            var envVar = Env.Vars[selected.ps[i].Name!];
                            var parser = selected.vps.FirstOrDefault( x => x.CanParseInto( t ) );

                            if( envVar is null ) break;
                            if( parser is null || !parser.CanParseInto( t ) )
                            {
                                throw new NotImplementedException(
                                    $"No suitable parser found for type '{t.FullName}'.\n"
                                  + $"You can register a parser using Env.AddParser() or "
                                  + $"by using the ValueParserAttribute on the property/field."
                                );
                            }

                            var value = parser.Parse( envVar, t );
                            args[i] = value
                                   ?? throw new InvalidOperationException(
                                          $"Failed to parse environment variable '{
                                              Env.Vars.FullName( selected.ps[i].Name! )
                                          }' into type '{
                                              t.FullName
                                          }'."
                                      );
                        }

                        var instance = selected.ctor.Invoke( args );
                        return instance;
                    };
                }
            }
        }

        if( factory is null )
        {
            throw new InvalidOperationException(
                $"No suitable constructor found for type '{type.FullName}'."
            );
        }

        var model = new Model( factory, [..members] );
        Env.TypeCache[type] = model;
    }
}
