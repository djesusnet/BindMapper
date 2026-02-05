using System.Runtime.CompilerServices;

namespace VelocityMapper;

/// <summary>
/// High-performance mapper configuration API.
/// Use this to configure mappings between types.
/// </summary>
/// <remarks>
/// <para>Usage:</para>
/// <code>
/// [MapperConfiguration]
/// public static void Configure()
/// {
///     Configuration.CreateMap&lt;User, UserDto&gt;();
///     Configuration.CreateMap&lt;Product, ProductDto&gt;();
/// }
/// </code>
/// </remarks>
public static class Configuration
{
    /// <summary>
    /// Creates a mapping configuration for the Source Generator to analyze.
    /// This method is analyzed at compile-time to generate optimized mapping code.
    /// </summary>
    /// <typeparam name="TSource">The source type to map from.</typeparam>
    /// <typeparam name="TDestination">The destination type to map to.</typeparam>
    /// <returns>A mapper configuration object for fluent configuration.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MapperConfiguration<TSource, TDestination> CreateMap<TSource, TDestination>()
    {
        return new MapperConfiguration<TSource, TDestination>();
    }

    /// <summary>
    /// Creates a mapping configuration with custom options.
    /// </summary>
    /// <typeparam name="TSource">The source type to map from.</typeparam>
    /// <typeparam name="TDestination">The destination type to map to.</typeparam>
    /// <param name="config">Action to configure the mapping.</param>
    /// <returns>A mapper configuration object for fluent configuration.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MapperConfiguration<TSource, TDestination> CreateMap<TSource, TDestination>(
        Action<MapperConfiguration<TSource, TDestination>> config)
    {
        var cfg = new MapperConfiguration<TSource, TDestination>();
        config(cfg);
        return cfg;
    }
}
