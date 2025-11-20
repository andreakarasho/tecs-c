using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Filter that requires entities to have a specific component.
/// </summary>
[SkipLocalsInit]
public ref struct With<T> : IFilter<With<T>> where T : struct
{
    public static void Build(QueryBuilder builder)
    {
        builder.With<T>();
    }

    public static With<T> CreateIterator(QueryIterator iterator)
    {
        return new With<T>();
    }

    [UnscopedRef]
    public ref With<T> Current => ref this;

    public readonly With<T> GetEnumerator() => this;

    public readonly bool MoveNext()
    {
        return true; // Low-level query already filtered
    }

    public readonly void SetTicks(ulong lastRun, ulong thisRun)
    {
        // No tick tracking needed
    }
}

/// <summary>
/// Filter that requires entities to NOT have a specific component.
/// </summary>
[SkipLocalsInit]
public ref struct Without<T> : IFilter<Without<T>> where T : struct
{
    public static void Build(QueryBuilder builder)
    {
        builder.Without<T>();
    }

    public static Without<T> CreateIterator(QueryIterator iterator)
    {
        return new Without<T>();
    }

    [UnscopedRef]
    public ref Without<T> Current => ref this;

    public readonly Without<T> GetEnumerator() => this;

    public readonly bool MoveNext()
    {
        return true; // Low-level query already filtered
    }

    public readonly void SetTicks(ulong lastRun, ulong thisRun)
    {
        // No tick tracking needed
    }
}

/// <summary>
/// Filter that allows entities to optionally have a component.
/// Components will be null refs if not present.
/// </summary>
[SkipLocalsInit]
public ref struct Optional<T> : IFilter<Optional<T>> where T : struct
{
    public static void Build(QueryBuilder builder)
    {
        builder.Optional<T>();
    }

    public static Optional<T> CreateIterator(QueryIterator iterator)
    {
        return new Optional<T>();
    }

    [UnscopedRef]
    public ref Optional<T> Current => ref this;

    public readonly Optional<T> GetEnumerator() => this;

    public readonly bool MoveNext()
    {
        return true; // Component may or may not exist
    }

    public readonly void SetTicks(ulong lastRun, ulong thisRun)
    {
        // No tick tracking needed
    }
}

/// <summary>
/// Filter that only includes entities where the component was modified.
/// Uses change detection ticks to track modifications.
/// </summary>
[SkipLocalsInit]
public ref struct Changed<T> : IFilter<Changed<T>> where T : struct
{
    private QueryIterator _iterator;
    private ulong _lastRun, _thisRun;

    private Changed(QueryIterator iterator)
    {
        _iterator = iterator;
        _lastRun = 0;
        _thisRun = 0;
    }

    public static void Build(QueryBuilder builder)
    {
        builder.With<T>(); // Changed implies With
    }

    public static Changed<T> CreateIterator(QueryIterator iterator)
    {
        return new Changed<T>(iterator);
    }

    [UnscopedRef]
    public ref Changed<T> Current => ref this;

    public readonly Changed<T> GetEnumerator() => this;

    public bool MoveNext()
    {
        // TODO: Implement change detection using ComponentId
        // For now, just pass through to underlying iterator
        return _iterator.MoveNext();
    }

    public void SetTicks(ulong lastRun, ulong thisRun)
    {
        _lastRun = lastRun;
        _thisRun = thisRun;
    }
}

/// <summary>
/// Filter that only includes entities where the component was just added.
/// Uses add-detection ticks to track first-time additions.
/// </summary>
[SkipLocalsInit]
public ref struct Added<T> : IFilter<Added<T>> where T : struct
{
    private QueryIterator _iterator;
    private ulong _lastRun, _thisRun;

    private Added(QueryIterator iterator)
    {
        _iterator = iterator;
        _lastRun = 0;
        _thisRun = 0;
    }

    public static void Build(QueryBuilder builder)
    {
        builder.With<T>(); // Added implies With
    }

    public static Added<T> CreateIterator(QueryIterator iterator)
    {
        return new Added<T>(iterator);
    }

    [UnscopedRef]
    public ref Added<T> Current => ref this;

    public readonly Added<T> GetEnumerator() => this;

    public bool MoveNext()
    {
        // TODO: Implement add detection using ComponentId
        // For now, just pass through to underlying iterator
        return _iterator.MoveNext();
    }

    public void SetTicks(ulong lastRun, ulong thisRun)
    {
        _lastRun = lastRun;
        _thisRun = thisRun;
    }
}
