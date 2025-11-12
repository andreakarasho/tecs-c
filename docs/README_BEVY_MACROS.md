# TinyEcs.Bevy Macro Reference

## Overview

TinyEcs.Bevy provides convenience macros that automatically append `_id` to component type names when calling Bevy layer functions. This reduces boilerplate and makes code more readable.

## Entity Commands Macros

### `TBEVY_ENTITY_INSERT(ec, Type, value)`

**Purpose**: Insert or update a component on an entity.

**Expands to:**
```c
do { Type _tmp = value; tbevy_entity_insert(ec, Type##_id, &_tmp, sizeof(Type)); } while(0)
```

**Before (manual):**
```c
tbevy_entity_commands_t ec = tbevy_commands_spawn(&commands);
Position pos = {10.0f, 20.0f};
tbevy_entity_insert(&ec, Position_id, &pos, sizeof(Position));
```

**After (macro):**
```c
tbevy_entity_commands_t ec = tbevy_commands_spawn(&commands);
Position pos = {10.0f, 20.0f};
TBEVY_ENTITY_INSERT(&ec, Position, pos);
```

**Benefits:**
- Automatically appends `_id` to type name
- Takes address of value automatically
- Calculates `sizeof(Type)` automatically
- Thread-safe (uses local temporary variable)

### `TBEVY_ENTITY_REMOVE(ec, Type)`

**Purpose**: Remove a component from an entity.

**Expands to:**
```c
tbevy_entity_remove(ec, Type##_id)
```

**Before (manual):**
```c
tbevy_entity_remove(&ec, Position_id);
```

**After (macro):**
```c
TBEVY_ENTITY_REMOVE(&ec, Position);
```

## Entity-Specific Observer Macros

Entity-specific observers are attached to a single entity and only fire for that entity.

### `TBEVY_ENTITY_OBSERVE_INSERT(ec, Type, callback, user_data)`

**Purpose**: Observe when a component is inserted or updated on this specific entity.

**Expands to:**
```c
tbevy_entity_observe(ec, TBEVY_TRIGGER_ON_INSERT, Type##_id, callback, user_data)
```

**Example:**
```c
static void on_health_changed(tbevy_app_t* app, tecs_entity_t entity,
                               tecs_component_id_t component_id,
                               const void* component_data, void* user_data) {
    const Health* hp = (const Health*)component_data;
    printf("Entity %llu health changed to %d\n", entity, hp->current);
}

tbevy_entity_commands_t ec = tbevy_commands_spawn(&commands);
TBEVY_ENTITY_OBSERVE_INSERT(&ec, Health, on_health_changed, NULL);
TBEVY_ENTITY_INSERT(&ec, Health, ((Health){100, 100}));
```

### `TBEVY_ENTITY_OBSERVE_REMOVE(ec, Type, callback, user_data)`

**Purpose**: Observe when a component is removed from this specific entity.

**Expands to:**
```c
tbevy_entity_observe(ec, TBEVY_TRIGGER_ON_REMOVE, Type##_id, callback, user_data)
```

**Example:**
```c
static void on_weapon_removed(tbevy_app_t* app, tecs_entity_t entity,
                               tecs_component_id_t component_id,
                               const void* component_data, void* user_data) {
    printf("Entity %llu lost weapon!\n", entity);
}

TBEVY_ENTITY_OBSERVE_REMOVE(&ec, Weapon, on_weapon_removed, NULL);
```

### `TBEVY_ENTITY_OBSERVE_ADD(ec, Type, callback, user_data)`

**Purpose**: Observe when a component is added to this entity for the first time.

**Expands to:**
```c
tbevy_entity_observe(ec, TBEVY_TRIGGER_ON_ADD, Type##_id, callback, user_data)
```

**Difference from INSERT:**
- `ON_ADD` fires only on the first insertion
- `ON_INSERT` fires on every insertion (including updates)

**Example:**
```c
static void on_armor_equipped(tbevy_app_t* app, tecs_entity_t entity,
                               tecs_component_id_t component_id,
                               const void* component_data, void* user_data) {
    printf("Entity %llu equipped armor for first time!\n", entity);
}

TBEVY_ENTITY_OBSERVE_ADD(&ec, Armor, on_armor_equipped, NULL);
```

## Global Observer Macros

Global observers fire for ALL entities in the world when the observed event occurs.

### `TBEVY_ADD_OBSERVER(app, trigger_type, Type, callback, user_data)`

**Purpose**: Generic global observer with custom trigger type.

**Expands to:**
```c
tbevy_app_add_observer(app, trigger_type, Type##_id, callback, user_data)
```

