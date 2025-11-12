# Deferred Commands and Simplified Entity Commands API

## Overview

The TinyECS Bevy layer now features a fully deferred command execution system and a simplified entity commands API that eliminates verbose boilerplate when operating on existing entities.

## Deferred Command Execution

All entity commands (spawn, insert, remove, despawn) are now queued in memory and applied atomically after each system completes execution.

**Benefits:**
- Thread-safe command execution
- Atomic application of all commands
- Prevents mid-system entity modifications
- Proper ordering guarantees

**Implementation:**
```c
void tbevy_commands_apply(tbevy_commands_t* commands) {
    if (commands->count == 0) return;

    /* Begin deferred mode - queue all operations */
    tecs_begin_deferred(commands->world);

    /* Process all queued commands */
    for (size_t i = 0; i < commands->count; i++) {
        tbevy_command_t* cmd = &commands->commands[i];
        /* ... execute command ... */
    }

    /* End deferred mode - apply all operations atomically */
    tecs_end_deferred(commands->world);

    /* Free command data and reset buffer */
    /* ... cleanup ... */
}
```

## Simplified Entity Commands API

### Old Pattern (Verbose)

Previously, operating on existing entities required manually creating an `tbevy_entity_commands_t` struct:

```c
/* Verbose - 2 lines per operation */
tbevy_entity_commands_t bullet_ec = {ctx->commands, bullet_entities[b]};
tbevy_entity_despawn(&bullet_ec);

tbevy_entity_commands_t ec = {ctx->commands, entity_id};
tbevy_entity_insert(&ec, component_id, &data, sizeof(data));
```

### New Pattern (Simplified)

Now you can call commands directly with just the commands buffer and entity ID:

```c
/* Clean - 1 line per operation */
tbevy_commands_entity_despawn(ctx->commands, bullet_entities[b]);

tbevy_commands_entity_insert(ctx->commands, entity_id, component_id, &data, sizeof(data));
```

## Available Simplified Functions

**`tbevy_commands_entity_insert(commands, entity_id, component_id, data, size)`**

Insert or update a component on an existing entity.

```c
Position pos = {.x = 10.0f, .y = 20.0f};
tbevy_commands_entity_insert(ctx->commands, entity_id, Position_id, &pos, sizeof(pos));
```

**`tbevy_commands_entity_remove(commands, entity_id, component_id)`**

Remove a component from an existing entity.

```c
tbevy_commands_entity_remove(ctx->commands, entity_id, Health_id);
```

**`tbevy_commands_entity_despawn(commands, entity_id)`**

Despawn (delete) an entity and all its components.

```c
tbevy_commands_entity_despawn(ctx->commands, entity_id);
```

## When to Use Each Pattern

**Use Simplified API** - For operations on existing entities:
```c
/* Operating on existing entities - use simplified API */
static void bullet_lifetime_system(tbevy_system_ctx_t* ctx, void* user_data) {
    /* ... get entities array ... */

    for (size_t i = 0; i < count; i++) {
        if (lifetimes[i] <= 0.0f) {
            tbevy_commands_entity_despawn(ctx->commands, entities[i]);
        }
    }
}
```

**Use Chainable API** - For spawning new entities with multiple components:
```c
/* Spawning new entity - use chainable API */
static void spawn_player(tbevy_system_ctx_t* ctx, void* user_data) {
    tbevy_entity_commands_t player = tbevy_commands_spawn(ctx->commands);

    Position pos = {.x = 0.0f, .y = 0.0f, .z = 0.0f};
    TBEVY_ENTITY_INSERT(&player, pos, Position);

    Health health = {.value = 100};
    TBEVY_ENTITY_INSERT(&player, health, Health);

    /* Chainable pattern keeps code clean when setting up new entities */
}
```

## Complete Example

