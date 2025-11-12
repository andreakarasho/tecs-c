# Bevy Resource Macros

## Overview

The Bevy layer provides convenient macros for working with resources in TinyECS, following the same `Type##_id` convention used for components.

## Available Macros

### Resource Registration

**`TBEVY_REGISTER_RESOURCE(name, T)`**

Registers a new resource type with the application.

```c
// Define resource struct
typedef struct {
    float delta;
    float elapsed;
} TimeResource;

// Register the resource type
uint64_t TimeResource_id = TBEVY_REGISTER_RESOURCE("TimeResource", TimeResource);
```

### Resource Insertion

**`TBEVY_APP_INSERT_RESOURCE(app, Type, value)`** ✨ **NEW!**

Simplified macro that automatically appends `_id` to the type name. This is the recommended way to insert resources.

```c
TimeResource time_init = {0.0f, 0.0f};

// NEW: Cleaner syntax - Type name only (auto-appends _id)
TBEVY_APP_INSERT_RESOURCE(app, TimeResource, time_init);
```

**`TBEVY_INSERT_RESOURCE(app, type_id, value)`**

Lower-level macro that requires explicit type ID.

```c
TimeResource time_init = {0.0f, 0.0f};

// OLD: Requires manual _id suffix
TBEVY_INSERT_RESOURCE(app, TimeResource_id, time_init);
```

### Resource Access

**`TBEVY_GET_RESOURCE(app, type_id, T)`**

Get immutable (read-only) access to a resource.

```c
const TimeResource* time = TBEVY_GET_RESOURCE(app, TimeResource_id, TimeResource);
printf("Elapsed: %f\n", time->elapsed);
```

**`TBEVY_GET_RESOURCE_MUT(app, type_id, T)`**

Get mutable (read-write) access to a resource.

```c
TimeResource* time = TBEVY_GET_RESOURCE_MUT(app, TimeResource_id, TimeResource);
time->elapsed += time->delta;
```

## System Context Access (NEW!)

Systems now receive a `tbevy_system_ctx_t*` context instead of `tbevy_app_t*` app. This context provides:
- Direct world access via `ctx->world`
- Per-system commands via `ctx->commands`
- Resource access via new context macros

**New Resource Access Macros:**

**`TBEVY_CTX_GET_RESOURCE(ctx, Type)`** - Get immutable resource from context

**`TBEVY_CTX_GET_RESOURCE_MUT(ctx, Type)`** - Get mutable resource from context

**Benefits:**
1. Cleaner API - systems no longer need `tbevy_app_world(app)`
2. Commands automatically available - no need to pass via `user_data`
3. Consistent with `Type##_id` convention

```c
/* NEW: System with context */
static void update_time(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    /* Clean resource access with Type##_id convention */
    TimeResource* time = TBEVY_CTX_GET_RESOURCE_MUT(ctx, TimeResource);
    time->delta = GetFrameTime();
    time->elapsed += time->delta;
}

/* NEW: System with commands already in context */
static void spawn_enemy(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;  /* No longer need to pass commands! */

    /* Commands available directly in context */
    tbevy_entity_commands_t enemy = tbevy_commands_spawn(ctx->commands);
    /* ... */
}

/* OLD: System with app (deprecated pattern) */
static void update_time_old(tbevy_app_t* app, void* user_data) {
    TimeResource* time = TBEVY_GET_RESOURCE_MUT(tbevy_app_world(app), TimeResource_id, TimeResource);
    time->delta = GetFrameTime();
    time->elapsed += time->delta;
}
```

## Complete Example

