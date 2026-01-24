using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FlashMapper.Runtime;

/// <summary>
/// Internal helper utilities for high-performance operations.
/// Uses Span&lt;T&gt; internally but never exposes it in public API.
/// </summary>
internal static class SpanHelper
{
    private const int StackAllocThreshold = 128;

    /// <summary>
    /// Fast array copy using spans internally with stack allocation for small arrays.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T[] CopyArray<T>(T[] source)
    {
        if (source.Length == 0)
            return Array.Empty<T>();

        var destination = new T[source.Length];
        source.AsSpan().CopyTo(destination);
        return destination;
    }

    /// <summary>
    /// Fast array copy with optimization for value types and small arrays.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CopyArrayTo<T>(ReadOnlySpan<T> source, Span<T> destination)
    {
        source.CopyTo(destination);
    }

    /// <summary>
    /// Fast list to array conversion using CollectionsMarshal when available.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T[] ListToArray<T>(List<T> source)
    {
#if NET8_0_OR_GREATER
        var sourceSpan = CollectionsMarshal.AsSpan(source);
        return sourceSpan.ToArray();
#else
        return source.ToArray();
#endif
    }

    /// <summary>
    /// Optimized batch copy for arrays using SIMD when possible.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BatchCopy<T>(ReadOnlySpan<T> source, Span<T> destination)
    {
        if (source.Length != destination.Length)
            throw new ArgumentException("Source and destination must have the same length");

        // Let the runtime optimize with SIMD when T is suitable
        source.CopyTo(destination);
    }
}
