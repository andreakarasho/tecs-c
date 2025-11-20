# Bevy-Style ECS API for TinyECS C# Bindings

This folder contains the Bevy-inspired ECS API for TinyECS, featuring a complete implementation of Bevy's core patterns including queries, systems, schedules, observers, and more.

## Features

### ✅ Source-Generated Query Types
- **Data<T1...T8>**: Generated query data types for 1-8 components (31KB)
- **And<TFilter1...TFilter8>**: Generated filter combinators for 2-8 filters (19KB)

### ✅ System Parameters
- **Query<TData, TFilter>**: Query entities with components and filters
- **Commands**: Deferred entity/component mutations
- **Res<T> / ResMut<T>**: Read/write access to world resources
- **Local<T>**: System-local state that persists between runs
- **EventReader<T> / EventWriter<T>**: Event publishing/subscribing system
- **In<T>**: Receive piped input from previous systems

### ✅ Query Filters
- **With<T>**: Require component presence
- **Without<T>**: Require component absence
- **Optional<T>**: Component may or may not be present
- **Changed<T>**: Only entities where component was modified
- **Added<T>**: Only entities where component was just added

### ✅ Schedules & Stages
- **Custom Schedules**: Named schedules (Main, FixedUpdate, Render, Startup)
- **Stages**: Ordered execution within schedules (PreUpdate, Update, PostUpdate)
- **Parallel Execution**: Automatic parallelization based on system access patterns
- **System Dependencies**: `.Before()`, `.After()`, `.Label()` for ordering

### ✅ Observers
- **Component Events**: React to Added, Removed, Changed events
- **System Parameters**: Observers support Query, Commands, and other system params
- **Typed Triggers**: `On<TComponent, TEvent>` provides entity and component data

### ✅ System Piping
- **Chain Systems**: Pipe output from one system to the next
- **Typed Outputs**: Type-safe output types (Success, Failure, Count, Result<T>)
- **Composable**: Build complex system chains with `.Pipe()`

### ✅ Exclusive Systems
- **Exclusive World Access**: Systems that never run in parallel
- **`.Exclusive()` API**: Mark systems for exclusive execution

### ✅ State Management
- **State<T>**: Type-safe state machines
- **State Transitions**: Automatic transition system
- **Run Conditions**: `.InState<T>()` for conditional execution

### ✅ Run Conditions
- **Custom Conditions**: `RunCondition` delegates for conditional system execution
- **Combinators**: Combine conditions with logical operators

### ✅ Core Interfaces
- `IData<T>`: Interface for query data types
- `IFilter<T>`: Interface for query filters
- `IQueryIterator<T>`: Base iterator interface
- `ISystemParam`: Interface for system parameters
- `ISystem`: Interface for systems
- `IObserver`: Interface for observers

### ✅ Clean API
- Auto-component registration via `world.Component<T>()`
- No world parameter needed on iterator methods
- QueryIterator carries TinyWorld reference
- Fluent configuration APIs

## Usage Examples

### Basic App with Systems

```csharp
using TinyEcsBindings;
using TinyEcsBindings.Bevy;

// Define components
struct Position { public float X, Y; }
struct Velocity { public float X, Y; }

// Create app and add systems
var app = new App()
    .AddStartupSystem((Commands commands) =>
    {
        // Spawn entities at startup
        commands.Spawn()
            .Insert(new Position { X = 0, Y = 0 })
            .Insert(new Velocity { X = 1, Y = 1 });
    })
    .AddSystem((Query<Data<Position, Velocity>> query) =>
    {
        // Movement system - runs every frame
        foreach (var (entity, positions, velocities) in query)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i].X += velocities[i].X;
                positions[i].Y += velocities[i].Y;
            }
        }
    });

// Run the app
app.Run();
```

### Observers with System Parameters

```csharp
struct Health { public int Value; }
struct Damage { public int Amount; }

app.AddObserver((On<Health, Changed> trigger, Commands commands) =>
{
    // React when health changes
    var health = trigger.Component;
    if (health.Value <= 0)
    {
        commands.Despawn(trigger.Entity);
    }
})
.AddObserver((On<Damage, Added> trigger, Query<Data<Health>> query) =>
{
    // Apply damage when Damage component is added
    var damage = trigger.Component;
    // Query and modify health...
});
```