**Example:**
```c
static void on_any_health_insert(tbevy_app_t* app, tecs_entity_t entity,
                                  tecs_component_id_t component_id,
                                  const void* component_data, void* user_data) {
    printf("Some entity got health component!\n");
}

TBEVY_ADD_OBSERVER(app, TBEVY_TRIGGER_ON_INSERT, Health, on_any_health_insert, NULL);
```

### `TBEVY_ADD_OBSERVER_INSERT(app, Type, callback, user_data)`

**Purpose**: Observe all component insertions/updates globally.

**Expands to:**
```c
tbevy_app_add_observer(app, TBEVY_TRIGGER_ON_INSERT, Type##_id, callback, user_data)
```

**Example:**
```c
static void log_all_position_changes(tbevy_app_t* app, tecs_entity_t entity,
                                      tecs_component_id_t component_id,
                                      const void* component_data, void* user_data) {
    const Position* pos = (const Position*)component_data;
    printf("Entity %llu moved to (%.2f, %.2f)\n", entity, pos->x, pos->y);
}

TBEVY_ADD_OBSERVER_INSERT(app, Position, log_all_position_changes, NULL);
```

### `TBEVY_ADD_OBSERVER_REMOVE(app, Type, callback, user_data)`

**Purpose**: Observe all component removals globally.

**Expands to:**
```c
tbevy_app_add_observer(app, TBEVY_TRIGGER_ON_REMOVE, Type##_id, callback, user_data)
```

**Example:**
```c
static void on_any_player_removed(tbevy_app_t* app, tecs_entity_t entity,
                                   tecs_component_id_t component_id,
                                   const void* component_data, void* user_data) {
    printf("Player tag removed from entity %llu\n", entity);
}

TBEVY_ADD_OBSERVER_REMOVE(app, Player, on_any_player_removed, NULL);
```

### `TBEVY_ADD_OBSERVER_ADD(app, Type, callback, user_data)`

**Purpose**: Observe when component is added for the first time globally.

**Expands to:**
```c
tbevy_app_add_observer(app, TBEVY_TRIGGER_ON_ADD, Type##_id, callback, user_data)
```

**Example:**
```c
static void track_new_enemies(tbevy_app_t* app, tecs_entity_t entity,
                               tecs_component_id_t component_id,
                               const void* component_data, void* user_data) {
    int* enemy_count = (int*)user_data;
    (*enemy_count)++;
    printf("Enemy spawned! Total enemies: %d\n", *enemy_count);
}

int enemy_count = 0;
TBEVY_ADD_OBSERVER_ADD(app, Enemy, track_new_enemies, &enemy_count);
```

## Complete Example

```c
#define TINYECS_IMPLEMENTATION
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs.h"
#include "tinyecs_bevy.h"

/* Components */
TECS_DECLARE_COMPONENT(Position);
struct Position { float x, y; };

TECS_DECLARE_COMPONENT(Health);
struct Health { int current, max; };

TECS_DECLARE_COMPONENT(Player);
struct Player {};

/* Global observer for health changes */
static void on_health_changed(tbevy_app_t* app, tecs_entity_t entity,
                               tecs_component_id_t component_id,
                               const void* component_data, void* user_data) {
    const Health* hp = (const Health*)component_data;
    if (hp->current <= 0) {
        printf("Entity %llu died!\n", entity);
    }
}

/* Startup system - spawn entities */
static void startup_system(tbevy_app_t* app, void* user_data) {
    tbevy_commands_t* commands = (tbevy_commands_t*)user_data;

    /* Spawn player with entity-specific observer */
    tbevy_entity_commands_t player = tbevy_commands_spawn(commands);

    /* Attach observer BEFORE inserting components */
    TBEVY_ENTITY_OBSERVE_INSERT(&player, Health, on_health_changed, NULL);

    /* Insert components using macros */
    TBEVY_ENTITY_INSERT(&player, Position, ((Position){0.0f, 0.0f}));
    TBEVY_ENTITY_INSERT(&player, Health, ((Health){100, 100}));
    TBEVY_ENTITY_INSERT(&player, Player, ((Player){}));

    printf("Player spawned with ID: %llu\n", tbevy_entity_id(&player));
}

int main(void) {
    tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_SINGLE);
    tecs_world_t* world = tbevy_app_world(app);

    /* Register components */
    TECS_COMPONENT_REGISTER(world, Position);
    TECS_COMPONENT_REGISTER(world, Health);
    TECS_COMPONENT_REGISTER(world, Player);

    /* Add global observer for all health changes */
    TBEVY_ADD_OBSERVER_INSERT(app, Health, on_health_changed, NULL);

    /* Add startup system */
    tbevy_commands_t commands;
    tbevy_commands_init(&commands, app);

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, startup_system, &commands),
            tbevy_stage_default(TBEVY_STAGE_STARTUP)
        )
    );

    /* Run */
    tbevy_app_run_startup(app);

    tbevy_commands_free(&commands);
    tbevy_app_free(app);
    return 0;
}
```

