# TinyEcs.Bevy C - Bevy-Inspired Scheduling Layer

Single-header scheduling and systems framework built on top of TinyEcs.

## Features

- **Application Framework** - App structure with stages and system scheduling
- **System Scheduling** - Topological ordering with label-based dependencies
- **Resources** - Global singleton storage (Res/ResMut pattern)
- **Commands** - Deferred entity/component operations
- **Observers** - React to component lifecycle events
- **Events** - Decoupled communication with double-buffered channels
- **State Machines** - OnEnter/OnExit callbacks for state transitions
- **Component Bundles** - Spawn entities with multiple components

## Quick Start

```c
#define TINYECS_IMPLEMENTATION
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs.h"
#include "tinyecs_bevy.h"

typedef struct { float x, y; } Position;
typedef struct { float x, y; } Velocity;

static tecs_component_id_t Position_id;
static tecs_component_id_t Velocity_id;

void movement_system(tbevy_app_t* app, void* user_data) {
    tecs_query_t* query = tecs_query_new(tbevy_app_world(app));
    tecs_query_with(query, Position_id);
    tecs_query_with(query, Velocity_id);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_query_next(iter)) {
        int count = tecs_iter_count(iter);
        Position* positions = tecs_iter_column(iter, 0);
        Velocity* velocities = tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            positions[i].x += velocities[i].x * 0.016f;
            positions[i].y += velocities[i].y * 0.016f;
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

int main(void) {
    // Create app
    tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_AUTO);

    // Register components
    Position_id = tecs_register_component(tbevy_app_world(app), "Position", sizeof(Position));
    Velocity_id = tecs_register_component(tbevy_app_world(app), "Velocity", sizeof(Velocity));

    // Add system
    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, movement_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    // Run app
    tbevy_app_run(app, should_quit_callback);

    // Cleanup
    tbevy_app_free(app);
    return 0;
}
```

## Core Concepts

### Application & Stages

The app runs systems in stages each frame:

1. **Startup** - Runs once (world setup)
2. **First** - First regular stage
3. **PreUpdate** - Before main logic
4. **Update** - Main gameplay systems
5. **PostUpdate** - Reactions and derived state
6. **Last** - Rendering, cleanup

```c
tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_AUTO);

// Custom stage
tbevy_stage_t* rendering = tbevy_stage_custom("Rendering");
tbevy_app_add_stage(app, rendering);
tbevy_stage_after(rendering, tbevy_stage_default(TBEVY_STAGE_UPDATE));

tbevy_app_run_startup(app);  // Run startup once
tbevy_app_update(app);        // Run one frame
tbevy_app_run(app, quit_fn);  // Run until quit
```

### System Scheduling

Systems are functions that run each frame:

```c
void my_system(tbevy_app_t* app, void* user_data) {
    // System logic here
}

// Add with fluent builder API
tbevy_system_build(
    tbevy_system_before(
        tbevy_system_after(
            tbevy_system_label(
                tbevy_system_in_stage(
                    tbevy_app_add_system(app, my_system, NULL),
                    tbevy_stage_default(TBEVY_STAGE_UPDATE)
                ),
                "my_system"
            ),
            "input"
        ),
        "render"
    )
);
```

**System Ordering:**
- `.Label("name")` - Give system a name for dependencies
- `.After("label")` - Run after labeled system
- `.Before("label")` - Run before labeled system
- `.SingleThreaded()` - Force single-threaded execution
- `.RunIf(condition_fn, data)` - Conditional execution

Systems run in **declaration order** when no dependencies exist.

### Resources

Global singletons accessible to all systems:

```c
typedef struct { float delta_time; } TimeResource;
static uint64_t TimeResource_id;

// Register type
TimeResource_id = TBEVY_REGISTER_RESOURCE("TimeResource", TimeResource);

// Insert resource
TimeResource time = {0.016f};
TBEVY_INSERT_RESOURCE(app, TimeResource_id, time);

// Get resource (immutable)
const TimeResource* time = TBEVY_GET_RESOURCE(app, TimeResource_id, TimeResource);

// Get resource (mutable)
TimeResource* time = TBEVY_GET_RESOURCE_MUT(app, TimeResource_id, TimeResource);

// Check existence
if (tbevy_app_has_resource(app, TimeResource_id)) { ... }

// Remove
tbevy_app_remove_resource(app, TimeResource_id);
```

### Commands

Deferred entity/component operations (applied at end of system execution):

