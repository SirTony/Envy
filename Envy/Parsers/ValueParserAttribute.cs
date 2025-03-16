namespace Envy.Parsers;

[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field )]
public sealed class ValueParserAttribute<T> : ValueParserAttribute
    where T : IValueParser, new()
{
    public ValueParserAttribute() : base( typeof( T ) ) => this.Parser = new T();
}

[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field )]
public class ValueParserAttribute : Attribute
{
    public IValueParser Parser { get; protected init; }

    // ReSharper disable once MemberCanBeProtected.Global
    public ValueParserAttribute( Type parserType )
    {
        if( !typeof( IValueParser ).IsAssignableFrom( parserType ) )
        {
            throw new ArgumentException(
                $"Type '{parserType.FullName}' does not implement '{nameof( IValueParser )}'"
            );
        }

        this.Parser = Activator.CreateInstance( parserType ) as IValueParser
                   ?? throw new ArgumentNullException( nameof( parserType ) );
    }
}
