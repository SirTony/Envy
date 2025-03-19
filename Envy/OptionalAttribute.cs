namespace Envy;

[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field )]
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class OptionalAttribute : Attribute;
