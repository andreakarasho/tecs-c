# TinyECS .NET Bindings

C# bindings for TinyECS and TinyECS.Bevy, providing a high-performance Entity Component System for .NET applications.

## Overview

This project provides P/Invoke bindings to the TinyECS C library, allowing you to use the ECS architecture in your .NET applications. It includes:

- **TinyEcs.cs**: Core ECS bindings (World, Entity, Component management)
- **TinyEcsBevy.cs**: Bevy-inspired scheduling system bindings (Application, Systems, Resources, Stages)
- **Program.cs**: Example demonstrating both basic ECS usage and Bevy-style application structure

## Features

### Core ECS (TinyEcs)
- Entity creation and management with generation counters
- Component registration and manipulation
- Archetype-based storage for cache-friendly performance
- Entity hierarchy support (parent-child relationships)
- Change detection with tick tracking

### Bevy-Style API (TinyEcsBevy)
- Application framework with stages (Startup, PreUpdate, Update, PostUpdate, etc.)
- System scheduling with dependency ordering
- Resource management
- Run conditions for systems
- Single-threaded and multi-threaded execution modes

## Prerequisites

Before running the .NET project, you need to build the native TinyECS libraries:

### Building the Native Libraries

1. Navigate to the root tecs-c directory
2. Build the shared libraries using make:

```bash
# On Windows (with MinGW/MSYS2)
make shared

# On Linux/macOS
make shared
```

This will create the following files:
- `libtinyecs.dll` (Windows) or `libtinyecs.so` (Linux) or `libtinyecs.dylib` (macOS)
- `libtinyecs_bevy.dll` (Windows) or `libtinyecs_bevy.so` (Linux) or `libtinyecs_bevy.dylib` (macOS)

3. Copy these libraries to one of the following locations:
   - The same directory as your .NET executable (recommended for development)
   - A directory in your system PATH
   - The `TinyEcsBindings` project directory

## Building and Running

1. Navigate to the bindings directory:
```bash
cd dotnet-bindings/TinyEcsBindings
```

2. Build the project:
```bash
dotnet build
```

3. Run the example:
```bash
dotnet run
```

## Example Usage

### Basic ECS Operations

```csharp
using TinyEcsBindings;
using static TinyEcsBindings.TinyEcs;

unsafe
{
    // Create world
    var world = tecs_world_new();

    // Define components
    struct Position { public float X, Y; }

    // Register component
    var posId = RegisterComponent<Position>(world, "Position");

    // Retrieve component ID by name (useful for dynamic component lookup)
    var foundId = GetComponentId(world, "Position");
    // Returns ComponentId with Value = 0 if component not found

    // Create entity
    var entity = tecs_entity_new(world);

    // Add component
    var pos = new Position { X = 10.0f, Y = 20.0f };
    Set(world, entity, posId, pos);

    // Query component
    var posPtr = Get<Position>(world, entity, posId);
    Console.WriteLine($"Position: ({posPtr->X}, {posPtr->Y})");

    // Cleanup
    tecs_world_free(world);
}
```

### Bevy-Style Application

```csharp
using TinyEcsBindings;
using static TinyEcsBindings.TinyEcsBevy;
using SystemContext = TinyEcsBindings.TinyEcsBevy.SystemContext;

struct GameState { public int Score; public int Frame; }

unsafe class MyApp
{
    private static ulong s_gameStateId;

    static void StartupSystem(SystemContext* ctx, void* userData)
    {
        var state = GetResourceMut<GameState>(ctx->app, s_gameStateId);
        if (state != null)
        {
            state->Score = 100;
            state->Frame = 0;
        }
    }

    static void UpdateSystem(SystemContext* ctx, void* userData)
    {
        var state = GetResourceMut<GameState>(ctx->app, s_gameStateId);
        if (state != null)
        {
            state->Frame++;
            state->Score += 10;
            Console.WriteLine($"Frame {state->Frame}: Score = {state->Score}");
        }
    }

    static void Main()
    {
        // Create app
        var app = tbevy_app_new(ThreadingMode.SingleThreaded);

        // Register and insert resource
        s_gameStateId = RegisterResourceType<GameState>("GameState");
        InsertResource(app, s_gameStateId, new GameState { Score = 0, Frame = 0 });

        // Add startup system
        var startupStage = tbevy_stage_default(StageId.Startup);
        var startupBuilder = tbevy_app_add_system(app, StartupSystem, null);
        tbevy_system_in_stage(startupBuilder, startupStage);
        tbevy_system_build(startupBuilder);

        // Add update system
        var updateStage = tbevy_stage_default(StageId.Update);
        var updateBuilder = tbevy_app_add_system(app, UpdateSystem, null);
        tbevy_system_in_stage(updateBuilder, updateStage);
        tbevy_system_build(updateBuilder);

        // Run app
        tbevy_app_run_startup(app);
        for (int i = 0; i < 3; i++)
        {
            tbevy_app_update(app);
        }

        // Cleanup
        tbevy_app_free(app);
    }
}
```

**Important Notes:**
- System functions receive a `SystemContext*` which contains world, commands, and app pointers
- Access the app pointer via `ctx->app` to get resources
- Store delegates in static fields or class members to prevent garbage collection
- Resources must be registered before insertion

## Project Structure

```
dotnet-bindings/
├── TinyEcsBindings/
│   ├── TinyEcs.cs           # Core ECS bindings
│   ├── TinyEcsBevy.cs       # Bevy-style API bindings
│   ├── Program.cs           # Example application
│   └── TinyEcsBindings.csproj
└── README.md
```

## API Reference

### TinyEcs Core Types

- `Entity`: Represents an entity (64-bit ID with index and generation)
- `ComponentId`: Component type identifier
- `Tick`: World tick counter for change detection

### TinyEcs Core Functions

- `tecs_world_new()` / `tecs_world_free()`: World lifecycle
- `tecs_entity_new()` / `tecs_entity_delete()`: Entity lifecycle
- `tecs_register_component()`: Register component types
- `tecs_get_component_id()`: Retrieve component ID by name (returns 0 if not found)
- `tecs_set()` / `tecs_get()`: Component access
- `tecs_has()` / `tecs_unset()`: Component queries and removal

### TinyEcsBevy Types

- `ThreadingMode`: SingleThreaded or MultiThreaded execution
- `StageId`: Built-in stages (Startup, Update, etc.)
- `SystemFunction`: Delegate for system functions
- `RunConditionFunction`: Delegate for conditional execution

### TinyEcsBevy Functions

- `tbevy_app_new()` / `tbevy_app_free()`: Application lifecycle
- `tbevy_app_add_system()`: Register systems
- `tbevy_system_in_stage()` / `tbevy_system_after()`: System ordering
- `tbevy_app_insert_resource()` / `tbevy_app_get_resource()`: Resource management
- `tbevy_app_run_startup()` / `tbevy_app_update()`: Execution control

## Notes

- The bindings use `unsafe` code and raw pointers for P/Invoke interop
- All components must be `unmanaged` types (no managed references)
- Remember to free worlds and apps to prevent memory leaks
- The native libraries must be in the DLL search path at runtime

## License

MIT License - Same as TinyECS C implementation
