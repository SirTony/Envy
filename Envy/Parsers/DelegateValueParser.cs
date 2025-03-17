namespace Envy.Parsers;

/// <summary>
///     Represents a value parser that uses a delegate function to parse strings into type <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">The type into which the string is parsed.</typeparam>
/// <param name="parser">A delegate function that defines how to parse the string into type <typeparamref name="T" />.</param>
public sealed class DelegateValueParser<T>( Func<string, T> parser ) : ValueParser<T>
{
    /// <summary>
    ///     Parses the given string value using the provided parser function.
    /// </summary>
    /// <param name="value">The string value to be parsed.</param>
    /// <returns>The parsed result of type T.</returns>
    public override T Parse( string value ) => parser( value );
}