### System Piping

```csharp
// Define output types
public readonly struct PlayerCount : ISystemOutput
{
    public int Count { get; init; }
}

// Pipe systems together
app.AddPipedSystem(
    // Source system counts players
    () => new PlayerCount { Count = 42 },
    // Target system receives the count
    (In<PlayerCount> input, Commands commands) =>
    {
        Console.WriteLine($"Players: {input.Value.Count}");
    }
);
```

### Custom Schedules

```csharp
// Create a custom schedule
var fixedUpdate = new Schedule("FixedUpdate")
    .AddStage(new Stage("Physics"));

app.AddSchedule(fixedUpdate)
   .AddSystemToSchedule("FixedUpdate", "Physics", physicsSystem);

// Run custom schedule at fixed timestep
while (running)
{
    if (accumulator >= fixedTimestep)
    {
        app.RunSchedule("FixedUpdate");
        accumulator -= fixedTimestep;
    }
    app.Update(); // Main schedule
}
```

### State Management

```csharp
enum GameState { Menu, Playing, Paused }

app.AddState(GameState.Menu)
   .AddSystem((Res<State<GameState>> state) =>
   {
       // System runs regardless of state
   })
   .RunIf(InState(GameState.Playing),
       (Query<Data<Position, Velocity>> query) =>
       {
           // Only runs when in Playing state
       });
```

### Exclusive Systems

```csharp
// System that needs exclusive world access
app.AddSystem((Query<Data<Transform>> query) =>
{
    // Heavy operation that can't run in parallel
    RebuildSpatialIndex(query);
})
.Exclusive(); // Never runs in parallel with other systems
```

## API Reference

### System Parameters

All system parameters implement `ISystemParam` and can be used in any system or observer:

```csharp
// Query entities with components
Query<Data<Position, Velocity>>
Query<Data<Health>, With<Player>>

// Deferred commands
Commands commands
commands.Spawn().Insert(new Position());
commands.Despawn(entity);

// Resources (read-only)
Res<GameConfig> config

// Resources (mutable)
ResMut<Score> score

// System-local state
Local<int> frameCounter

// Events
EventReader<CollisionEvent> reader
EventWriter<DamageEvent> writer

// Piped input
In<MyOutput> input
```

### Query Filters

Filters control which entities match a query:

```csharp
// Entity must have component
With<Health>

// Entity must not have component
Without<Dead>

// Component is optional (may be null ref)
Optional<Armor>

// Only entities where component changed
Changed<Position>

// Only entities where component was just added
Added<Enemy>

// Combine multiple filters
And<With<Player>, Without<Dead>>
```

### Observer Events

Observers react to component lifecycle events:

```csharp
// Component was added to entity
Added

// Component was removed from entity
Removed

// Component value was changed
Changed

// Any event on component
Any
```

### System Configuration

Configure system execution with fluent API:

```csharp
app.AddSystem(mySystem)
   .Label("movement")              // Add label for ordering
   .Before("physics")              // Run before labeled system
   .After("input")                 // Run after labeled system
   .InSet<GameplaySystems>()       // Add to system set
   .Exclusive()                    // Never run in parallel
   .RunIf(condition);              // Conditional execution
```

### System Output Types

Built-in output types for system piping:

```csharp
// Simple continuation signal
Continue

// Success with optional message
Success(message: "Loaded")

// Failure with error
Failure(error: "Not found")

// Integer count
Count(value: 42)

// Generic result type
Result<T>.Ok(value)
Result<T>.Err("error")
```

## Generated Code

### Data Types (Data.g.cs)
Generated from `Data.tt` template:
- `Data<T0>` - Single component queries
- `Data<T0, T1>` - Two component queries
- ... up to `Data<T0...T7>` - Eight component queries

Each generated type includes:
- Per-entity iteration with `Entity`, `Item0`, `Item1`, etc.
- Deconstruction into component spans
- Clean integration with QueryIterator