```c
void spawn_system(tbevy_app_t* app, void* user_data) {
    tbevy_commands_t commands;
    tbevy_commands_init(&commands, app);

    // Spawn entity with components
    tbevy_entity_commands_t player = tbevy_commands_spawn(&commands);

    Position pos = {100.0f, 100.0f};
    tbevy_entity_insert(&player, Position_id, &pos, sizeof(Position));

    Health health = {100.0f};
    tbevy_entity_insert(&player, Health_id, &health, sizeof(Health));

    // Get entity ID
    tecs_entity_t entity_id = tbevy_entity_id(&player);

    // Modify existing entity
    tbevy_entity_commands_t ec = tbevy_commands_entity(&commands, entity_id);
    tbevy_entity_remove(&ec, Velocity_id);
    tbevy_entity_despawn(&ec);

    tbevy_commands_apply(&commands);
    tbevy_commands_free(&commands);
}
```

### Events

Decoupled communication between systems:

```c
typedef struct { int points; } ScoreEvent;
static uint64_t ScoreEvent_id;

// Register event type
ScoreEvent_id = TBEVY_REGISTER_EVENT("ScoreEvent", ScoreEvent);

// Send event
ScoreEvent evt = {100};
TBEVY_SEND_EVENT(app, ScoreEvent_id, evt);

// Read events
void handle_score(tbevy_app_t* app, const void* event_data, void* user_data) {
    const ScoreEvent* score = (const ScoreEvent*)event_data;
    printf("Score: %d\n", score->points);
}

void score_system(tbevy_app_t* app, void* user_data) {
    tbevy_app_read_events(app, ScoreEvent_id, handle_score, NULL);
}
```

Events use **double-buffering** - events sent this frame are read next frame.

### Observers

React to component lifecycle events:

```c
void on_health_insert(tbevy_app_t* app, tecs_entity_t entity,
                      tecs_component_id_t component_id,
                      const void* component_data, void* user_data) {
    const Health* health = (const Health*)component_data;
    printf("Entity %llu health: %.1f\n", entity, health->value);
}

// Add global observer
tbevy_app_add_observer(app, TBEVY_TRIGGER_ON_INSERT, Health_id,
                       on_health_insert, NULL);

// Add entity-specific observer
tbevy_entity_commands_t ec = tbevy_commands_spawn(&commands);
tbevy_entity_observe(&ec, TBEVY_TRIGGER_ON_REMOVE, Damage_id,
                     on_damage_removed, NULL);
```

**Trigger Types:**
- `TBEVY_TRIGGER_ON_SPAWN` - Entity created
- `TBEVY_TRIGGER_ON_DESPAWN` - Entity destroyed
- `TBEVY_TRIGGER_ON_ADD` - Component added (first time)
- `TBEVY_TRIGGER_ON_INSERT` - Component added/updated
- `TBEVY_TRIGGER_ON_REMOVE` - Component removed

### State Machines

Manage game states with automatic transitions:

```c
typedef enum { MENU, PLAYING, PAUSED } GameState;
static uint64_t GameState_id;

GameState_id = TBEVY_REGISTER_RESOURCE("GameState", GameState);

// Add state machine
tbevy_app_add_state(app, GameState_id, MENU);

// Get current state
uint32_t state = tbevy_app_get_state(app, GameState_id);

// Queue transition
tbevy_app_set_state(app, GameState_id, PLAYING);

// OnEnter/OnExit systems
void on_enter_playing(tbevy_app_t* app, void* user_data) {
    printf("Game started!\n");
}

void on_exit_playing(tbevy_app_t* app, void* user_data) {
    printf("Game paused!\n");
}

tbevy_system_build(
    tbevy_app_add_system_on_enter(app, GameState_id, PLAYING,
                                   on_enter_playing, NULL)
);

tbevy_system_build(
    tbevy_app_add_system_on_exit(app, GameState_id, PLAYING,
                                  on_exit_playing, NULL)
);
```

### Component Bundles

Spawn entities with multiple components:

```c
typedef struct {
    Position position;
    Velocity velocity;
    Health health;
} PlayerBundle;

void insert_player_bundle(void* bundle_data, tecs_world_t* world,
                          tecs_entity_t entity) {
    PlayerBundle* bundle = (PlayerBundle*)bundle_data;
    tecs_set(world, entity, Position_id, &bundle->position, sizeof(Position));
    tecs_set(world, entity, Velocity_id, &bundle->velocity, sizeof(Velocity));
    tecs_set(world, entity, Health_id, &bundle->health, sizeof(Health));
}

// Spawn with bundle
PlayerBundle player = {
    .position = {100.0f, 100.0f},
    .velocity = {10.0f, 5.0f},
    .health = {100.0f}
};

tbevy_commands_spawn_bundle(&commands, &player, insert_player_bundle);
```

## Threading

