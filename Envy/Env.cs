using System.Globalization;
using System.Runtime.CompilerServices;
using Envy.Parsers;

[assembly: InternalsVisibleTo( "Envy.Tests" )]

namespace Envy;

/// <summary>
///     The Env class provides functionality to parse environment variables into model objects.
///     It supports adding custom parsers for different data types and caching type information
///     for efficient model creation. The class also handles missing environment variables by
///     throwing exceptions.
/// </summary>
public static class Env
{
    private static readonly Dictionary<Type, Model> TypeCache = [];
    private static readonly List<IValueParser>      Parsers   = [];

    /// <summary>
    ///     The EnvVars class provides a way to access environment variables with a specific prefix.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static EnvVars Vars { get; } = new();

    /// <summary>
    ///     The separator used to join the prefix and the variable name in the environment variable.
    /// </summary>
    public static string? PrefixSeparator { get; set; } = "_";

    static Env()
    {
        const NumberStyles integerStyle = NumberStyles.Integer | NumberStyles.AllowThousands;
        const NumberStyles floatStyle   = NumberStyles.Float   | NumberStyles.AllowThousands;

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

    /// <summary>
    ///     Determines if a type can be parsed using the available parsers in the Env class.
    /// </summary>
    /// <typeparam name="T">The type to check for parsing capability.</typeparam>
    /// <returns>True if the type can be parsed; otherwise, false.</returns>
    public static bool CanParse<T>() => Env.CanParse( typeof( T ) );

    /// <summary>
    ///     Determines if a specified type can be parsed using the available parsers in the Env class.
    /// </summary>
    /// <typeparam name="T">The type to check for parsing capability.</typeparam>
    /// <returns>True if the type can be parsed; otherwise, false.</returns>
    public static bool CanParse( Type type ) => Env.Parsers.Any( parser => parser.CanParseInto( type ) );

    /// <summary>
    ///     Converts environment variables into an instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to convert the environment variables into.</typeparam>
    /// <param name="prefix">An optional prefix for the environment variables.</param>
    /// <returns>An instance of type <typeparamref name="T" /> populated with values from environment variables.</returns>
    public static T ToModel<T>( string? prefix = null ) => (T)Env.ToModel( typeof( T ), prefix );

    /// <summary>
    ///     Converts environment variables into an instance of the specified type.
    /// </summary>
    /// <param name="type">The type to convert the environment variables into.</param>
    /// <param name="prefix">An optional prefix for the environment variables.</param>
    /// <returns>An instance of <paramref name="type" /> populated with values from environment variables.</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static object ToModel( Type type, string? prefix = null )
    {
        Env.CacheType( type );

        var vars          = Env.Vars.WithPrefix( prefix );
        var model         = Env.TypeCache[type];
        var parameters    = model.Constructor.GetParameters();
        var ctorArgs      = new object?[parameters.Length];
        var alreadyParsed = new HashSet<string>();
        var obj           = ctorArgs.Length == 0 ? model.Constructor.Invoke( null ) : null;

        for( var i = 0; i < parameters.Length; i++ )
        {
            var param = parameters[i];
            var value = vars.Get( param.Name!, EnvironmentVariableTarget.Process );
            if( value is not null )
            {
                ctorArgs[i] = ParseValue( value, param.ParameterType );
                alreadyParsed.Add( vars.FullName( param.Name! ) );
                continue;
            }

            switch( param.IsOptional )
            {
                case true:
                    ctorArgs[i] = param.RawDefaultValue;
                    continue;
                default:
                {
                    Env.ThrowMissingVariable( vars.FullName( param.Name! ) );
                    break; // unreachable
                }
            }
        }

        obj ??= model.Constructor.Invoke( null, ctorArgs );
        if( obj is null )
        {
            throw new InvalidOperationException(
                $"Failed to create instance of {type.Name}."
            );
        }

        foreach( var (name, member) in model.Members )
        {
            if( alreadyParsed.Contains( vars.FullName( name ) ) ) continue;
            var value = vars.Get( name, EnvironmentVariableTarget.Process );

            switch( value )
            {
                case null when member.IsRequired:
                    Env.ThrowMissingVariable( vars.FullName( name ) );
                    break;
                case null when member.IsNullable:
                    continue;
                case null:
                    Env.ThrowMissingVariable( vars.FullName( name ) );
                    break;
            }

            var parsedValue = ParseValue( value, member.Type );
            if( parsedValue is null && member.IsRequired ) Env.ThrowMissingVariable( vars.FullName( name ) );
            member.SetValue( obj, parsedValue );
        }

        return obj;

        static object? ParseValue( string? value, Type type )
        {
            if( type == typeof( string ) ) return value;
            if( value is null ) return null;
            var parser = Env.Parsers.FirstOrDefault( p => p.CanParseInto( type ) );
            if( parser is not null ) return parser.Parse( value, type );

            throw new InvalidOperationException(
                $"No parser found for {type.Name}."
            );
        }
    }

    /// <summary>
    ///     Adds a new value parser to the list of parsers in the Env class.
    /// </summary>
    /// <param name="parser">The parser to be added.</param>
    public static void AddParser( IValueParser parser ) => Env.Parsers.Add( parser );

    /// <summary>
    ///     Adds a new value parser to the list of parsers in the Env class.
    /// </summary>
    /// <param name="parser">The parser to be added.</param>
    // ReSharper disable once MemberCanBePrivate.Global
    public static void AddParser<T>( Func<string, T> parser )
    {
        var p = new DelegateValueParser<T>( parser );
        Env.Parsers.Add( p );
    }

    /// <summary>
    ///     Adds multiple value parsers to the list of parsers in the Env class.
    /// </summary>
    /// <param name="parsers">An array of collections of parsers to be added.</param>
    public static void AddParsers( params IEnumerable<IValueParser> parsers )
        => Env.Parsers.AddRange( parsers );

    /// <summary>
    ///     Caches the specified type in the TypeCache dictionary if it is not already cached.
    /// </summary>
    /// <param name="type">The type to be cached.</param>
    // ReSharper disable once MemberCanBePrivate.Global
    public static void CacheType( Type type )
    {
        if( Env.TypeCache.ContainsKey( type ) ) return;
        Env.TypeCache[type] = Model.FromType( type );
    }

    private static void ThrowMissingVariable( string name )
        => throw new KeyNotFoundException(
            $"Missing required environment variable: {name}."
        );
}
