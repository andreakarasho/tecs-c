using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace TinyEcsBindings;

/// <summary>
/// C# bindings for TinyECS - A high-performance Entity Component System
/// </summary>
public static unsafe class TinyEcs
{
    private const string DllName = "tinyecs";

    // ============================================================================
    // Opaque Struct Types
    // ============================================================================

    /// <summary>Opaque pointer to ECS world</summary>
    public readonly struct World
    {
        public readonly IntPtr Handle;
        public World(IntPtr handle) => Handle = handle;
        public static implicit operator IntPtr(World w) => w.Handle;
        public static implicit operator World(IntPtr ptr) => new World(ptr);
    }

    /// <summary>Opaque pointer to children collection</summary>
    public readonly struct Children
    {
        public readonly IntPtr Handle;
        public Children(IntPtr handle) => Handle = handle;
        public static implicit operator IntPtr(Children c) => c.Handle;
        public static implicit operator Children(IntPtr ptr) => new Children(ptr);
    }

    /// <summary>Opaque pointer to query</summary>
    public readonly struct Query
    {
        public readonly IntPtr Handle;
        public Query(IntPtr handle) => Handle = handle;
        public static implicit operator IntPtr(Query q) => q.Handle;
        public static implicit operator Query(IntPtr ptr) => new Query(ptr);
    }

    /// <summary>Query iterator structure (must match C struct layout)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct QueryIter
    {
        public IntPtr query;                // tecs_query_t*
        public int archetype_index;         // int
        public int chunk_index;             // int
        public IntPtr current_chunk;        // tecs_chunk_t*
        public IntPtr current_archetype;    // tecs_archetype_t*
    }

    // ============================================================================
    // Type Definitions
    // ============================================================================

    public readonly struct Entity
    {
        public readonly ulong Value;

        public Entity(ulong value) => Value = value;

        public uint Index => (uint)(Value & 0xFFFFFFFF);
        public uint Generation => (uint)(Value >> 32);

        public static readonly Entity Null = new(0);
        public bool IsNull => Value == 0;
    }

    public readonly struct ComponentId
    {
        public readonly uint Value;

        public ComponentId(uint value) => Value = value;

        public static readonly ComponentId Invalid = new(0xFFFFFFFF);
    }

    public readonly struct Tick
    {
        public readonly ulong Value;

        public Tick(ulong value) => Value = value;
    }

    // ============================================================================
    // World Management
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern World tecs_world_new();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_world_free(World world);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_world_update(World world);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Tick tecs_world_tick(World world);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int tecs_world_entity_count(World world);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_world_clear(World world);

    // ============================================================================
    // Component Registration
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ComponentId tecs_register_component(World world, byte* name, int size);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ComponentId tecs_get_component_id(World world, byte* name);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void* tecs_get_default_storage_provider();

    public static ComponentId RegisterComponent<T>(World world, string name) where T : notnull
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* namePtr = nameBytes)
        {
            return tecs_register_component(world, namePtr, Unsafe.SizeOf<T>());
        }
    }

    public static ComponentId GetComponentId(World world, string name)
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* namePtr = nameBytes)
        {
            return tecs_get_component_id(world, namePtr);
        }
    }

    // ============================================================================
    // Entity Operations
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Entity tecs_entity_new(World world);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Entity tecs_entity_new_with_id(World world, Entity id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_entity_delete(World world, Entity entity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool tecs_entity_exists(World world, Entity entity);

    // ============================================================================
    // Component Operations
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_set(World world, Entity entity, ComponentId component_id, void* data, int size);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void* tecs_get(World world, Entity entity, ComponentId component_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void* tecs_get_const(World world, Entity entity, ComponentId component_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool tecs_has(World world, Entity entity, ComponentId component_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_unset(World world, Entity entity, ComponentId component_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_add_tag(World world, Entity entity, ComponentId tag_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_mark_changed(World world, Entity entity, ComponentId component_id);

    // ============================================================================
    // Helper Methods
    // ============================================================================

    public static void Set<T>(World world, Entity entity, ComponentId componentId, T value) where T : unmanaged
    {
        T* ptr = &value;
        tecs_set(world, entity, componentId, ptr, sizeof(T));
    }

    public static T* Get<T>(World world, Entity entity, ComponentId componentId) where T : unmanaged
    {
        return (T*)tecs_get(world, entity, componentId);
    }

    public static T* GetConst<T>(World world, Entity entity, ComponentId componentId) where T : unmanaged
    {
        return (T*)tecs_get_const(world, entity, componentId);
    }

    // ============================================================================
    // Hierarchy Operations
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ComponentId tecs_get_parent_component_id(World world);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ComponentId tecs_get_children_component_id(World world);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_add_child(World world, Entity parent, Entity child);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_remove_child(World world, Entity parent, Entity child);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_remove_all_children(World world, Entity parent);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Entity tecs_get_parent(World world, Entity child);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool tecs_has_parent(World world, Entity child);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int tecs_child_count(World world, Entity parent);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool tecs_is_ancestor_of(World world, Entity ancestor, Entity descendant);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool tecs_is_descendant_of(World world, Entity descendant, Entity ancestor);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int tecs_get_hierarchy_depth(World world, Entity entity);

    // ============================================================================
    // Query API
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Query tecs_query_new(World world);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tecs_query_free")]
    private static extern void tecs_query_free_impl(IntPtr query);

    public static void tecs_query_free(Query query)
    {
        tecs_query_free_impl(query.Handle);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_query_with(Query query, ComponentId component_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_query_without(Query query, ComponentId component_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_query_optional(Query query, ComponentId component_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_query_changed(Query query, ComponentId component_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_query_added(Query query, ComponentId component_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_query_build(Query query);

    // ============================================================================
    // Query Iterator API
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern QueryIter* tecs_query_iter(Query query);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern QueryIter* tecs_query_iter_cached(Query query);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_query_iter_init(QueryIter* iter, Query query);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool tecs_iter_next(QueryIter* iter);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tecs_query_iter_free(QueryIter iter);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int tecs_iter_count(QueryIter* iter);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Entity* tecs_iter_entities(QueryIter* iter);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void* tecs_iter_column(QueryIter* iter, int index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int tecs_iter_column_index(QueryIter* iter, ComponentId componentId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void* tecs_iter_storage_provider(QueryIter* iter, int index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Tick* tecs_iter_changed_ticks(QueryIter* iter, int index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Tick* tecs_iter_added_ticks(QueryIter* iter, int index);

    // ============================================================================
    // Query Helper Methods
    // ============================================================================

    public static T* IterColumn<T>(QueryIter* iter, int index) where T : unmanaged
    {
        return (T*)tecs_iter_column(iter, index);
    }
}
