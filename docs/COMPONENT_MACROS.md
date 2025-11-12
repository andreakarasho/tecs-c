# TinyEcs Component Declaration & Registration Macros

## Overview

TinyEcs provides optional macros to reduce boilerplate when declaring and registering components. You can choose between manual declaration or using macros based on your preference.

## Two Approaches

### Approach 1: Manual Declaration (Traditional)

```c
/* Declare component struct */
typedef struct { float x, y; } Position;
typedef struct { float x, y; } Velocity;

/* Declare component ID variables */
static tecs_component_id_t Position_id;
static tecs_component_id_t Velocity_id;

/* In main or setup function */
int main(void) {
    tecs_world_t* world = tecs_world_new();

    /* Register components manually */
    Position_id = tecs_register_component(world, "Position", sizeof(Position));
    Velocity_id = tecs_register_component(world, "Velocity", sizeof(Velocity));

    /* Use the IDs */
    tecs_entity_t entity = tecs_entity_new(world);
    Position pos = {10.0f, 20.0f};
    tecs_set(world, entity, Position_id, &pos, sizeof(Position));
}
```

**Pros:**
- ✅ Explicit and clear
- ✅ Full control over naming
- ✅ Easy to understand for beginners
- ✅ Standard C patterns

**Cons:**
- ⚠️ More verbose (3 lines per component)
- ⚠️ Easy to forget to declare ID variable
- ⚠️ Manual name string matching

### Approach 2: Macro Declaration ⭐ **RECOMMENDED**

```c
/* Declare components with macros */
TECS_DECLARE_COMPONENT(Position);
struct Position { float x, y; };

TECS_DECLARE_COMPONENT(Velocity);
struct Velocity { float x, y; };

/* In main or setup function */
int main(void) {
    tecs_world_t* world = tecs_world_new();

    /* Register components using macro */
    TECS_COMPONENT_REGISTER(world, Position);
    TECS_COMPONENT_REGISTER(world, Velocity);

    /* Use the IDs (automatically available as Position_id, Velocity_id) */
    tecs_entity_t entity = tecs_entity_new(world);
    Position pos = {10.0f, 20.0f};
    tecs_set(world, entity, Position_id, &pos, sizeof(Position));
}
```

**Pros:**
- ✅ Less boilerplate (2 lines per component)
- ✅ Automatic ID variable declaration
- ✅ Automatic name string generation (no typos)
- ✅ Consistent naming convention enforced
- ✅ Type-safe

**Cons:**
- ⚠️ Slightly less explicit
- ⚠️ Requires understanding macro expansion

## Macro Reference

### `TECS_DECLARE_COMPONENT(Name)`

**Purpose:** Declares a component typedef and its associated ID variable.

**Expands to:**
```c
typedef struct Name Name;
static tecs_component_id_t Name##_id = 0
```

**Usage:**
```c
TECS_DECLARE_COMPONENT(Health);
struct Health { int value; };
```

**Result:**
- Creates typedef `Health` for `struct Health`
- Creates variable `static tecs_component_id_t Health_id = 0`

### `TECS_COMPONENT_REGISTER(world, Name)`

**Purpose:** Registers a component with the world and stores the ID.

**Expands to:**
```c
(Name##_id = tecs_register_component(world, #Name, sizeof(Name)))
```

**Usage:**
```c
TECS_COMPONENT_REGISTER(world, Health);
```

**Result:**
- Registers component "Health" with size `sizeof(Health)`
- Stores component ID in `Health_id`
- Returns the component ID (can be used in expressions)

### `TECS_SET(world, entity, Type, value)`

**Purpose:** Simplifies setting component values by automatically handling ID, address, and size.

**Expands to:**
```c
do { Type _tmp = value; tecs_set(world, entity, Type##_id, &_tmp, sizeof(Type)); } while(0)
```

**Usage:**
```c
Position pos = {10.0f, 20.0f};
TECS_SET(world, entity, Position, pos);

// Or inline with compound literal (C99)
TECS_SET(world, entity, Position, ((Position){10.0f, 20.0f}));
```

