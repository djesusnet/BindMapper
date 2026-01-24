namespace VelocityMapper;

/// <summary>
/// Configuration builder for creating type mappings.
/// This is used by the Source Generator to detect mapping configurations.
/// </summary>
/// <typeparam name="TSource">The source type to map from.</typeparam>
/// <typeparam name="TDestination">The destination type to map to.</typeparam>
public sealed class MapperConfiguration<TSource, TDestination>
{
    /// <summary>
    /// Configures a custom mapping for a destination member.
    /// </summary>
    /// <typeparam name="TMember">The type of the destination member.</typeparam>
    /// <param name="destinationMember">Expression selecting the destination member.</param>
    /// <param name="memberOptions">Configuration action for the member.</param>
    /// <returns>The configuration instance for method chaining.</returns>
    public MapperConfiguration<TSource, TDestination> ForMember<TMember>(
        System.Linq.Expressions.Expression<Func<TDestination, TMember>> destinationMember,
        Action<MemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
    {
        _ = destinationMember;
        _ = memberOptions;
        // This is analyzed by the Source Generator
        return this;
    }

    /// <summary>
    /// Reverses the mapping to allow TDestination -> TSource as well.
    /// </summary>
    /// <returns>The configuration instance for method chaining.</returns>
    public MapperConfiguration<TSource, TDestination> ReverseMap()
    {
        // This is analyzed by the Source Generator
        return this;
    }
}

/// <summary>
/// Provides member-level mapping configuration options.
/// </summary>
public sealed class MemberConfigurationExpression<TSource, TDestination, TMember>
{
    /// <summary>
    /// Maps from a custom source expression.
    /// </summary>
    /// <param name="sourceMember">Expression selecting the source.</param>
    public void MapFrom(System.Linq.Expressions.Expression<Func<TSource, TMember>> sourceMember)
    {
        _ = sourceMember;
        // This is analyzed by the Source Generator
    }

    /// <summary>
    /// Maps from a custom source expression with full context.
    /// </summary>
    /// <param name="valueResolver">Expression with both source and destination context.</param>
    public void MapFrom(System.Linq.Expressions.Expression<Func<TSource, TDestination, TMember>> valueResolver)
    {
        _ = valueResolver;
        // This is analyzed by the Source Generator
    }

    /// <summary>
    /// Ignores this member during mapping.
    /// </summary>
    public void Ignore()
    {
        // This is analyzed by the Source Generator
    }
}
