using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// A ref struct that wraps a reference to a value.
/// Used in Bevy-style queries to provide ref access to individual components.
/// </summary>
[SkipLocalsInit]
public readonly ref struct Ref<T> where T : struct
{
    private readonly ref T _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Ref(ref T value)
    {
        _value = ref value;
    }

    /// <summary>
    /// Get a reference to the wrapped value
    /// </summary>
    [UnscopedRef]
    public ref T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _value;
    }


    /// <summary>
    /// Get readonly reference
    /// </summary>
    [UnscopedRef]
    public ref readonly T ReadOnly
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _value;
    }
}