**Result:**
- Automatically appends `_id` to type name
- Takes address of value automatically
- Calculates `sizeof(Type)` automatically
- Thread-safe (uses local temporary variable)

**Benefits:**
- Reduces `tecs_set(world, entity, Position_id, &pos, sizeof(Position))` to `TECS_SET(world, entity, Position, pos)`
- Less error-prone (no manual sizeof or address-of operators)
- Consistent with `TECS_DECLARE_COMPONENT` naming convention

### Query Building Macros

TinyEcs also provides macros to simplify query construction, automatically appending `_id` to component type names.

**Available macros:**
- `TECS_QUERY_WITH(query, Type)` - Add component to query (must have)
- `TECS_QUERY_WITHOUT(query, Type)` - Exclude component from query (must not have)
- `TECS_QUERY_OPTIONAL(query, Type)` - Optional component access
- `TECS_QUERY_CHANGED(query, Type)` - Only entities where component changed
- `TECS_QUERY_ADDED(query, Type)` - Only entities where component just added

**Before:**
```c
tecs_query_t* query = tecs_query_new(world);
tecs_query_with(query, Position_id);
tecs_query_with(query, Velocity_id);
tecs_query_without(query, Dead_id);
tecs_query_build(query);
```

**After:**
```c
tecs_query_t* query = tecs_query_new(world);
TECS_QUERY_WITH(query, Position);
TECS_QUERY_WITH(query, Velocity);
TECS_QUERY_WITHOUT(query, Dead);
tecs_query_build(query);
```

### Tag Components

**Purpose**: Add zero-sized tag components to entities.

**Available macros:**
- `TECS_ADD_TAG(world, entity, Type)` - Add tag component to entity

**Before (manual):**
```c
tecs_add_tag(world, player_entity, Player_id);
tecs_add_tag(world, enemy_entity, Enemy_id);
```

**After (macro):**
```c
TECS_ADD_TAG(world, player_entity, Player);
TECS_ADD_TAG(world, enemy_entity, Enemy);
```

### Change Detection

**Purpose**: Manually mark components as changed to trigger change detection filters.

**Available macros:**
- `TECS_MARK_CHANGED(world, entity, Type)` - Mark component as changed

**Before (manual):**
```c
tecs_mark_changed(world, entity, Position_id);
```

**After (macro):**
```c
TECS_MARK_CHANGED(world, entity, Position);
```

## Complete Examples

### Example 1: Simple Game Components

```c
#define TINYECS_IMPLEMENTATION
#include "tinyecs.h"

/* Declare all components */
TECS_DECLARE_COMPONENT(Position);
struct Position { float x, y; };

TECS_DECLARE_COMPONENT(Velocity);
struct Velocity { float x, y; };

TECS_DECLARE_COMPONENT(Health);
struct Health { int current, max; };

TECS_DECLARE_COMPONENT(Player);
struct Player {};  /* Tag component */

int main(void) {
    tecs_world_t* world = tecs_world_new();

    /* Register all components */
    TECS_COMPONENT_REGISTER(world, Position);
    TECS_COMPONENT_REGISTER(world, Velocity);
    TECS_COMPONENT_REGISTER(world, Health);
    TECS_COMPONENT_REGISTER(world, Player);

    /* Create player entity */
    tecs_entity_t player = tecs_entity_new(world);

    Position pos = {0.0f, 0.0f};
    Velocity vel = {1.0f, 0.0f};
    Health hp = {100, 100};
    Player tag = {};

    TECS_SET(world, player, Position, pos);
    TECS_SET(world, player, Velocity, vel);
    TECS_SET(world, player, Health, hp);
    TECS_SET(world, player, Player, tag);

    /* Create query for moving entities */
    tecs_query_t* query = tecs_query_new(world);
    TECS_QUERY_WITH(query, Position);
    TECS_QUERY_WITH(query, Velocity);
    tecs_query_build(query);

    /* Update positions */
    tecs_query_iter_t* iter = tecs_query_iter_cached(query);
    while (tecs_query_next(iter)) {
        int count = tecs_iter_count(iter);
        Position* positions = (Position*)tecs_iter_column(iter, 0);
        Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            positions[i].x += velocities[i].x;
            positions[i].y += velocities[i].y;
        }
    }

    /* Cleanup */
    tecs_query_free(query);
    tecs_world_free(world);
    return 0;
}
```

