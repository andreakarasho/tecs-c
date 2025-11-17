using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyEcsBindings;

/// <summary>
/// High-level wrapper around TinyECS that only accepts structs.
/// Provides a safe, idiomatic C# API for entity-component-system operations.
/// </summary>
public sealed unsafe partial class TinyWorld : IDisposable
{
    internal TinyEcs.World _world;
    internal bool _disposed;
    private bool _isExternalWorld;

    public TinyWorld()
    {
        _world = TinyEcs.tecs_world_new();
        if (_world.Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create ECS world");
        }
    }

    /// <summary>
    /// Register a component type. Can be a struct with or without references.
    /// </summary>
    public ComponentId<T> RegisterComponent<T>() where T : struct
    {
        var name = ComponentName<T>.Name;
        var size = ComponentSize<T>.Size;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            // Managed component - contains references
            var id = ManagedStorage.RegisterManagedComponent<T>(_world, name, out var provider);
            return new ComponentId<T>(id, provider);
        }
        else
        {
            // Unmanaged component
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
            TinyEcs.ComponentId id;
            fixed (byte* namePtr = nameBytes)
            {
                id = TinyEcs.tecs_register_component(_world, namePtr, size);
            }
            return new ComponentId<T>(id, null);
        }
    }

    /// <summary>
    /// Create a new entity.
    /// </summary>
    public Entity Create()
    {
        var entity = TinyEcs.tecs_entity_new(_world);
        return new Entity(entity);
    }

    /// <summary>
    /// Delete an entity.
    /// </summary>
    public void Delete(Entity entity)
    {
        TinyEcs.tecs_entity_delete(_world, entity.Raw);
    }

    /// <summary>
    /// Check if an entity exists.
    /// </summary>
    public bool Exists(Entity entity)
    {
        return TinyEcs.tecs_entity_exists(_world, entity.Raw);
    }

    /// <summary>
    /// Set a component on an entity.
    /// </summary>
    public void Set<T>(Entity entity, ComponentId<T> componentId, T value) where T : struct
    {
        var size = ComponentSize<T>.Size;

        // For empty structs (tag components), pass null pointer and size 0 to avoid allocating chunk data
        if (size == 0)
        {
            TinyEcs.tecs_set(_world, entity.Raw, componentId.Id, null, 0);
        }
        else
        {
            // Both managed and unmanaged components use tecs_set
            // For managed components, pass pointer to the object reference
            // For unmanaged components, pass pointer to the value
            TinyEcs.tecs_set(_world, entity.Raw, componentId.Id, Unsafe.AsPointer(ref value), size);
        }
    }

    /// <summary>
    /// Get a reference to a component on an entity.
    /// </summary>
    public ref T Get<T>(Entity entity, ComponentId<T> componentId) where T : struct
    {
        // Both managed and unmanaged components use the same access pattern
        // The storage provider's GetPtr callback returns the correct pointer
        var ptr = TinyEcs.tecs_get(_world, entity.Raw, componentId.Id);
        if (ptr == null)
            return ref Unsafe.NullRef<T>();
        return ref Unsafe.AsRef<T>(ptr);
    }

    /// <summary>
    /// Check if an entity has a component.
    /// </summary>
    public bool Has<T>(Entity entity, ComponentId<T> componentId) where T : struct
    {
        return TinyEcs.tecs_has(_world, entity.Raw, componentId.Id);
    }

    /// <summary>
    /// Remove a component from an entity.
    /// </summary>
    public void Remove<T>(Entity entity, ComponentId<T> componentId) where T : struct
    {
        TinyEcs.tecs_unset(_world, entity.Raw, componentId.Id);
    }

    /// <summary>
    /// Create a query builder.
    /// </summary>
    public QueryBuilder Query()
    {
        return new QueryBuilder(_world);
    }

    /// <summary>
    /// Get the world tick (version counter).
    /// </summary>
    public ulong Tick => TinyEcs.tecs_world_tick(_world).Value;

    /// <summary>
    /// Get the number of entities in the world.
    /// </summary>
    public int Count => TinyEcs.tecs_world_entity_count(_world);

    public void Dispose()
    {
        if (!_disposed)
        {
            // Only free the world if it's not external (i.e., not owned by TinyApp)
            if (!_isExternalWorld)
            {
                TinyEcs.tecs_world_free(_world);
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents an entity ID.
/// </summary>
public readonly struct Entity
{
    internal readonly TinyEcs.Entity Raw;

    internal Entity(TinyEcs.Entity raw)
    {
        Raw = raw;
    }

    public ulong Id => Raw.Value;

    public override string ToString() => $"Entity({Raw.Index}:{Raw.Generation})";
}

/// <summary>
/// Represents a typed component ID.
/// </summary>
public readonly struct ComponentId<T> where T : struct
{
    internal readonly TinyEcs.ComponentId Id;
    internal readonly ManagedStorage.ManagedStorageProvider<T>? StorageProvider;

    internal ComponentId(TinyEcs.ComponentId id, ManagedStorage.ManagedStorageProvider<T>? storageProvider)
    {
        Id = id;
        StorageProvider = storageProvider;
    }

    public override string ToString() => $"ComponentId<{typeof(T).ToString()}>({Id.Value})";
}

/// <summary>
/// Query builder for constructing ECS queries.
/// </summary>
public readonly unsafe ref struct QueryBuilder
{
    private readonly TinyEcs.Query _query;

    internal QueryBuilder(TinyEcs.World world)
    {
        _query = TinyEcs.tecs_query_new(world);
    }

    /// <summary>
    /// Add a required component to the query.
    /// </summary>
    public QueryBuilder With<T>(ComponentId<T> componentId) where T : struct
    {
        TinyEcs.tecs_query_with(_query, componentId.Id);
        return this;
    }

    /// <summary>
    /// Add an excluded component to the query.
    /// </summary>
    public QueryBuilder Without<T>(ComponentId<T> componentId) where T : struct
    {
        TinyEcs.tecs_query_without(_query, componentId.Id);
        return this;
    }

    /// <summary>
    /// Add an optional component to the query.
    /// </summary>
    public QueryBuilder Optional<T>(ComponentId<T> componentId) where T : struct
    {
        TinyEcs.tecs_query_optional(_query, componentId.Id);
        return this;
    }

    /// <summary>
    /// Filter for components that have been changed since the last query.
    /// </summary>
    public QueryBuilder Changed<T>(ComponentId<T> componentId) where T : struct
    {
        TinyEcs.tecs_query_changed(_query, componentId.Id);
        return this;
    }

    /// <summary>
    /// Filter for components that have been added since the last query.
    /// </summary>
    public QueryBuilder Added<T>(ComponentId<T> componentId) where T : struct
    {
        TinyEcs.tecs_query_added(_query, componentId.Id);
        return this;
    }

    /// <summary>
    /// Execute the query and get an iterator.
    /// </summary>
    public QueryIterator Iter()
    {
        TinyEcs.tecs_query_build(_query);
        var iter = TinyEcs.tecs_query_iter(_query);
        return new QueryIterator(iter, _query);
    }

    /// <summary>
    /// Free the query resources (called automatically when ref struct goes out of scope).
    /// </summary>
    public void Dispose()
    {
        TinyEcs.tecs_query_free(_query);
    }
}

/// <summary>
/// Iterator for query results. Implements IEnumerator pattern.
/// </summary>
public readonly unsafe ref struct QueryIterator
{
    private readonly TinyEcs.QueryIter* _iter;
    private readonly TinyEcs.Query _query;

    internal QueryIterator(TinyEcs.QueryIter* iter, TinyEcs.Query query)
    {
        _iter = iter;
        _query = query;
    }

    /// <summary>
    /// Get the number of entities in the current chunk.
    /// </summary>
    public readonly int Count => TinyEcs.tecs_iter_count(_iter);

    /// <summary>
    /// Get entities in the current chunk.
    /// </summary>
    public readonly ReadOnlySpan<Entity> Entities
    {
        get
        {
            var entities = TinyEcs.tecs_iter_entities(_iter);
            var count = Count;
            return new ReadOnlySpan<Entity>(entities, count);
        }
    }

    /// <summary>
    /// Get a span of components for the current chunk.
    /// Automatically handles both managed and unmanaged components.
    /// Uses the component ID to find the correct column in the archetype.
    /// </summary>
    public readonly Span<T> Column<T>(ComponentId<T> componentId) where T : struct
    {
        // Find the archetype column index for this component ID
        var columnIndex = TinyEcs.tecs_iter_column_index(_iter, componentId.Id);

        if (columnIndex < 0)
        {
            // Component not found in this archetype (shouldn't happen if query is correct)
            return Span<T>.Empty;
        }

        if (componentId.StorageProvider != null)
        {
            // Managed component
            return ManagedStorage.GetManagedSpan<T>(_iter, columnIndex);
        }
        else
        {
            // Unmanaged component
            var count = TinyEcs.tecs_iter_count(_iter);
            var ptr = TinyEcs.tecs_iter_column(_iter, columnIndex);
            return new Span<T>(ptr, count);
        }
    }

    /// <summary>
    /// Get the changed ticks for a component column.
    /// Use this to determine which entities had their components modified.
    /// </summary>
    public readonly ReadOnlySpan<TinyEcs.Tick> ChangedTicks<T>(ComponentId<T> componentId) where T : struct
    {
        var columnIndex = TinyEcs.tecs_iter_column_index(_iter, componentId.Id);
        if (columnIndex < 0) return ReadOnlySpan<TinyEcs.Tick>.Empty;

        var ticks = TinyEcs.tecs_iter_changed_ticks(_iter, columnIndex);
        if (ticks == null) return ReadOnlySpan<TinyEcs.Tick>.Empty;

        return new ReadOnlySpan<TinyEcs.Tick>(ticks, Count);
    }

    /// <summary>
    /// Get the added ticks for a component column.
    /// Use this to determine when components were added to entities.
    /// </summary>
    public readonly ReadOnlySpan<TinyEcs.Tick> AddedTicks<T>(ComponentId<T> componentId) where T : struct
    {
        var columnIndex = TinyEcs.tecs_iter_column_index(_iter, componentId.Id);
        if (columnIndex < 0) return ReadOnlySpan<TinyEcs.Tick>.Empty;

        var ticks = TinyEcs.tecs_iter_added_ticks(_iter, columnIndex);
        if (ticks == null) return ReadOnlySpan<TinyEcs.Tick>.Empty;

        return new ReadOnlySpan<TinyEcs.Tick>(ticks, Count);
    }

    /// <summary>
    /// Move to the next chunk.
    /// </summary>
    public readonly bool MoveNext()
    {
        return TinyEcs.tecs_iter_next(_iter);
    }

    /// <summary>
    /// Free the iterator resources.
    /// </summary>
    public readonly void Dispose()
    {
        TinyEcs.tecs_query_free(_query);
    }
}
