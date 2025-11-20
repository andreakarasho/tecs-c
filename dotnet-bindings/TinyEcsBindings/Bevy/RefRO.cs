using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// A ref struct that wraps a read-only mutable reference to a value.
/// Used in Bevy-style queries to provide read-only ref access to individual components that updates on each iteration.
/// </summary>
[SkipLocalsInit]
public ref struct RefRO<T> where T : struct
{
    internal ref T _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RefRO(ref T value)
    {
        _value = ref value;
    }

    /// <summary>
    /// Get a read-only reference to the wrapped value
    /// </summary>
    [UnscopedRef]
    public ref readonly T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _value;
    }
}
