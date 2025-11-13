using System.Runtime.InteropServices;

namespace TinyEcsBindings;

/// <summary>
/// Span-based iteration helpers for high-performance component access
/// </summary>
public static unsafe class SpanIteration
{
    /// <summary>
    /// Get a Span over a component column for native storage (zero-copy)
    /// </summary>
    public static Span<T> GetSpan<T>(TinyEcs.QueryIter* iter, int columnIndex) where T : unmanaged
    {
        var ptr = TinyEcs.tecs_iter_column(iter, columnIndex);
        if (ptr == null)
        {
            // Custom storage - cannot return contiguous span
            return Span<T>.Empty;
        }

        var count = TinyEcs.tecs_iter_count(iter);
        return new Span<T>(ptr, count);
    }

    /// <summary>
    /// Check if a column uses native storage (supports Span access)
    /// </summary>
    public static bool IsNativeStorage(TinyEcs.QueryIter* iter, int columnIndex)
    {
        return TinyEcs.tecs_iter_column(iter, columnIndex) != null;
    }

    /// <summary>
    /// Iterate with Span access for 1 component
    /// </summary>
    public static void ForEach<T1>(
        TinyEcs.QueryIter* iter,
        Action<Span<T1>> action)
        where T1 : unmanaged
    {
        while (TinyEcs.tecs_iter_next(iter))
        {
            var span1 = GetSpan<T1>(iter, 0);
            action(span1);
        }
    }

    /// <summary>
    /// Iterate with Span access for 2 components
    /// </summary>
    public static void ForEach<T1, T2>(
        TinyEcs.QueryIter* iter,
        Action<Span<T1>, Span<T2>> action)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        while (TinyEcs.tecs_iter_next(iter))
        {
            var span1 = GetSpan<T1>(iter, 0);
            var span2 = GetSpan<T2>(iter, 1);
            action(span1, span2);
        }
    }

    /// <summary>
    /// Iterate with Span access for 3 components
    /// </summary>
    public static void ForEach<T1, T2, T3>(
        TinyEcs.QueryIter* iter,
        Action<Span<T1>, Span<T2>, Span<T3>> action)
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
    {
        while (TinyEcs.tecs_iter_next(iter))
        {
            var span1 = GetSpan<T1>(iter, 0);
            var span2 = GetSpan<T2>(iter, 1);
            var span3 = GetSpan<T3>(iter, 2);
            action(span1, span2, span3);
        }
    }

    /// <summary>
    /// Iterate with Span access for 4 components
    /// </summary>
    public static void ForEach<T1, T2, T3, T4>(
        TinyEcs.QueryIter* iter,
        Action<Span<T1>, Span<T2>, Span<T3>, Span<T4>> action)
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
    {
        while (TinyEcs.tecs_iter_next(iter))
        {
            var span1 = GetSpan<T1>(iter, 0);
            var span2 = GetSpan<T2>(iter, 1);
            var span3 = GetSpan<T3>(iter, 2);
            var span4 = GetSpan<T4>(iter, 3);
            action(span1, span2, span3, span4);
        }
    }

    /// <summary>
    /// Delegate for per-entity iteration with 1 component (ref allows modification)
    /// </summary>
    public delegate void EntityAction<T1>(int index, ref T1 c1) where T1 : unmanaged;

    /// <summary>
    /// Delegate for per-entity iteration with 2 components (ref allows modification)
    /// </summary>
    public delegate void EntityAction<T1, T2>(int index, ref T1 c1, ref T2 c2)
        where T1 : unmanaged
        where T2 : unmanaged;

    /// <summary>
    /// Delegate for per-entity iteration with 3 components (ref allows modification)
    /// </summary>
    public delegate void EntityAction<T1, T2, T3>(int index, ref T1 c1, ref T2 c2, ref T3 c3)
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged;

    /// <summary>
    /// Iterate with per-entity access (works with any storage)
    /// </summary>
    public static void ForEachEntity<T1>(
        TinyEcs.QueryIter* iter,
        EntityAction<T1> action)
        where T1 : unmanaged
    {
        while (TinyEcs.tecs_iter_next(iter))
        {
            var count = TinyEcs.tecs_iter_count(iter);
            var c1 = TinyEcs.IterColumn<T1>(iter, 0);

            for (int i = 0; i < count; i++)
            {
                action(i, ref c1[i]);
            }
        }
    }

    /// <summary>
    /// Iterate with per-entity access for 2 components
    /// </summary>
    public static void ForEachEntity<T1, T2>(
        TinyEcs.QueryIter* iter,
        EntityAction<T1, T2> action)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        while (TinyEcs.tecs_iter_next(iter))
        {
            var count = TinyEcs.tecs_iter_count(iter);
            var c1 = TinyEcs.IterColumn<T1>(iter, 0);
            var c2 = TinyEcs.IterColumn<T2>(iter, 1);

            for (int i = 0; i < count; i++)
            {
                action(i, ref c1[i], ref c2[i]);
            }
        }
    }

    /// <summary>
    /// Iterate with per-entity access for 3 components
    /// </summary>
    public static void ForEachEntity<T1, T2, T3>(
        TinyEcs.QueryIter* iter,
        EntityAction<T1, T2, T3> action)
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
    {
        while (TinyEcs.tecs_iter_next(iter))
        {
            var count = TinyEcs.tecs_iter_count(iter);
            var c1 = TinyEcs.IterColumn<T1>(iter, 0);
            var c2 = TinyEcs.IterColumn<T2>(iter, 1);
            var c3 = TinyEcs.IterColumn<T3>(iter, 2);

            for (int i = 0; i < count; i++)
            {
                action(i, ref c1[i], ref c2[i], ref c3[i]);
            }
        }
    }
}
