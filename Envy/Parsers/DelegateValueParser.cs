namespace Envy.Parsers;

public sealed class DelegateValueParser<T>( Func<string, T> parser ) : ValueParser<T>
{
    public override T Parse( string value ) => parser( value );
}
