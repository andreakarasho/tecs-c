using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace TinyEcsBindings;

/// <summary>
/// Helper class to get the actual size of a component type.
/// Handles managed components, unmanaged components, and zero-sized structs (tags).
/// </summary>
internal static class ComponentSize<T> where T : struct
{
    public static readonly int Size = GetSize();

    private static int GetSize()
    {
        var size = RuntimeHelpers.IsReferenceOrContainsReferences<T>() ? IntPtr.Size : Unsafe.SizeOf<T>();

        if (size != 1)
            return size;

        // Credit: BeanCheeseBurrito from Flecs.NET
        // Detects if a struct is truly empty (zero-sized) vs. having 1 byte of data
        Unsafe.SkipInit<T>(out var t1);
        Unsafe.SkipInit<T>(out var t2);
        Unsafe.As<T, byte>(ref t1) = 0x7F;
        Unsafe.As<T, byte>(ref t2) = 0xFF;

        return ValueType.Equals(t1, t2) ? 0 : size;
    }
}

/// <summary>
/// Helper class to get a clean, readable name for a component type.
/// Handles nested types and generic types properly.
/// </summary>
internal static class ComponentName<T> where T : struct
{
    public static readonly string Name = GetName();

    private static string GetName()
    {
        var name = typeof(T).ToString();
        name = name
            .Replace('+', '.')
            .Replace('[', '<')
            .Replace(']', '>');

        int start = 0;
        int current = 0;
        bool skip = false;

        var stringBuilder = new StringBuilder();

        foreach (char c in name)
        {
            if (skip && (c == '<' || c == '.'))
            {
                start = current;
                skip = false;
            }
            else if (!skip && c == '`')
            {
                stringBuilder.Append(name.AsSpan(start, current - start));
                skip = true;
            }

            current++;
        }

        var str = stringBuilder.Append(name.AsSpan(start)).ToString();
        return str;
    }
}

/// <summary>
/// Automatic component registration using generic static constructors.
/// Components are auto-registered the first time they're used in a world.
/// No reflection required!
/// </summary>
public sealed unsafe partial class TinyWorld
{
    // Per-world component registry
    private readonly ConcurrentDictionary<Type, object> _componentIds = new();

    /// <summary>
    /// Get or auto-register a component. This is the magic method that enables
    /// auto-registration without manual calls or reflection.
    /// </summary>
    public ComponentId<T> Component<T>() where T : struct
    {
        // Fast path - component already registered in this world
        if (_componentIds.TryGetValue(typeof(T), out var existing))
        {
            return (ComponentId<T>)existing;
        }

        // Slow path - register the component
        var componentId = RegisterComponent<T>();
        _componentIds[typeof(T)] = componentId;
        return componentId;
    }

    /// <summary>
    /// Set a component on an entity (auto-registers component if needed).
    /// </summary>
    public void Set<T>(Entity entity, T value) where T : struct
    {
        var componentId = Component<T>();
        Set(entity, componentId, value);
    }

    /// <summary>
    /// Get a reference to a component on an entity (auto-registers component if needed).
    /// </summary>
    public ref T Get<T>(Entity entity) where T : struct
    {
        var componentId = Component<T>();
        return ref Get(entity, componentId);
    }

    /// <summary>
    /// Check if an entity has a component (auto-registers component if needed).
    /// </summary>
    public bool Has<T>(Entity entity) where T : struct
    {
        var componentId = Component<T>();
        return Has(entity, componentId);
    }

    /// <summary>
    /// Remove a component from an entity (auto-registers component if needed).
    /// </summary>
    public void Remove<T>(Entity entity) where T : struct
    {
        var componentId = Component<T>();
        Remove(entity, componentId);
    }
}

/// <summary>
/// Auto-registration extensions for QueryBuilder.
/// </summary>
public static class QueryBuilderExtensions
{
    /// <summary>
    /// Add a required component to the query (auto-registers if needed).
    /// </summary>
    public static QueryBuilder With<T>(this QueryBuilder builder, TinyWorld world) where T : struct
    {
        var componentId = world.Component<T>();
        return builder.With(componentId);
    }

    /// <summary>
    /// Add an excluded component to the query (auto-registers if needed).
    /// </summary>
    public static QueryBuilder Without<T>(this QueryBuilder builder, TinyWorld world) where T : struct
    {
        var componentId = world.Component<T>();
        return builder.Without(componentId);
    }

    /// <summary>
    /// Add an optional component to the query (auto-registers if needed).
    /// </summary>
    public static QueryBuilder Optional<T>(this QueryBuilder builder, TinyWorld world) where T : struct
    {
        var componentId = world.Component<T>();
        return builder.Optional(componentId);
    }

    /// <summary>
    /// Filter for components that have been changed since the last query (auto-registers if needed).
    /// </summary>
    public static QueryBuilder Changed<T>(this QueryBuilder builder, TinyWorld world) where T : struct
    {
        var componentId = world.Component<T>();
        return builder.Changed(componentId);
    }

    /// <summary>
    /// Filter for components that have been added since the last query (auto-registers if needed).
    /// </summary>
    public static QueryBuilder Added<T>(this QueryBuilder builder, TinyWorld world) where T : struct
    {
        var componentId = world.Component<T>();
        return builder.Added(componentId);
    }
}

/// <summary>
/// Auto-registration extensions for QueryIterator.
/// </summary>
public static class QueryIteratorExtensions
{
    /// <summary>
    /// Get a span of components for the current chunk (auto-registers if needed).
    /// </summary>
    public static Span<T> Column<T>(this QueryIterator iterator, TinyWorld world) where T : struct
    {
        var componentId = world.Component<T>();
        return iterator.Column(componentId);
    }

    /// <summary>
    /// Get the changed ticks for a component column (auto-registers if needed).
    /// </summary>
    public static ReadOnlySpan<TinyEcs.Tick> ChangedTicks<T>(this QueryIterator iterator, TinyWorld world) where T : struct
    {
        var componentId = world.Component<T>();
        return iterator.ChangedTicks(componentId);
    }

    /// <summary>
    /// Get the added ticks for a component column (auto-registers if needed).
    /// </summary>
    public static ReadOnlySpan<TinyEcs.Tick> AddedTicks<T>(this QueryIterator iterator, TinyWorld world) where T : struct
    {
        var componentId = world.Component<T>();
        return iterator.AddedTicks(componentId);
    }
}
