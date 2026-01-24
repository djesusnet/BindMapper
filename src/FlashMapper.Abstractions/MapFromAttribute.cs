namespace FlashMapper;

/// <summary>
/// Specifies a custom mapping for a property.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class MapFromAttribute : Attribute
{
    /// <summary>
    /// The source property name to map from.
    /// </summary>
    public string SourcePropertyName { get; }

    public MapFromAttribute(string sourcePropertyName)
    {
        SourcePropertyName = sourcePropertyName ?? throw new ArgumentNullException(nameof(sourcePropertyName));
    }
}
