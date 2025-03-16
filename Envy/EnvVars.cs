using System.Diagnostics.CodeAnalysis;
using CaseConverter;

namespace Envy;

[SuppressMessage( "ReSharper",   "MemberCanBeMadeStatic.Global" )]
[SuppressMessage( "Performance", "CA1822:Mark members as static" )]
[SuppressMessage( "ReSharper",   "MemberCanBePrivate.Global" )]
public sealed class EnvVars
{
    private readonly string? _prefix;

    public string? this[ string key ]
    {
        get => this.Get( key, EnvironmentVariableTarget.Process );
        set => this.Set( key, value, EnvironmentVariableTarget.Process );
    }

    internal EnvVars( string? prefix = null ) => this._prefix = prefix;

    public string? Get( string key, EnvironmentVariableTarget target )
        => Environment.GetEnvironmentVariable( this.FullName( key ), target );

    public void Set( string key, string? value, EnvironmentVariableTarget target )
        => Environment.SetEnvironmentVariable( this.FullName( key ), value, target );

    public EnvVars WithPrefix( string? prefix ) => new( prefix );

    public string FullName( string name )
    {
        if( this._prefix is null ) return name.ToSnakeCase().ToUpperInvariant();

        var prefix = this._prefix.ToSnakeCase().ToUpperInvariant();
        var sep    = Env.PrefixSeparator ?? String.Empty;

        return $"{prefix}{sep}{name.ToSnakeCase().ToUpperInvariant()}";
    }
}
