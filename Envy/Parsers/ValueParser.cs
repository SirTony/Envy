namespace Envy.Parsers;

/// <summary>
///     Represents a value parser that converts a string into a specific type.
/// </summary>
/// <typeparam name="T">
///     The type into which the string is parsed.
/// </typeparam>
public abstract class ValueParser<T> : IValueParser<T>
{
    /// <inheritdoc />
    public abstract T Parse( string value );

    /// <inheritdoc />
    public bool CanParseInto( Type type ) => type.IsAssignableFrom( typeof( T ) );

    /// <inheritdoc />
    public object? Parse( string value, Type type ) => this.Parse( value );
}
