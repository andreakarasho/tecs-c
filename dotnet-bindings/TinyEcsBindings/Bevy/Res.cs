using System;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Immutable resource access system parameter.
/// Provides read-only access to a global resource.
/// Multiple systems can read the same resource in parallel.
/// </summary>
public sealed class Res<T> : ISystemParam where T : notnull
{
    private T? _value;
    private bool _hasValue;

    public void Initialize(TinyWorld world)
    {
        _value = default;
        _hasValue = false;
    }

    public void Fetch(TinyWorld world)
    {
        _hasValue = world.TryGetResource<T>(out _value);
    }

    public SystemParamAccess GetAccess()
    {
        var access = new SystemParamAccess();
        access.ReadResources.Add(typeof(T));
        return access;
    }

    /// <summary>
    /// Gets a read-only reference to the resource value.
    /// Throws if the resource doesn't exist.
    /// </summary>
    public ref readonly T Value
    {
        get
        {
            if (!_hasValue)
            {
                throw new InvalidOperationException(
                    $"Resource of type {typeof(T).Name} does not exist. " +
                    "Ensure the resource has been inserted and the system runs through the Bevy scheduler.");
            }
            return ref _value!;
        }
    }

    /// <summary>
    /// Returns true if the resource exists.
    /// </summary>
    public bool Exists => _hasValue;
}

/// <summary>
/// Mutable resource access system parameter.
/// Provides read-write access to a global resource.
/// Only one system can write to a resource at a time (exclusive access).
/// </summary>
public sealed class ResMut<T> : ISystemParam where T : notnull
{
    private T? _value;
    private bool _hasValue;

    public void Initialize(TinyWorld world)
    {
        _value = default;
        _hasValue = false;
    }

    public void Fetch(TinyWorld world)
    {
        _hasValue = world.TryGetResource<T>(out _value);
    }

    public SystemParamAccess GetAccess()
    {
        var access = new SystemParamAccess();
        access.WriteResources.Add(typeof(T)); // Write access = exclusive
        return access;
    }

    /// <summary>
    /// Gets a mutable reference to the resource value.
    /// Throws if the resource doesn't exist.
    /// </summary>
    public ref T Value
    {
        get
        {
            if (!_hasValue)
            {
                throw new InvalidOperationException(
                    $"Resource of type {typeof(T).Name} does not exist. " +
                    "Ensure the resource has been inserted and the system runs through the Bevy scheduler.");
            }
            return ref _value!;
        }
    }

    /// <summary>
    /// Returns true if the resource exists.
    /// </summary>
    public bool Exists => _hasValue;
}

/// <summary>
/// Per-system local state that persists between runs.
/// Each system instance gets its own copy of the local state.
/// Does not conflict with other systems.
/// </summary>
public sealed class Local<T> : ISystemParam where T : new()
{
    private T _value = default!;

    public void Initialize(TinyWorld world)
    {
        _value = new T();
    }

    public void Fetch(TinyWorld world)
    {
        // Local state persists, no need to fetch
    }

    public SystemParamAccess GetAccess()
    {
        // Local state has no conflicts - it's per-system
        return new SystemParamAccess();
    }

    /// <summary>
    /// Gets a mutable reference to the local state.
    /// </summary>
    public ref T Value => ref _value;
}