### Filter Combinators (Filters.g.cs)
Generated from `Filters.tt` template:
- `And<TFilter1, TFilter2>` - Combine 2 filters
- `And<TFilter1, TFilter2, TFilter3>` - Combine 3 filters
- ... up to `And<TFilter1...TFilter8>` - Combine 8 filters

### System Adapters (SystemAdapters.g.cs)
Generated from `SystemAdapters.tt` template:
- Overloads for 0-8 system parameters
- Automatic parameter initialization and fetching
- Type-safe system registration

### Observer Extensions (Observer.g.cs)
Generated from `Observer.tt` template:
- Overloads for 1-8 system parameters
- Automatic parameter initialization and fetching
- Type-safe observer registration with system params

## Regenerating Code

To regenerate the source code after modifying templates:

```bash
cd dotnet-bindings/TinyEcsBindings/Bevy
t4 Data.tt
t4 Filters.tt
t4 SystemAdapters.tt
t4 Observer.tt
```

## Architecture

### Query Pipeline
```
TinyWorld.Query()
    ↓
QueryBuilder (stores TinyWorld reference)
    ↓
QueryIterator (carries TinyWorld via .World property)
    ↓
Extension methods use iterator.World automatically
    ↓
Clean API: query.Column<T>() instead of query.Column<T>(world)
```

### System Execution Pipeline
```
App.Update()
    ↓
Schedule.Run()
    ↓
Stage.Execute()
    ↓
Parallel batching (respects access patterns)
    ↓
SystemNode.Run()
    ↓
System parameters initialized and fetched
    ↓
User code executes
```

### Observer Pipeline
```
Commands.Insert<T>() / Remove<T>()
    ↓
Command.Execute()
    ↓
ObserverRegistry.Trigger()
    ↓
Match observers by (ComponentType, EventType)
    ↓
Initialize and fetch system parameters
    ↓
Observer callback executes with On<> trigger
```

## Design Decisions

1. **TinyWorld stored in QueryIterator**: Eliminates passing world everywhere
2. **Extension methods**: Clean, ergonomic API without polluting core types
3. **Source generation via T4**: Type-safe, zero-reflection, compile-time code gen
4. **Chunk-based iteration**: SIMD-friendly data layout matching Bevy's approach
5. **Deferred commands**: Safe mutation during iteration, commands apply between systems
6. **System parameter dependency injection**: Declarative, type-safe world access
7. **Parallel system execution**: Automatic parallelization based on read/write access
8. **Observer registry as resource**: Enables Commands to trigger observers without coupling
9. **Ref struct triggers**: Zero-allocation observer triggers with `On<TComponent, TEvent>`
10. **Generic system piping**: Type-safe output chaining with `In<T>` parameter

## File Structure

```
Bevy/
├── README.md                  # This file
├── Data.tt                    # T4 template for Data<T...> types
├── Data.g.cs                  # Generated query data types
├── Filters.tt                 # T4 template for And<T...> filters
├── Filters.g.cs               # Generated filter combinators
├── Filters.cs                 # Built-in filters (With, Without, etc.)
├── QueryIterator.cs           # Core query iteration
├── SystemAdapters.tt          # T4 template for system adapters
├── SystemAdapters.g.cs        # Generated system adapters
├── SystemParam.cs             # System parameter interfaces
├── Query.cs                   # Query system parameter
├── Commands.cs                # Deferred command system
├── Resources.cs               # Res<T> and ResMut<T>
├── Local.cs                   # Local<T> system-local state
├── Events.cs                  # Event system (EventReader/Writer)
├── App.cs                     # Application and world container
├── Stage.cs                   # Execution stage with parallel batching
├── Schedule.cs                # Custom schedules
├── SystemSets.cs              # System grouping and ordering
├── RunConditions.cs           # Conditional system execution
├── State.cs                   # State machine management
├── Observer.cs                # Observer base implementation
├── Observer.tt                # T4 template for observer params
├── Observer.g.cs              # Generated observer extensions
├── SystemPiping.cs            # System piping infrastructure
└── ExclusiveSystem.cs         # Exclusive system support
```
