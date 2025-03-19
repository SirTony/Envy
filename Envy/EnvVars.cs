using System.Diagnostics.CodeAnalysis;
using CaseConverter;

namespace Envy;

[SuppressMessage( "ReSharper",   "MemberCanBeMadeStatic.Global" )]
[SuppressMessage( "Performance", "CA1822:Mark members as static" )]
[SuppressMessage( "ReSharper",   "MemberCanBePrivate.Global" )]
public sealed class EnvVars
{
    private readonly string? _prefix;

    /// <summary>
    ///     Gets or sets the environment variable with the specified name.
    /// </summary>
    /// <param name="key">
    ///     The name of the environment variable to get or set.
    /// </param>
    public string? this[ string key ]
    {
        get => this.Get( key, EnvironmentVariableTarget.Process );
        set => this.Set( key, value, EnvironmentVariableTarget.Process );
    }

    internal EnvVars( string? prefix = null ) => this._prefix = prefix;

    /// <summary>
    ///     Retrieves the value of an environment variable with the specified key and target scope.
    /// </summary>
    /// <param name="key">The name of the environment variable to retrieve.</param>
    /// <param name="target">The target scope from which to retrieve the environment variable.</param>
    /// <returns>The value of the environment variable, or null if the variable is not found.</returns>
    public string? Get( string key, EnvironmentVariableTarget target )
        => Environment.GetEnvironmentVariable( this.FullName( key ), target );

    /// <summary>
    ///     Sets the value of an environment variable with the specified key and target scope.
    /// </summary>
    /// <param name="key">The name of the environment variable to set.</param>
    /// <param name="value">The value to assign to the environment variable. Can be null to delete the variable.</param>
    /// <param name="target">The target scope where the environment variable should be set.</param>
    public void Set( string key, string? value, EnvironmentVariableTarget target )
        => Environment.SetEnvironmentVariable( this.FullName( key ), value, target );

    /// <summary>
    ///     Creates a new instance of the <see cref="EnvVars" /> class with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to be used for environment variable names.</param>
    /// <returns>A new <see cref="EnvVars" /> instance with the given prefix.</returns>
    public EnvVars WithPrefix( string? prefix ) => new( prefix );

    /// <summary>
    ///     Constructs the full name of an environment variable by combining a prefix and the given name.
    /// </summary>
    /// <param name="name">The base name of the environment variable.</param>
    /// <returns>The full name of the environment variable in uppercase snake case, optionally prefixed.</returns>
    public string FullName( string name )
    {
        var fullName = $"{
            this._prefix ?? String.Empty
        }{
            ( this._prefix is not null && Env.PrefixSeparator is not null ? Env.PrefixSeparator : String.Empty )
        }{
            name
        }";

        return fullName.ToSnakeCase().ToUpperInvariant();
    }
}