### Example 2: Bevy Integration

```c
#define TINYECS_IMPLEMENTATION
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs.h"
#include "tinyecs_bevy.h"

/* Components */
TECS_DECLARE_COMPONENT(Position);
struct Position { float x, y; };

TECS_DECLARE_COMPONENT(Velocity);
struct Velocity { float x, y; };

/* Movement system */
static void movement_system(tbevy_app_t* app, void* user_data) {
    tecs_query_t* query = (tecs_query_t*)user_data;

    tecs_query_iter_t* iter = tecs_query_iter_cached(query);
    while (tecs_query_next(iter)) {
        int count = tecs_iter_count(iter);
        Position* positions = (Position*)tecs_iter_column(iter, 0);
        Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            positions[i].x += velocities[i].x;
            positions[i].y += velocities[i].y;
        }
    }
}

int main(void) {
    tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_SINGLE);
    tecs_world_t* world = tbevy_app_world(app);

    /* Register components */
    TECS_COMPONENT_REGISTER(world, Position);
    TECS_COMPONENT_REGISTER(world, Velocity);

    /* Create query */
    tecs_query_t* query = tecs_query_new(world);
    TECS_QUERY_WITH(query, Position);
    TECS_QUERY_WITH(query, Velocity);
    tecs_query_build(query);

    /* Add system */
    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, movement_system, query),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    /* Spawn entities */
    for (int i = 0; i < 1000; i++) {
        tecs_entity_t e = tecs_entity_new(world);
        Position pos = {0.0f, 0.0f};
        Velocity vel = {1.0f, 1.0f};
        TECS_SET(world, e, Position, pos);
        TECS_SET(world, e, Velocity, vel);
    }

    /* Run 60 frames */
    for (int i = 0; i < 60; i++) {
        tbevy_app_update(app);
    }

    tecs_query_free(query);
    tbevy_app_free(app);
    return 0;
}
```

## Best Practices

### When to Use Macros

✅ **Use macros when:**
- Starting a new project
- Want consistent naming conventions
- Have many components to declare
- Want to reduce boilerplate
- Working in a team (enforces consistency)

### When to Use Manual Declaration

✅ **Use manual when:**
- Working with existing codebase that uses manual style
- Need custom naming schemes
- Want maximum explicitness
- Learning ECS concepts (more visible)

### Mixed Usage

You can mix both approaches in the same codebase:

```c
/* Use macros for most components */
TECS_DECLARE_COMPONENT(Position);
struct Position { float x, y; };

TECS_DECLARE_COMPONENT(Velocity);
struct Velocity { float x, y; };

/* Use manual for special cases */
typedef struct {
    char data[256];
} LargeComponent;
static tecs_component_id_t LargeComponent_id;

/* Register both styles */
int main(void) {
    tecs_world_t* world = tecs_world_new();

    TECS_COMPONENT_REGISTER(world, Position);
    TECS_COMPONENT_REGISTER(world, Velocity);

    LargeComponent_id = tecs_register_component(
        world, "LargeComponent", sizeof(LargeComponent)
    );
}
```

## Macro Expansion

Understanding how macros expand can help debug issues:

### Input:
```c
TECS_DECLARE_COMPONENT(Position);
struct Position { float x, y; };

TECS_COMPONENT_REGISTER(world, Position);
```

### Expands to:
```c
typedef struct Position Position;
static tecs_component_id_t Position_id = 0;
struct Position { float x, y; };

(Position_id = tecs_register_component(world, "Position", sizeof(Position)));
```

## Troubleshooting

### Error: "redefinition of Position_id"

**Cause:** Component declared twice

**Fix:** Check for duplicate `TECS_DECLARE_COMPONENT` calls

```c
// Wrong
TECS_DECLARE_COMPONENT(Position);
TECS_DECLARE_COMPONENT(Position);  // Duplicate!

// Correct
TECS_DECLARE_COMPONENT(Position);
struct Position { float x, y; };
```

### Error: "use of undeclared identifier Position_id"

**Cause:** Component not declared before use

