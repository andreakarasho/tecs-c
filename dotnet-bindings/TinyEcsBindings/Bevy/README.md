# Bevy-Style ECS API for TinyECS C# Bindings

This folder contains the Bevy-inspired query API for TinyECS, featuring:

## Features

### ✅ Source-Generated Query Types
- **Data<T1...T8>**: Generated query data types for 1-8 components (31KB)
- **And<TFilter1...TFilter8>**: Generated filter combinators for 2-8 filters (19KB)

### ✅ Core Interfaces
- `IData<T>`: Interface for query data types
- `IFilter<T>`: Interface for query filters
- `IQueryIterator<T>`: Base iterator interface

### ✅ Clean API
- Auto-component registration via `world.Component<T>()`
- No world parameter needed on iterator methods
- QueryIterator carries TinyWorld reference

## Usage Example

```csharp
using TinyEcsBindings;
using TinyEcsBindings.Bevy;

// Create world and entities
using var world = new TinyWorld();

var entity = world.Create();
world.Set(entity, new Position { X = 10.0f, Y = 20.0f });
world.Set(entity, new Velocity { X = 1.0f, Y = 0.5f });

// Query with auto-registration - clean API!
var query = world.Query()
    .With<Position>(world)
    .With<Velocity>(world)
    .Iter();

while (query.MoveNext())
{
    // No world parameter needed - uses iterator.World automatically!
    var positions = query.Column<Position>();
    var velocities = query.Column<Velocity>();

    // Process entire chunk (SIMD-friendly)
    for (int i = 0; i < query.Count; i++)
    {
        positions[i].X += velocities[i].X;
        positions[i].Y += velocities[i].Y;
    }
}
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

## Regenerating Code

To regenerate the source code after modifying templates:

```bash
cd dotnet-bindings/TinyEcsBindings/Bevy
t4 Data.tt
t4 Filters.tt
```

## Architecture

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

## Design Decisions

1. **TinyWorld stored in QueryIterator**: Eliminates passing world everywhere
2. **Extension methods**: Clean, ergonomic API without polluting core types
3. **Source generation via T4**: Type-safe, zero-reflection, compile-time code gen
4. **Chunk-based iteration**: SIMD-friendly data layout matching Bevy's approach