```c
#define TINYECS_IMPLEMENTATION
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs.h"
#include "tinyecs_bevy.h"

/* Define resource types */
typedef struct {
    float delta;
    float elapsed;
} TimeResource;

typedef struct {
    int score;
    int lives;
} GameStats;

/* Register resource types (global scope) */
uint64_t TimeResource_id;
uint64_t GameStats_id;

int main(void) {
    /* Register resource types */
    TimeResource_id = TBEVY_REGISTER_RESOURCE("TimeResource", TimeResource);
    GameStats_id = TBEVY_REGISTER_RESOURCE("GameStats", GameStats);

    /* Create app */
    tbevy_app_t* app = tbevy_app_new(tecs_world_new());

    /* Initialize and insert resources */
    TimeResource time_init = {0.0f, 0.0f};
    GameStats stats_init = {0, 3};

    TBEVY_APP_INSERT_RESOURCE(app, TimeResource, time_init);
    TBEVY_APP_INSERT_RESOURCE(app, GameStats, stats_init);

    /* Use resources in systems */
    tbevy_app_add_system(app, update_time, NULL);
    tbevy_app_add_system(app, update_game, NULL);

    /* Run app */
    tbevy_app_run_startup(app);
    while (running) {
        tbevy_app_update(app);
    }

    tbevy_app_free(app);
    return 0;
}

/* System that updates time */
static void update_time(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    TimeResource* time = TBEVY_CTX_GET_RESOURCE_MUT(ctx, TimeResource);
    time->delta = GetFrameTime();  /* From raylib or similar */
    time->elapsed += time->delta;
}

/* System that reads time and updates game */
static void update_game(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    GameStats* stats = TBEVY_CTX_GET_RESOURCE_MUT(ctx, GameStats);

    if (time->elapsed > 60.0f) {
        stats->score += 100;  /* Bonus every minute */
    }
}
```

## Comparison: Before and After

### Before (Verbose)

```c
TimeResource time_init = {0.0f, 0.0f};
CameraResource cam_init = {/* ... */};
GameStats stats_init = {0, 0, 0};

tbevy_app_insert_resource(app, TimeResource_id, &time_init, sizeof(time_init));
tbevy_app_insert_resource(app, CameraResource_id, &cam_init, sizeof(cam_init));
tbevy_app_insert_resource(app, GameStats_id, &stats_init, sizeof(stats_init));
```

### After (Clean)

```c
TimeResource time_init = {0.0f, 0.0f};
CameraResource cam_init = {/* ... */};
GameStats stats_init = {0, 0, 0};

TBEVY_APP_INSERT_RESOURCE(app, TimeResource, time_init);
TBEVY_APP_INSERT_RESOURCE(app, CameraResource, cam_init);
TBEVY_APP_INSERT_RESOURCE(app, GameStats, stats_init);
```

## Benefits

1. **Less Boilerplate**: No need to write `&variable, sizeof(variable)` repeatedly
2. **Type Safety**: Macro ensures the correct sizeof is used
3. **Consistency**: Matches the component macro pattern (`TECS_SET`, `TBEVY_ENTITY_INSERT`)
4. **Readability**: Cleaner code that's easier to understand at a glance
5. **Convention**: Uses the same `Type##_id` suffix convention as components

## Macro Implementation

```c
/* Simplified macro using Type##_id convention (auto-appends _id) */
#define TBEVY_APP_INSERT_RESOURCE(app, Type, value) \
    do { Type _tmp = value; \
         tbevy_app_insert_resource(app, Type##_id, &_tmp, sizeof(Type)); } while(0)
```

**How it works:**
1. Takes the type name without `_id` suffix (e.g., `TimeResource`)
2. Automatically appends `_id` using token concatenation (`Type##_id`)
3. Creates a temporary copy of the value
4. Passes the address and size to the underlying function

## Related Macros

### Component Macros
- `TECS_SET(world, entity, Type, value)` - Set component on entity
- `TECS_GET(world, entity, Type)` - Get component from entity

### Bevy Entity Command Macros
- `TBEVY_ENTITY_INSERT(ec, Type, value)` - Insert component via entity commands

### Bevy Observer Macros
- `TBEVY_ADD_OBSERVER_INSERT(app, Type, callback, user_data)` - Observe component inserts
- `TBEVY_ENTITY_OBSERVE_INSERT(ec, Type, callback, user_data)` - Entity-specific observer

All these macros follow the same `Type##_id` convention for consistency across the API.

## See Also

- [COMPONENT_MACROS.md](COMPONENT_MACROS.md) - Component operation macros
- [example_bevy_raylib_3d.c](example_bevy_raylib_3d.c) - Full game using resource macros
- [tinyecs_bevy.h](tinyecs_bevy.h) - Complete Bevy layer API reference