```c
#define TINYECS_IMPLEMENTATION
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs.h"
#include "tinyecs_bevy.h"

typedef struct { float x, y; } Position;
typedef struct { int value; } Health;

uint64_t Position_id;
uint64_t Health_id;

/* System using simplified API for existing entities */
static void damage_system(tbevy_system_ctx_t* ctx, void* user_data) {
    tecs_query_t* q = tecs_query_new(ctx->world, "Health, Position");
    tecs_iter_t it = tecs_query_iter(ctx->world, q);

    while (tecs_query_next(&it)) {
        tecs_entity_t* entities = tecs_entities(&it);
        Health* healths = tecs_column(&it, Health, 1);
        size_t count = tecs_count(&it);

        for (size_t i = 0; i < count; i++) {
            healths[i].value -= 10;

            if (healths[i].value <= 0) {
                /* Clean despawn using simplified API */
                tbevy_commands_entity_despawn(ctx->commands, entities[i]);
            }
        }
    }

    tecs_query_free(q);
}

/* System using chainable API for spawning */
static void spawn_enemy(tbevy_system_ctx_t* ctx, void* user_data) {
    tbevy_entity_commands_t enemy = tbevy_commands_spawn(ctx->commands);

    Position pos = {.x = 100.0f, .y = 50.0f};
    TBEVY_ENTITY_INSERT(&enemy, pos, Position);

    Health health = {.value = 50};
    TBEVY_ENTITY_INSERT(&enemy, health, Health);
}

int main(void) {
    Position_id = tecs_register_component(sizeof(Position), alignof(Position), "Position");
    Health_id = tecs_register_component(sizeof(Health), alignof(Health), "Health");

    tecs_world_t* world = tecs_world_new();
    tbevy_app_t* app = tbevy_app_new(world);

    /* Register systems */
    tbevy_app_add_system(app, spawn_enemy, NULL);
    tbevy_app_add_system(app, damage_system, NULL);

    /* Run startup systems */
    tbevy_app_run_startup(app);

    /* Game loop */
    for (int frame = 0; frame < 10; frame++) {
        tbevy_app_update(app);
    }

    tbevy_app_free(app);
    return 0;
}
```

## Comparison: Before and After

### Before (Verbose)

```c
/* 4 lines per despawn operation */
for (size_t b = 0; b < bullet_count; b++) {
    if (collision_detected[b]) {
        tbevy_entity_commands_t bullet_ec = {ctx->commands, bullet_entities[b]};
        tbevy_entity_despawn(&bullet_ec);
    }
}

/* 2 lines per insert operation */
tbevy_entity_commands_t ec = {ctx->commands, entity_id};
tbevy_entity_insert(&ec, Health_id, &health, sizeof(health));
```

### After (Clean)

```c
/* 2 lines per despawn operation */
for (size_t b = 0; b < bullet_count; b++) {
    if (collision_detected[b]) {
        tbevy_commands_entity_despawn(ctx->commands, bullet_entities[b]);
    }
}

/* 1 line per insert operation */
tbevy_commands_entity_insert(ctx->commands, entity_id, Health_id, &health, sizeof(health));
```

## Benefits

1. **Less Boilerplate**: Eliminated manual struct creation for existing entity operations
2. **Cleaner Code**: 50% fewer lines for common operations
3. **Clear Intent**: Function names clearly indicate what operation is performed
4. **Consistency**: Matches modern C API design patterns
5. **Thread-Safe**: All commands are deferred and applied atomically
6. **Backward Compatible**: Chainable spawn pattern remains unchanged

## Implementation Details

All simplified functions internally call `tbevy_commands_queue()`:

```c
void tbevy_commands_entity_despawn(tbevy_commands_t* commands, tecs_entity_t entity_id) {
    tbevy_commands_queue(commands, TBEVY_CMD_DESPAWN, entity_id, 0, NULL, 0);
}

void tbevy_commands_entity_insert(tbevy_commands_t* commands, tecs_entity_t entity_id,
                                   tecs_component_id_t component_id,
                                   const void* data, size_t size) {
    tbevy_commands_queue(commands, TBEVY_CMD_INSERT, entity_id, component_id, data, size);
}

void tbevy_commands_entity_remove(tbevy_commands_t* commands, tecs_entity_t entity_id,
                                   tecs_component_id_t component_id) {
    tbevy_commands_queue(commands, TBEVY_CMD_REMOVE, entity_id, component_id, NULL, 0);
}
```

## Related Documentation

- [BEVY_RESOURCE_MACROS.md](BEVY_RESOURCE_MACROS.md) - Resource management macros
- [SUCCESS.md](SUCCESS.md) - 3D game example showcasing the Bevy layer
- [tinyecs_bevy.h](tinyecs_bevy.h) - Complete Bevy layer API reference

## Examples Using This API

All working examples have been updated to use the simplified API:

- ✅ [example_bevy_raylib_3d.c](example_bevy_raylib_3d.c) - 3D space shooter (4 despawn calls updated)
- ✅ [example_bevy_performance.c](example_bevy_performance.c) - Performance benchmark
- ✅ [example_bevy.c](example_bevy.c) - Core Bevy features
- ✅ [example_bevy_hierarchy.c](example_bevy_hierarchy.c) - Hierarchy demonstration

All examples compile and run successfully with the new API!
