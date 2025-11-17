using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TinyEcsBindings.TinyEcsBevy;

namespace TinyEcsBindings;

/// <summary>
/// High-level wrapper around TinyECS Bevy-style API that only accepts structs.
/// Provides a safe, idiomatic C# API for building ECS applications with systems and resources.
/// </summary>
public sealed unsafe class TinyApp : IDisposable
{
    private readonly App _app;
    private readonly TinyWorld _world;
    private bool _disposed;

    public TinyApp(ThreadingMode threadingMode = ThreadingMode.SingleThreaded)
    {
        _app = tbevy_app_new(threadingMode);
        if (_app.Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create Bevy app");
        }

        // Create wrapper around the app's world
        var worldHandle = tbevy_app_world(_app);
        _world = new TinyWorld(worldHandle);
    }

    /// <summary>
    /// Get the world associated with this app.
    /// </summary>
    public TinyWorld World => _world;

    /// <summary>
    /// Insert a resource into the app.
    /// Resources are global singleton components accessible by all systems.
    /// </summary>
    public ResourceId<T> InsertResource<T>(T value) where T : struct
    {
        var name = ComponentName<T>.Name;
        var size = ComponentSize<T>.Size;

        TinyEcs.ComponentId id;
        ManagedStorage.ManagedStorageProvider<T>? provider = null;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            // Managed resource - register as component in the world
            id = ManagedStorage.RegisterManagedComponent<T>(_world._world, name, out provider);
        }
        else
        {
            // Unmanaged resource
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
            fixed (byte* namePtr = nameBytes)
            {
                id = TinyEcs.tecs_register_component(_world._world, namePtr, size);
            }
        }

        // Set the resource value using tecs_set directly
        if (size == 0)
        {
            // Empty struct (tag resource) - don't allocate chunk data
            TinyEcs.tecs_set(_world._world, TinyEcs.Entity.Null, id, null, 0);
        }
        else
        {
            TinyEcs.tecs_set(_world._world, TinyEcs.Entity.Null, id,
                Unsafe.AsPointer(ref Unsafe.AsRef(in value)), size);
        }

        return new ResourceId<T>(id, provider);
    }

    /// <summary>
    /// Get a mutable reference to a resource.
    /// </summary>
    public ref T GetResourceMut<T>(ResourceId<T> resourceId) where T : struct
    {
        // Both managed and unmanaged resources use the same access pattern
        var ptr = TinyEcs.tecs_get(_world._world, TinyEcs.Entity.Null, resourceId.Id);
        if (ptr == null)
            return ref Unsafe.NullRef<T>();
        return ref Unsafe.AsRef<T>(ptr);
    }

    /// <summary>
    /// Get a readonly reference to a resource.
    /// </summary>
    public ref readonly T GetResource<T>(ResourceId<T> resourceId) where T : struct
    {
        // Both managed and unmanaged resources use the same access pattern
        var ptr = TinyEcs.tecs_get(_world._world, TinyEcs.Entity.Null, resourceId.Id);
        if (ptr == null)
            return ref Unsafe.NullRef<T>();
        return ref Unsafe.AsRef<T>(ptr);
    }

    /// <summary>
    /// Add a system to the app.
    /// </summary>
    public SystemConfig AddSystem(SystemFunction system, void* userData = null)
    {
        var builder = tbevy_app_add_system(_app, system, userData);
        return new SystemConfig(builder);
    }

    /// <summary>
    /// Run all startup systems once.
    /// </summary>
    public void RunStartup()
    {
        tbevy_app_run_startup(_app);
    }

    /// <summary>
    /// Run one frame of update systems.
    /// </summary>
    public void Update()
    {
        tbevy_app_update(_app);
    }

    /// <summary>
    /// Run the application loop with a quit condition.
    /// </summary>
    public void Run(TinyEcsBevy.ShouldQuitFunction shouldQuit)
    {
        tbevy_app_run(_app, shouldQuit);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            tbevy_app_free(_app);
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a typed resource ID.
/// </summary>
public readonly struct ResourceId<T> where T : struct
{
    internal readonly TinyEcs.ComponentId Id;
    internal readonly ManagedStorage.ManagedStorageProvider<T>? StorageProvider;

    internal ResourceId(TinyEcs.ComponentId id, ManagedStorage.ManagedStorageProvider<T>? storageProvider)
    {
        Id = id;
        StorageProvider = storageProvider;
    }

    public override string ToString() => $"ResourceId<{typeof(T).ToString()}>({Id.Value})";
}

/// <summary>
/// Builder for configuring a system.
/// </summary>
public readonly unsafe struct SystemConfig
{
    private readonly TinyEcsBevy.SystemBuilder _builder;

    internal SystemConfig(TinyEcsBevy.SystemBuilder builder)
    {
        _builder = builder;
    }

    /// <summary>
    /// Set which stage this system runs in.
    /// </summary>
    public SystemConfig InStage(Stage stage)
    {
        tbevy_system_in_stage(_builder, stage);
        return this;
    }

    /// <summary>
    /// Add a system that must run before this system.
    /// </summary>
    public SystemConfig Before(string systemName)
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(systemName + "\0");
        fixed (byte* namePtr = nameBytes)
        {
            tbevy_system_before(_builder, namePtr);
        }
        return this;
    }

    /// <summary>
    /// Add a system that must run after this system.
    /// </summary>
    public SystemConfig After(string systemName)
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(systemName + "\0");
        fixed (byte* namePtr = nameBytes)
        {
            tbevy_system_after(_builder, namePtr);
        }
        return this;
    }

    /// <summary>
    /// Finalize the system configuration.
    /// </summary>
    public void Build()
    {
        tbevy_system_build(_builder);
    }
}

/// <summary>
/// Extension of TinyWorld to support being created from an existing world handle.
/// </summary>
public partial class TinyWorld
{
    internal TinyWorld(TinyEcs.World existingWorld)
    {
        _world = existingWorld;

        if (_world.Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Invalid world handle");
        }

        _disposed = false;
        _isExternalWorld = true;
    }
}

/// <summary>
/// Helper class for default stages.
/// </summary>
public static class Stages
{
    public static Stage Startup => tbevy_stage_default(StageId.Startup);
    public static Stage First => tbevy_stage_default(StageId.First);
    public static Stage PreUpdate => tbevy_stage_default(StageId.PreUpdate);
    public static Stage Update => tbevy_stage_default(StageId.Update);
    public static Stage PostUpdate => tbevy_stage_default(StageId.PostUpdate);
    public static Stage Last => tbevy_stage_default(StageId.Last);
}
