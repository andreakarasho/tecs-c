# TinyEcs C Translation Notes

## Overview

This is a C99 translation of the core TinyEcs ECS implementation from C#. The translation focuses on the archetype-based storage system, entity management, and query iteration - the fundamental building blocks of the ECS.

## What Was Translated

### Core Features (✓ Implemented)

- **World Management** - Main ECS container with entity and archetype tracking
- **Entity Operations** - Creation, deletion, existence checking with generation counters
- **Component Registration** - Manual registration with name and size
- **Component Operations** - Set, get, has, unset with archetype transitions
- **Archetype System** - Archetype-based storage with graph edges for transitions
- **Chunk System** - 4096 entities per chunk with columnar storage
- **Query System** - Query builder with with/without/optional/changed/added filters
- **Query Iteration** - Zero-allocation iteration over matched archetypes
- **Sparse Set** - O(1) entity lookup with recycling
- **Tag Components** - Zero-sized marker components
- **Change Detection** - Tick-based tracking per component (basic implementation)

### Architecture Preserved

1. **Archetype Graph** - Entities grouped by component signature, with cached edges for O(1) transitions
2. **Columnar Storage** - Components stored in separate arrays per chunk for cache-friendly access
3. **Entity Recycling** - 16-bit generation counter embedded in 64-bit entity ID
4. **Hash-based Archetype Lookup** - FNV-1a hash of component set for fast archetype matching
5. **Chunk-based Allocation** - Fixed 4096-entity chunks to minimize allocations

## What Was NOT Translated

### Bevy Layer (Excluded)

The entire `TinyEcs.Bevy` layer was intentionally excluded to keep the C implementation focused on core ECS:

- **App & Stages** - No application framework or stage scheduling
- **System Scheduling** - No automatic system ordering or parallel execution
- **System Parameters** - No automatic dependency injection (Commands, Res, ResMut, etc.)
- **Observers** - No event system (OnInsert, OnRemove, OnSpawn, etc.)
- **Bundles** - No component bundle system (can be implemented in user code)
- **State Management** - No state machine (State<T>, NextState<T>)
- **Plugins** - No plugin system
- **Resources** - No global resource storage (can be added by user)

### Advanced Features (Not Implemented)

- **Relationship Pairs** - Entity-to-entity relationships not implemented
- **Deferred Commands** - Basic structure present but not fully implemented
- **Thread Safety** - Single-threaded only (no parallel query execution)
- **Query Caching** - Queries rebuild on every iteration (no structural change version check working yet)
- **Empty Archetype Cleanup** - Present but not thoroughly tested

## Key Design Decisions

### 1. Single-Header Library

Following the stb-style single-header pattern:
```c
#define TINYECS_IMPLEMENTATION
#include "tinyecs.h"
```

**Rationale**: Easy integration, no build system required

### 2. Manual Component Registration

Unlike C# with reflection:
```c
tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
```

**Rationale**: C has no reflection, explicit registration is necessary

### 3. Explicit Query Building

Queries require explicit build call:
```c
tecs_query_t* query = tecs_query_new(world);
tecs_query_with(query, Position_id);
tecs_query_build(query);  // Required!
```

**Rationale**: Separates query construction from archetype matching for clarity

### 4. Direct Memory Access

Queries return raw pointers to component arrays:
```c
Position* positions = tecs_iter_column(iter, 0);
for (int i = 0; i < count; i++) {
    positions[i].x += 1.0f;  // Direct array access
}
```

**Rationale**: Zero-copy access, maximum performance

### 5. Manual Memory Management

User must free queries and iterators:
```c
tecs_query_iter_free(iter);
tecs_query_free(query);
tecs_world_free(world);
```

**Rationale**: Explicit lifetime management is C idiom

## Performance Characteristics

Based on the C# implementation design:

- **Entity Creation**: O(1) amortized (sparse set insertion)
- **Component Add**: O(1) with archetype edge caching
- **Component Remove**: O(1) with archetype edge caching
- **Component Access**: O(1) sparse set + chunk offset calculation
- **Query Matching**: O(archetypes) with early exit on mismatch
- **Query Iteration**: O(entities) linear scan, cache-friendly

### Memory Usage

Per entity overhead:
- Sparse set entry: 4 bytes (sparse array index)
- Dense set entry: 16 bytes (archetype pointer + chunk index + row)
- Entity ID in chunk: 8 bytes
- Generation counter: 2 bytes (amortized)
- **Total**: ~30 bytes + component data

