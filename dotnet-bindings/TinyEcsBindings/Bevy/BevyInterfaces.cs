using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Base interface for query iterators (both Data and Filter types)
/// </summary>
public interface IQueryIterator<TData>
    where TData : struct, allows ref struct
{
    TData GetEnumerator();

    [UnscopedRef]
    ref TData Current { get; }

    bool MoveNext();
}

/// <summary>
/// Interface for query data types (components to fetch)
/// </summary>
public interface IData<TData> : IQueryIterator<TData>
    where TData : struct, allows ref struct
{
    static abstract void Build(TinyWorld world);
    static abstract TData CreateIterator(QueryIterator iterator);
}

/// <summary>
/// Interface for query filters (With, Without, Changed, Added)
/// </summary>
public interface IFilter<TFilter> : IQueryIterator<TFilter>
    where TFilter : struct, allows ref struct
{
    void SetTicks(ulong lastRun, ulong thisRun);
    static abstract void Build(TinyWorld world);
    static abstract TFilter CreateIterator(QueryIterator iterator);
}

/// <summary>
/// Empty data/filter for queries with only filters or only data
/// </summary>
[SkipLocalsInit]
public ref struct Empty : IData<Empty>, IFilter<Empty>
{
    private readonly bool _asFilter;
    private QueryIterator _iterator;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Empty(QueryIterator iterator, bool asFilter)
    {
        _iterator = iterator;
        _asFilter = asFilter;
    }

    public static void Build(TinyWorld world) { }

    [UnscopedRef]
    public ref Empty Current => ref this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Empty GetEnumerator() => this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => _asFilter || _iterator.MoveNext();

    public readonly void SetTicks(ulong lastRun, ulong thisRun) { }

    static Empty IData<Empty>.CreateIterator(QueryIterator iterator)
    {
        return new Empty(iterator, false);
    }

    static Empty IFilter<Empty>.CreateIterator(QueryIterator iterator)
    {
        return new Empty(iterator, true);
    }
}