**Fix:** Add `TECS_DECLARE_COMPONENT` at the top of the file

```c
// Wrong
tecs_set(world, e, Position_id, &pos, sizeof(Position));  // Position_id not declared yet!
TECS_DECLARE_COMPONENT(Position);

// Correct
TECS_DECLARE_COMPONENT(Position);
struct Position { float x, y; };
// ... later ...
tecs_set(world, e, Position_id, &pos, sizeof(Position));
```

### Error: "incomplete type struct Position"

**Cause:** Forgot to define the struct body after declaration

**Fix:** Add the struct definition

```c
// Wrong
TECS_DECLARE_COMPONENT(Position);
// Missing struct definition!

// Correct
TECS_DECLARE_COMPONENT(Position);
struct Position { float x, y; };
```

## See Also

- [tinyecs.h](tinyecs.h) - Core API documentation
- [example_bevy_performance.c](example_bevy_performance.c) - Macro usage example
- [example_iter_library_cache.c](example_iter_library_cache.c) - Macro usage example
- [ITERATOR_CACHING.md](ITERATOR_CACHING.md) - Iterator optimization guide

## Summary

| Aspect | Manual | Macro |
|--------|--------|-------|
| **Lines of code** | 3 per component | 2 per component |
| **Type safety** | ✅ Yes | ✅ Yes |
| **Explicitness** | ✅ Very explicit | ⚠️ Less explicit |
| **Name consistency** | ⚠️ Manual | ✅ Automatic |
| **Boilerplate** | ⚠️ More | ✅ Less |
| **Best for** | Learning, existing code | New projects, teams |

**Available Macros:**

**Component Declaration & Registration:**
- `TECS_DECLARE_COMPONENT(Name)` - Declare component typedef and ID variable
- `TECS_COMPONENT_REGISTER(world, Name)` - Register component with world

**Component Operations:**
- `TECS_SET(world, entity, Type, value)` - Set component value
- `TECS_GET(world, entity, Type)` - Get component pointer
- `TECS_HAS(world, entity, Type)` - Check if entity has component
- `TECS_UNSET(world, entity, Type)` - Remove component from entity
- `TECS_ADD_TAG(world, entity, Type)` - Add zero-sized tag component
- `TECS_MARK_CHANGED(world, entity, Type)` - Manually mark component as changed

**Query Building:**
- `TECS_QUERY_WITH(query, Type)` - Add required component to query
- `TECS_QUERY_WITHOUT(query, Type)` - Exclude component from query
- `TECS_QUERY_OPTIONAL(query, Type)` - Add optional component to query
- `TECS_QUERY_CHANGED(query, Type)` - Filter by changed components
- `TECS_QUERY_ADDED(query, Type)` - Filter by newly added components

**Bevy Layer Macros:**

**Entity Commands:**
- `TBEVY_ENTITY_INSERT(ec, Type, value)` - Insert component on entity
- `TBEVY_ENTITY_REMOVE(ec, Type)` - Remove component from entity

**Entity-Specific Observers:**
- `TBEVY_ENTITY_OBSERVE_INSERT(ec, Type, callback, user_data)` - Observe component insert
- `TBEVY_ENTITY_OBSERVE_REMOVE(ec, Type, callback, user_data)` - Observe component remove
- `TBEVY_ENTITY_OBSERVE_ADD(ec, Type, callback, user_data)` - Observe component add

**Global Observers:**
- `TBEVY_ADD_OBSERVER(app, trigger_type, Type, callback, user_data)` - Generic observer
- `TBEVY_ADD_OBSERVER_INSERT(app, Type, callback, user_data)` - Observe all inserts
- `TBEVY_ADD_OBSERVER_REMOVE(app, Type, callback, user_data)` - Observe all removes
- `TBEVY_ADD_OBSERVER_ADD(app, Type, callback, user_data)` - Observe all adds

**Recommendation:** Use these macros for new projects to reduce boilerplate and ensure consistency. All macros automatically append `_id` to type names, eliminating manual ID variable references. Use `tecs_query_build(query)` directly to finalize the query (no macro needed). Both macro and manual approaches are fully supported and can be mixed as needed.
