namespace Envy.Parsers;

public interface IValueParser<out T> : IValueParser
{
    T Parse( string value );
}

public interface IValueParser
{
    bool    CanParseInto( Type type );
    object? Parse( string      value, Type type );
}
