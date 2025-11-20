using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Extension methods to add resource management to TinyWorld.
/// Resources are per-world singleton values accessible by type.
/// </summary>
public static class ResourceExtensions
{
    private static readonly ConditionalWeakTable<TinyWorld, ResourceStorage> s_worldResources = new();

    private sealed class ResourceStorage
    {
        public readonly Dictionary<Type, object> Resources = new();
    }

    private static ResourceStorage GetStorage(TinyWorld world)
    {
        return s_worldResources.GetOrCreateValue(world);
    }

    /// <summary>
    /// Insert or update a resource in the world.
    /// </summary>
    public static void SetResource<T>(this TinyWorld world, T resource) where T : notnull
    {
        var storage = GetStorage(world);
        storage.Resources[typeof(T)] = resource;
    }

    /// <summary>
    /// Try to get a resource from the world.
    /// </summary>
    public static bool TryGetResource<T>(this TinyWorld world, out T? resource) where T : notnull
    {
        var storage = GetStorage(world);
        if (storage.Resources.TryGetValue(typeof(T), out var obj))
        {
            resource = (T)obj;
            return true;
        }
        resource = default;
        return false;
    }

    /// <summary>
    /// Get a resource from the world. Throws if it doesn't exist.
    /// </summary>
    public static T GetResource<T>(this TinyWorld world) where T : notnull
    {
        if (TryGetResource<T>(world, out var resource))
        {
            return resource!;
        }
        throw new InvalidOperationException($"Resource of type {typeof(T).Name} does not exist");
    }

    /// <summary>
    /// Check if a resource exists in the world.
    /// </summary>
    public static bool HasResource<T>(this TinyWorld world) where T : notnull
    {
        var storage = GetStorage(world);
        return storage.Resources.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Remove a resource from the world.
    /// </summary>
    public static void RemoveResource<T>(this TinyWorld world) where T : notnull
    {
        var storage = GetStorage(world);
        storage.Resources.Remove(typeof(T));
    }

    /// <summary>
    /// Remove a resource by type.
    /// </summary>
    internal static void RemoveResource(this TinyWorld world, Type resourceType)
    {
        var storage = GetStorage(world);
        storage.Resources.Remove(resourceType);
    }

    /// <summary>
    /// Get all resource types currently stored in the world.
    /// </summary>
    internal static IEnumerable<Type> GetAllResourceTypes(this TinyWorld world)
    {
        var storage = GetStorage(world);
        return storage.Resources.Keys;
    }

    /// <summary>
    /// Get a resource by its type (non-generic).
    /// </summary>
    internal static object? GetResourceByType(this TinyWorld world, Type resourceType)
    {
        var storage = GetStorage(world);
        storage.Resources.TryGetValue(resourceType, out var resource);
        return resource;
    }
}
