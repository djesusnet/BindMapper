namespace VelocityMapper;

/// <summary>
/// Marks a property or field to be ignored during mapping.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreMapAttribute : Attribute
{
}

