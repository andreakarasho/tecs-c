using System.Runtime.InteropServices;

namespace TinyEcsBindings;

/// <summary>
/// C# bindings for TinyECS.Bevy - Bevy-inspired scheduling layer for TinyECS
/// </summary>
public static unsafe class TinyEcsBevy
{
    private const string DllName = "tinyecs_bevy";

    // ============================================================================
    // Opaque Struct Types
    // ============================================================================

    /// <summary>Opaque pointer to Bevy app</summary>
    public readonly struct App
    {
        public readonly IntPtr Handle;
        public App(IntPtr handle) => Handle = handle;
        public static implicit operator IntPtr(App a) => a.Handle;
        public static implicit operator App(IntPtr ptr) => new App(ptr);
    }

    /// <summary>Opaque pointer to stage</summary>
    public readonly struct Stage
    {
        public readonly IntPtr Handle;
        public Stage(IntPtr handle) => Handle = handle;
        public static implicit operator IntPtr(Stage s) => s.Handle;
        public static implicit operator Stage(IntPtr ptr) => new Stage(ptr);
    }

    /// <summary>Opaque pointer to system builder</summary>
    public readonly struct SystemBuilder
    {
        public readonly IntPtr Handle;
        public SystemBuilder(IntPtr handle) => Handle = handle;
        public static implicit operator IntPtr(SystemBuilder sb) => sb.Handle;
        public static implicit operator SystemBuilder(IntPtr ptr) => new SystemBuilder(ptr);
    }

    /// <summary>Entity commands builder</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EntityCommands
    {
        public Commands commands;
        public TinyEcs.Entity entity_id;
    }

    /// <summary>Opaque pointer to commands</summary>
    public readonly struct Commands
    {
        public readonly IntPtr Handle;
        public Commands(IntPtr handle) => Handle = handle;
        public static implicit operator IntPtr(Commands c) => c.Handle;
        public static implicit operator Commands(IntPtr ptr) => new Commands(ptr);
    }

    // ============================================================================
    // Type Definitions
    // ============================================================================

    public enum ThreadingMode
    {
        SingleThreaded = 0,
        MultiThreaded = 1
    }

    public enum StageId
    {
        Startup = 0,
        First = 1,
        PreUpdate = 2,
        Update = 3,
        PostUpdate = 4,
        Last = 5
    }

