namespace Envy.Parsers;

/// <summary>
///     Represents a value parser that converts a string into a specific type.
/// </summary>
/// <typeparam name="T">
///     The type into which the string is parsed.
/// </typeparam>
public interface IValueParser<out T> : IValueParser
{
    T Parse( string value );
}

/// <summary>
///     Represents a value parser that converts a string into a specific type.
/// </summary>
public interface IValueParser
{
    bool    CanParseInto( Type type );
    object? Parse( string      value, Type type );
}