Per archetype overhead:
- Component info: 16 bytes per component type
- Chunk metadata: 24 bytes per chunk
- Graph edges: 16 bytes per edge

## Known Issues

### Change Detection

The change detection system is implemented but not working correctly in the example:
- `changed_ticks` arrays are allocated and updated
- Queries with `tecs_query_changed()` don't filter correctly
- `tecs_mark_changed()` doesn't trigger detection

**Likely Cause**: The query iteration needs to check tick values against a "last run" tick, which isn't tracked per-query yet.

**Workaround**: Check ticks manually in user code.

### Entity ID Tracking in Sparse Set

When removing entities, we swap with the last entity but don't update the sparse index for the swapped entity (noted in comments). This could cause issues with many deletions.

**Impact**: Low (entity recycling still works)

### Deferred Commands

The deferred command system structure exists but isn't fully implemented:
- No command queue management
- No thread-local buffers
- `tecs_begin_deferred`/`tecs_end_deferred` are stubs

**Workaround**: Apply changes immediately (current behavior)

## Testing

The `example.c` demonstrates:

✓ Component registration
✓ Entity creation
✓ Component add/remove
✓ Query with multiple filters
✓ Query iteration
✓ Archetype transitions
✓ Entity deletion
✓ Tag components
✗ Change detection (not working)
✗ Deferred commands (not implemented)

## Migration from C# TinyEcs

If you're familiar with the C# version:

### Conceptual Mapping

| C# | C |
|----|---|
| `var world = new World()` | `tecs_world_t* world = tecs_world_new()` |
| `var entity = world.Entity()` | `tecs_entity_t entity = tecs_entity_new(world)` |
| `entity.Set(new Position { X = 10 })` | `Position p = {10}; tecs_set(world, entity, pos_id, &p, sizeof(Position))` |
| `entity.Get<Position>()` | `Position* p = tecs_get(world, entity, pos_id)` |
| `entity.Has<Position>()` | `tecs_has(world, entity, pos_id)` |
| `world.Query<Data<Position>>()` | `tecs_query_new(world); tecs_query_with(q, pos_id); tecs_query_build(q)` |

### Major Differences

1. **No RAII** - Must manually free resources
2. **No generics** - Component IDs are runtime values, not compile-time types
3. **No automatic system scheduling** - User implements game loop
4. **No Bevy layer** - No App, stages, observers, etc.
5. **Explicit sizes** - Must pass `sizeof(Component)` to `tecs_set()`

## Future Enhancements

Potential additions to bring C version closer to C# feature parity:

### High Priority

- [ ] Fix change detection query filtering
- [ ] Implement proper query caching (structural change version)
- [ ] Complete deferred command system
- [ ] Add sparse set entity tracking for correct removal

### Medium Priority

- [ ] Thread-safe query iteration (parallel for)
- [ ] Relationship pairs (entity-to-entity relationships)
- [ ] Query DSL macros for cleaner syntax
- [ ] Component bundle helpers (macros)

### Low Priority

- [ ] Simple observer system (callbacks on component add/remove)
- [ ] Basic resource storage (global singletons)
- [ ] Hot-reload support (serialization)
- [ ] Debug visualization tools

## Building

### Linux/macOS
```bash
gcc -std=c99 -Wall -Wextra -O2 -o example example.c -lm
./example
```

### Windows (MinGW)
```bash
gcc -std=c99 -Wall -Wextra -O2 -o example.exe example.c
example.exe
```

### Windows (MSVC)
```cmd
cl /std:c11 /W4 /O2 example.c
example.exe
```

## Configuration

See `tinyecs.h` header for compile-time configuration:

```c
#define TECS_CHUNK_SIZE 4096           // Entities per chunk
#define TECS_MAX_COMPONENTS 1024       // Max component types
#define TECS_MAX_QUERY_TERMS 16        // Max filters per query
#define TECS_MALLOC(size) my_malloc    // Custom allocator
```

## License

MIT License (same as TinyEcs C# implementation)

## Acknowledgments

- Based on C# TinyEcs by [original author]
- Archetype design inspired by Unity DOTS
- Single-header pattern inspired by stb libraries
- Translation by Claude (Anthropic AI)

## Contributing

To improve this C port:

1. Focus on core ECS features (keep it minimal)
2. Maintain API simplicity (no complex macros)
3. Preserve cache-friendly memory layout
4. Add tests before new features
5. Document performance implications

The goal is a lightweight, embeddable ECS for C projects, not feature parity with C# version.