    // System context structure
    [StructLayout(LayoutKind.Sequential)]
    public struct SystemContext
    {
        public TinyEcs.World world;     // tecs_world_t*
        public Commands commands;        // tbevy_commands_t*
        public App app;                  // tbevy_app_t*
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SystemFunction(SystemContext* ctx, void* user_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool ShouldQuitFunction(App app);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool RunConditionFunction(App app);

    // ============================================================================
    // Application Management
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern App tbevy_app_new(ThreadingMode threading_mode);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tbevy_app_free(App app);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern TinyEcs.World tbevy_app_world(App app);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tbevy_app_run_startup(App app);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tbevy_app_update(App app);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tbevy_app_run(App app, ShouldQuitFunction should_quit);

    // ============================================================================
    // Stage Management
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Stage tbevy_stage_default(StageId stage_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Stage tbevy_stage_custom(byte* name);

    public static Stage CreateCustomStage(string name)
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* namePtr = nameBytes)
        {
            return tbevy_stage_custom(namePtr);
        }
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Stage tbevy_app_add_stage(App app, Stage stage);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tbevy_stage_after(Stage stage, Stage after);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tbevy_stage_before(Stage stage, Stage before);

    // ============================================================================
    // System Management
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern SystemBuilder tbevy_app_add_system(App app, SystemFunction fn, void* user_data);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern SystemBuilder tbevy_system_in_stage(SystemBuilder builder, Stage stage);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern SystemBuilder tbevy_system_label(SystemBuilder builder, byte* label);

    public static SystemBuilder SetSystemLabel(SystemBuilder builder, string label)
    {
        var labelBytes = System.Text.Encoding.UTF8.GetBytes(label + "\0");
        fixed (byte* labelPtr = labelBytes)
        {
            return tbevy_system_label(builder, labelPtr);
        }
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern SystemBuilder tbevy_system_after(SystemBuilder builder, byte* label);

    public static SystemBuilder SetSystemAfter(SystemBuilder builder, string label)
    {
        var labelBytes = System.Text.Encoding.UTF8.GetBytes(label + "\0");
        fixed (byte* labelPtr = labelBytes)
        {
            return tbevy_system_after(builder, labelPtr);
        }
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern SystemBuilder tbevy_system_before(SystemBuilder builder, byte* label);

    public static SystemBuilder SetSystemBefore(SystemBuilder builder, string label)
    {
        var labelBytes = System.Text.Encoding.UTF8.GetBytes(label + "\0");
        fixed (byte* labelPtr = labelBytes)
        {
            return tbevy_system_before(builder, labelPtr);
        }
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern SystemBuilder tbevy_system_single_threaded(SystemBuilder builder);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern SystemBuilder tbevy_system_run_if(SystemBuilder builder, RunConditionFunction condition, void* user_data);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tbevy_system_build(SystemBuilder builder);

    // ============================================================================
    // Resource Management
    // ============================================================================

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ResourceDestructor(void* data);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong tbevy_register_resource_type(byte* name, nuint size, ResourceDestructor? destructor);

    public static ulong RegisterResourceType<T>(string name) where T : unmanaged
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* namePtr = nameBytes)
        {
            return tbevy_register_resource_type(namePtr, (nuint)sizeof(T), null);
        }
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tbevy_app_insert_resource(App app, ulong type_id, void* data, nuint size);

    public static void InsertResource<T>(App app, ulong type_id, T value) where T : unmanaged
    {
        T* ptr = &value;
        tbevy_app_insert_resource(app, type_id, ptr, (nuint)sizeof(T));
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void* tbevy_app_get_resource(App app, ulong type_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void* tbevy_app_get_resource_mut(App app, ulong type_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool tbevy_app_has_resource(App app, ulong type_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tbevy_app_remove_resource(App app, ulong type_id);

    public static T* GetResource<T>(App app, ulong type_id) where T : unmanaged
    {
        return (T*)tbevy_app_get_resource(app, type_id);
    }

    public static T* GetResourceMut<T>(App app, ulong type_id) where T : unmanaged
    {
        return (T*)tbevy_app_get_resource_mut(app, type_id);
    }

    // ============================================================================
    // Commands API
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tbevy_commands_init(Commands* commands, App app);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tbevy_commands_free(Commands* commands);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern EntityCommands tbevy_commands_spawn(Commands commands);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern EntityCommands tbevy_commands_entity(Commands commands, TinyEcs.Entity entity_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tbevy_commands_apply(Commands commands);

    // ============================================================================
    // Entity Commands API
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern EntityCommands* tbevy_entity_insert(EntityCommands* ec, TinyEcs.ComponentId component_id, void* data, int size);

    public static EntityCommands* EntityInsert<T>(EntityCommands* ec, TinyEcs.ComponentId componentId, T value) where T : unmanaged
    {
        T* ptr = &value;
        return tbevy_entity_insert(ec, componentId, ptr, sizeof(T));
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern EntityCommands* tbevy_entity_remove(EntityCommands* ec, TinyEcs.ComponentId component_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern EntityCommands* tbevy_entity_despawn(EntityCommands* ec);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ObserverCallback(TinyEcs.Entity entity, void* user_data);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern EntityCommands* tbevy_entity_observe(EntityCommands* ec,
        TinyEcs.ComponentId component_id,
        ObserverCallback callback,
        void* user_data);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern TinyEcs.Entity tbevy_entity_id(in EntityCommands ec);
}
