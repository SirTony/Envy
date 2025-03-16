namespace Envy.Parsers;

public abstract class ValueParser<T> : IValueParser<T>
{
    /// <inheritdoc />
    public abstract T Parse( string value );

    /// <inheritdoc />
    public bool CanParseInto( Type type ) => type.IsAssignableFrom( typeof( T ) );

    /// <inheritdoc />
    public object? Parse( string value, Type type ) => this.Parse( value );
}
