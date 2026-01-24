using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VelocityMapper;

/// <summary>
/// High-performance collection mapping utilities.
/// </summary>
public static class CollectionMapper
{
    private const int PoolingThreshold = 1024;

    /// <summary>
    /// Maps a list to another list using a provided mapping function.
    /// Optimized for performance using Span&lt;T&gt; and CollectionsMarshal.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<TDestination> MapList<TSource, TDestination>(
        List<TSource>? source,
        Func<TSource, TDestination> mapper)
    {
        if (source is null || source.Count == 0)
            return new List<TDestination>();

        var count = source.Count;
        var destination = new List<TDestination>(count);

#if NET8_0_OR_GREATER
        // Use CollectionsMarshal for zero-allocation enumeration
        var sourceSpan = CollectionsMarshal.AsSpan(source);
        
        // Pre-allocate capacity
        CollectionsMarshal.SetCount(destination, count);
        var destSpan = CollectionsMarshal.AsSpan(destination);

        // Map using spans for better performance
        for (int i = 0; i < sourceSpan.Length; i++)
        {
            destSpan[i] = mapper(sourceSpan[i]);
        }
#else
        // Fallback for older frameworks - still optimized
        foreach (var item in source)
        {
            destination.Add(mapper(item));
        }
#endif

        return destination;
    }

    /// <summary>
    /// Maps an array to another array using a provided mapping function.
    /// Optimized for performance using Span&lt;T&gt; internally.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDestination[] MapArray<TSource, TDestination>(
        TSource[]? source,
        Func<TSource, TDestination> mapper)
    {
        if (source is null || source.Length == 0)
            return Array.Empty<TDestination>();

        var length = source.Length;
        var destination = new TDestination[length];
        var sourceSpan = source.AsSpan();
        var destSpan = destination.AsSpan();

        // Use span-based iteration for better performance
        for (int i = 0; i < length; i++)
        {
            destSpan[i] = mapper(sourceSpan[i]);
        }

        return destination;
    }

    /// <summary>
    /// Maps an enumerable to a list using a provided mapping function.
    /// Optimized based on source type.
    /// </summary>
    public static List<TDestination> MapEnumerable<TSource, TDestination>(
        IEnumerable<TSource>? source,
        Func<TSource, TDestination> mapper)
    {
        if (source is null)
            return new List<TDestination>();

        // Fast path for List<T>
        if (source is List<TSource> list)
            return MapList(list, mapper);

        // Fast path for arrays
        if (source is TSource[] array)
        {
            if (array.Length == 0)
                return new List<TDestination>();

            var arrayDestination = new List<TDestination>(array.Length);

#if NET8_0_OR_GREATER
            CollectionsMarshal.SetCount(arrayDestination, array.Length);
            var destSpan = CollectionsMarshal.AsSpan(arrayDestination);
            var sourceSpan = array.AsSpan();

            for (int i = 0; i < sourceSpan.Length; i++)
            {
                destSpan[i] = mapper(sourceSpan[i]);
            }
#else
            for (int i = 0; i < array.Length; i++)
            {
                arrayDestination.Add(mapper(array[i]));
            }
#endif

            return arrayDestination;
        }

        // Fast path for ICollection<T>
        if (source is ICollection<TSource> collection)
            return MapToList(collection, mapper);

        // Slow path for IEnumerable
        var destination = new List<TDestination>();

        foreach (var item in source)
        {
            destination.Add(mapper(item));
        }

        return destination;
    }

    /// <summary>
    /// Maps a read-only span to an array using a mapping function.
    /// Maximum performance for span-based operations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDestination[] MapSpan<TSource, TDestination>(
        ReadOnlySpan<TSource> source,
        Func<TSource, TDestination> mapper)
    {
        if (source.IsEmpty)
            return Array.Empty<TDestination>();

        var destination = new TDestination[source.Length];
        var destSpan = destination.AsSpan();

        for (int i = 0; i < source.Length; i++)
        {
            destSpan[i] = mapper(source[i]);
        }

        return destination;
    }

    /// <summary>
    /// Maps a collection to a list using a provided mapping function.
    /// Optimized for ICollection&lt;T&gt; to avoid enumerator overhead.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<TDestination> MapToList<TSource, TDestination>(
        ICollection<TSource> source,
        Func<TSource, TDestination> mapper)
    {
        if (source.Count == 0)
            return new List<TDestination>();

        var destination = new List<TDestination>(source.Count);

#if NET8_0_OR_GREATER
        CollectionsMarshal.SetCount(destination, source.Count);
        var destSpan = CollectionsMarshal.AsSpan(destination);
        var i = 0;
        foreach (var item in source)
        {
            destSpan[i++] = mapper(item);
        }
#else
        foreach (var item in source)
        {
            destination.Add(mapper(item));
        }
#endif

        return destination;
    }

    /// <summary>
    /// Maps a collection to an array using a provided mapping function.
    /// Optimized for ICollection&lt;T&gt; to avoid intermediate lists.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDestination[] MapToArray<TSource, TDestination>(
        ICollection<TSource> source,
        Func<TSource, TDestination> mapper)
    {
        if (source.Count == 0)
            return Array.Empty<TDestination>();

        var destination = new TDestination[source.Count];
        var i = 0;
        foreach (var item in source)
        {
            destination[i++] = mapper(item);
        }

        return destination;
    }

    /// <summary>
    /// Maps a collection to a pooled array for large workloads.
    /// Caller must return the array via ReturnPooledArray.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDestination[] MapToPooledArray<TSource, TDestination>(
        ICollection<TSource> source,
        Func<TSource, TDestination> mapper,
        out int length)
    {
        length = source.Count;
        if (length == 0)
            return Array.Empty<TDestination>();

        if (length < PoolingThreshold)
        {
            return MapToArray(source, mapper);
        }

        var pool = ArrayPool<TDestination>.Shared;
        var destination = pool.Rent(length);
        var i = 0;

        foreach (var item in source)
        {
            destination[i++] = mapper(item);
        }

        return destination;
    }

    /// <summary>
    /// Returns a pooled array rented via MapToPooledArray.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnPooledArray<TDestination>(TDestination[]? array, bool clearArray = false)
    {
        if (array is null || array.Length == 0)
            return;

        ArrayPool<TDestination>.Shared.Return(array, clearArray);
    }
}