Systems can run in parallel if they don't conflict:

```c
// Auto threading (default)
tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_AUTO);

// Force single-threaded
tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_SINGLE);

// Force multi-threaded
tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_MULTI);

// Per-system override
tbevy_system_build(
    tbevy_system_single_threaded(
        tbevy_app_add_system(app, render_system, NULL)
    )
);
```

**Note:** Full parallel execution is not yet implemented. All systems currently run single-threaded.

## Configuration

```c
#define TBEVY_MAX_SYSTEMS 256        // Maximum systems per stage
#define TBEVY_MAX_STAGES 32          // Maximum custom stages
#define TBEVY_MAX_RESOURCES 128      // Maximum resource types
#define TBEVY_MAX_OBSERVERS 256      // Maximum global observers
#define TBEVY_MAX_STATE_SYSTEMS 64   // OnEnter/OnExit systems per state

#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs_bevy.h"
```

## Design

### System Execution Order

1. **Topological Sort** - Systems ordered by .After()/.Before() dependencies
2. **Declaration Order Preserved** - Systems with no dependencies run in declaration order
3. **Circular Dependency Detection** - Errors if dependency cycles exist
4. **Batching** (not yet implemented) - Non-conflicting systems run in parallel

### Resource Access Tracking (Planned)

- Systems track read/write access to resources
- Parallel execution checks for conflicts
- Read-read allowed, read-write and write-write blocked
- Currently simplified - all systems run sequentially

### Event Double-Buffering

- Events written to write buffer
- On frame end, buffers swap
- Systems read from read buffer
- Prevents event processing order issues

### Observer Queueing (Planned)

- Component operations queue observer triggers
- Triggers processed after system execution
- Prevents mid-system entity modification

## Limitations

### Not Yet Implemented

- **Parallel system execution** - All systems run sequentially
- **Resource access tracking** - No conflict detection
- **Full state transition system** - OnEnter/OnExit don't run correctly
- **Observer trigger queueing** - Observers fire immediately
- **Change detection** - Changed<T>/Added<T> filters not working
- **Query system parameters** - No automatic query injection
- **Local state** - No per-system persistent storage
- **Run conditions** - Stored but not fully tested

### Simplified vs C# Implementation

- **No Reflection** - Component/resource types manually registered
- **No Generic System Parameters** - Systems get raw app pointer
- **Explicit Query Building** - Must manually create and build queries
- **No Automatic Dependency Injection** - Systems manually get resources
- **No Advanced Scheduling** - Simplified topological sort
- **No Thread Pool** - Single-threaded execution only

## Example Patterns

### Startup vs Runtime

```c
void setup(tbevy_app_t* app, void* user_data) {
    // Runs once - world initialization
}

void update(tbevy_app_t* app, void* user_data) {
    // Runs every frame
}

tbevy_system_build(tbevy_system_in_stage(
    tbevy_app_add_system(app, setup, NULL),
    tbevy_stage_default(TBEVY_STAGE_STARTUP)
));

tbevy_system_build(tbevy_system_in_stage(
    tbevy_app_add_system(app, update, NULL),
    tbevy_stage_default(TBEVY_STAGE_UPDATE)
));
```

### System Dependencies

```c
// input_system must run first
tbevy_system_build(
    tbevy_system_label(
        tbevy_app_add_system(app, input_system, NULL),
        "input"
    )
);

// movement_system runs after input
tbevy_system_build(
    tbevy_system_after(
        tbevy_app_add_system(app, movement_system, NULL),
        "input"
    )
);

// render_system runs last
tbevy_system_build(
    tbevy_system_after(
        tbevy_app_add_system(app, render_system, NULL),
        "movement"
    )
);
```

### Conditional Systems

```c
bool only_in_playing_state(tbevy_app_t* app, void* user_data) {
    return tbevy_app_get_state(app, GameState_id) == PLAYING;
}

tbevy_system_build(
    tbevy_system_run_if(
        tbevy_app_add_system(app, gameplay_system, NULL),
        only_in_playing_state, NULL
    )
);
```

## Building

```bash
gcc -std=c99 -O2 -o game example_bevy.c
./game
```

## Status

**Alpha** - Core functionality working but incomplete:
- ✅ System scheduling with dependencies
- ✅ Resources (get/set)
- ✅ Commands (basic deferred operations)
- ✅ Events (double-buffered channels)
- ✅ Observers (global and entity-specific)
- ✅ State machines (basic)
- ✅ Component bundles
- ❌ Parallel execution
- ❌ Full state transitions
- ❌ Advanced system parameters
- ❌ Change detection in queries

## License

MIT License (same as TinyEcs C# implementation)
