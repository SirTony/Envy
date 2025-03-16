namespace Envy;

[AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct, Inherited = false )]
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class MemberDiscoveryAttribute( MemberDiscovery discovery ) : Attribute
{
    public MemberDiscovery Discovery { get; } = discovery;
}
