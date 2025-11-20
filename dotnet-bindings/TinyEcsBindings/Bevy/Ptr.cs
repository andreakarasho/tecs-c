using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// A ref struct that wraps a mutable reference to a value.
/// Used in Bevy-style queries to provide ref access to individual components that updates on each iteration.
/// </summary>
[SkipLocalsInit]
public ref struct Ptr<T> where T : struct
{
    internal ref T _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Ptr(ref T value)
    {
        _value = ref value;
    }

    /// <summary>
    /// Get a reference to the wrapped value
    /// </summary>
    [UnscopedRef]
    public ref T Ref
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _value;
    }
}