## Observer Trigger Types

| Trigger Type | When It Fires |
|--------------|---------------|
| `TBEVY_TRIGGER_ON_SPAWN` | Entity created |
| `TBEVY_TRIGGER_ON_DESPAWN` | Entity destroyed |
| `TBEVY_TRIGGER_ON_ADD` | Component added for the first time |
| `TBEVY_TRIGGER_ON_INSERT` | Component added or updated |
| `TBEVY_TRIGGER_ON_REMOVE` | Component removed |

## Best Practices

### Entity-Specific vs Global Observers

**Use entity-specific observers when:**
- Observing a single important entity (player, boss, etc.)
- Need to react to changes on a specific instance
- Want to minimize callback overhead

**Use global observers when:**
- Need to track all entities with a component
- Implementing systems like damage tracking, stats collection
- Debugging (log all component changes)

### Observer Callback Signature

All observer callbacks must match this signature:

```c
typedef void (*tbevy_observer_fn_t)(
    tbevy_app_t* app,              /* Application instance */
    tecs_entity_t entity,           /* Entity that triggered event */
    tecs_component_id_t component_id, /* Component that changed */
    const void* component_data,     /* Pointer to component data */
    void* user_data                 /* User-provided context */
);
```

**Important:**
- `component_data` is only valid for `ON_INSERT` and `ON_REMOVE` events
- For `ON_SPAWN` and `ON_DESPAWN`, `component_data` will be `NULL`

### Chaining Entity Commands

Entity commands return a pointer to themselves for chaining:

```c
tbevy_entity_commands_t ec = tbevy_commands_spawn(&commands);

TBEVY_ENTITY_INSERT(&ec, Position, ((Position){0.0f, 0.0f}));
TBEVY_ENTITY_INSERT(&ec, Velocity, ((Velocity){1.0f, 0.0f}));
TBEVY_ENTITY_INSERT(&ec, Health, ((Health){100, 100}));

TBEVY_ENTITY_OBSERVE_INSERT(&ec, Health, on_health_changed, NULL);
TBEVY_ENTITY_OBSERVE_REMOVE(&ec, Health, on_health_removed, NULL);

tecs_entity_t entity_id = tbevy_entity_id(&ec);
```

### Observer Execution Order

Observers fire in the order they were registered:

```c
/* These will execute in order: 1 -> 2 -> 3 */
TBEVY_ADD_OBSERVER_INSERT(app, Health, callback1, NULL);  // Fires first
TBEVY_ADD_OBSERVER_INSERT(app, Health, callback2, NULL);  // Fires second
TBEVY_ADD_OBSERVER_INSERT(app, Health, callback3, NULL);  // Fires third
```

## Troubleshooting

### Observer Not Firing

**Problem:** Observer callback never gets called.

**Possible causes:**
1. Component ID mismatch - ensure you registered the component
2. Wrong trigger type - use `ON_INSERT` vs `ON_ADD`
3. Observer added after the event occurred
4. Entity-specific observer on wrong entity

**Solution:**
```c
/* Register component FIRST */
TECS_COMPONENT_REGISTER(world, Health);

/* Add observer BEFORE inserting components */
TBEVY_ADD_OBSERVER_INSERT(app, Health, callback, NULL);

/* Now insert components */
TBEVY_ENTITY_INSERT(&ec, Health, ((Health){100, 100}));
```

### Segmentation Fault in Observer

**Problem:** Crash when accessing `component_data`.

**Cause:** Accessing `component_data` on events that don't provide it.

**Solution:**
```c
static void my_observer(tbevy_app_t* app, tecs_entity_t entity,
                        tecs_component_id_t component_id,
                        const void* component_data, void* user_data) {
    /* ALWAYS check if data is available */
    if (component_data != NULL) {
        const Health* hp = (const Health*)component_data;
        printf("Health: %d/%d\n", hp->current, hp->max);
    } else {
        /* This event doesn't provide component data */
        printf("Entity event on %llu\n", entity);
    }
}
```

## See Also

- [COMPONENT_MACROS.md](COMPONENT_MACROS.md) - Core component macros
- [README_BEVY.md](README_BEVY.md) - Bevy layer overview
- [example_bevy.c](example_bevy.c) - Complete Bevy example with observers
- [tinyecs_bevy.h](tinyecs_bevy.h) - Full Bevy API documentation

## Summary

| Macro Category | Count | Purpose |
|----------------|-------|---------|
| Entity Commands | 2 | Insert/remove components on entities |
| Entity Observers | 3 | Observe events on specific entities |
| Global Observers | 4 | Observe events on all entities |

**All macros automatically append `_id` to type names**, reducing boilerplate and preventing typos. Use these macros in production code for cleaner, more maintainable entity management.
