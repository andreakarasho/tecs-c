/*
 * TinyEcs.Bevy.h - Bevy-inspired scheduling layer for TinyEcs
 *
 * Features:
 * - Application framework with stages and system scheduling
 * - Topological system ordering with dependency resolution
 * - System parameters (Res, ResMut, Commands, Query, Events, Local)
 * - Change detection (Changed<T>, Added<T> filters)
 * - Observer system (OnInsert, OnRemove, OnSpawn, OnDespawn)
 * - Event channels with double-buffering
 * - State machines with OnEnter/OnExit callbacks
 * - Component bundles
 * - Deferred command execution
 *
 * Usage:
 *   Header-only (default):
 *     #define TINYECS_BEVY_IMPLEMENTATION
 *     #include "tinyecs.h"
 *     #include "tinyecs_bevy.h"
 *
 *   Shared library export:
 *     #define TINYECS_SHARED_LIBRARY
 *     #define TINYECS_BEVY_IMPLEMENTATION
 *     #include "tinyecs.h"
 *     #include "tinyecs_bevy.h"
 *
 *   Shared library import:
 *     #define TINYECS_SHARED_LIBRARY
 *     #include "tinyecs.h"
 *     #include "tinyecs_bevy.h"
 *
 * License: MIT
 * Based on TinyEcs.Bevy C# implementation
 */

#ifndef TINYECS_BEVY_H
#define TINYECS_BEVY_H

#include "tinyecs.h"
#include <stdbool.h>
#include <stdint.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

/* ============================================================================
 * DLL Export/Import Configuration
 * ========================================================================= */

#ifndef TBEVY_API
    #ifdef TINYECS_SHARED_LIBRARY
        #if defined(_WIN32) || defined(__CYGWIN__)
            #ifdef TINYECS_BEVY_IMPLEMENTATION
                #ifdef __GNUC__
                    #define TBEVY_API __attribute__((dllexport))
                #else
                    #define TBEVY_API __declspec(dllexport)
                #endif
            #else
                #ifdef __GNUC__
                    #define TBEVY_API __attribute__((dllimport))
                #else
                    #define TBEVY_API __declspec(dllimport)
                #endif
            #endif
        #else
            #if __GNUC__ >= 4
                #define TBEVY_API __attribute__((visibility("default")))
            #else
                #define TBEVY_API
            #endif
        #endif
    #else
        #define TBEVY_API
    #endif
#endif

/* ============================================================================
 * Configuration
 * ========================================================================= */

#ifndef TBEVY_MAX_SYSTEMS
#define TBEVY_MAX_SYSTEMS 256  /* Maximum systems per stage */
#endif

#ifndef TBEVY_MAX_STAGES
#define TBEVY_MAX_STAGES 32  /* Maximum custom stages */
#endif

#ifndef TBEVY_MAX_RESOURCES
#define TBEVY_MAX_RESOURCES 128  /* Maximum resource types */
#endif

#ifndef TBEVY_MAX_OBSERVERS
#define TBEVY_MAX_OBSERVERS 256  /* Maximum global observers */
#endif

#ifndef TBEVY_MAX_STATE_SYSTEMS
#define TBEVY_MAX_STATE_SYSTEMS 64  /* OnEnter/OnExit systems per state */
#endif

/* ============================================================================
 * Forward Declarations
 * ========================================================================= */

typedef struct tbevy_app_s tbevy_app_t;
typedef struct tbevy_system_s tbevy_system_t;
typedef struct tbevy_stage_s tbevy_stage_t;
typedef struct tbevy_commands_s tbevy_commands_t;
typedef struct tbevy_observer_s tbevy_observer_t;

/* ============================================================================
 * Enums and Constants
 * ========================================================================= */

/* Threading mode for system execution */
typedef enum {
    TBEVY_THREADING_AUTO,    /* Use multi-threading if CPU count > 1 */
    TBEVY_THREADING_SINGLE,  /* Force single-threaded execution */
    TBEVY_THREADING_MULTI    /* Force multi-threaded execution */
} tbevy_threading_mode_t;

/* Default stages */
typedef enum {
    TBEVY_STAGE_STARTUP,     /* Runs once on first frame */
    TBEVY_STAGE_FIRST,       /* First regular update stage */
    TBEVY_STAGE_PRE_UPDATE,  /* Before main update */
    TBEVY_STAGE_UPDATE,      /* Main gameplay logic */
    TBEVY_STAGE_POST_UPDATE, /* After main update */
    TBEVY_STAGE_LAST,        /* Final stage */
    TBEVY_STAGE_CUSTOM       /* Custom stage marker */
} tbevy_stage_id_t;

/* Observer trigger types */
typedef enum {
    TBEVY_TRIGGER_ON_SPAWN,    /* Entity created */
    TBEVY_TRIGGER_ON_DESPAWN,  /* Entity destroyed */
    TBEVY_TRIGGER_ON_ADD,      /* Component added (first time) */
    TBEVY_TRIGGER_ON_INSERT,   /* Component added/updated */
    TBEVY_TRIGGER_ON_REMOVE,   /* Component removed */
    TBEVY_TRIGGER_CUSTOM       /* Custom event */
} tbevy_trigger_type_t;

/* Deferred command types */
typedef enum {
    TBEVY_CMD_SPAWN,
    TBEVY_CMD_DESPAWN,
    TBEVY_CMD_INSERT,
    TBEVY_CMD_REMOVE,
    TBEVY_CMD_INSERT_RESOURCE,
    TBEVY_CMD_TRIGGER_EVENT,
    TBEVY_CMD_ATTACH_OBSERVER
} tbevy_command_type_t;

/* ============================================================================
 * Type Definitions
 * ========================================================================= */

/* System context - passed to all systems */
typedef struct {
    tecs_world_t* world;        /* Direct world access */
    tbevy_commands_t* commands; /* Per-system commands instance */
    tbevy_app_t* _app;          /* Private - for resource access */
} tbevy_system_ctx_t;

/* System function signature */
typedef void (*tbevy_system_fn_t)(tbevy_system_ctx_t* ctx, void* user_data);

/* Run condition function */
typedef bool (*tbevy_run_condition_fn_t)(tbevy_app_t* app, void* user_data);

/* Observer callback signature */
typedef void (*tbevy_observer_fn_t)(tbevy_app_t* app, tecs_entity_t entity,
                                     tecs_component_id_t component_id,
                                     const void* component_data, void* user_data);

/* Event callback signature */
typedef void (*tbevy_event_fn_t)(tbevy_app_t* app, const void* event_data, void* user_data);

/* Stage descriptor */
struct tbevy_stage_s {
    tbevy_stage_id_t id;
    char name[64];
    int order;  /* Execution order (lower = earlier) */
};

/* Resource box (type-erased container) */
typedef struct {
    void* data;
    size_t size;
    uint64_t type_id;
    void (*destructor)(void*);  /* Optional cleanup function */
} tbevy_resource_t;

/* Event channel (double-buffered event queue) */
typedef struct {
    void* read_buffer;
    void* write_buffer;
    size_t read_count, read_capacity;
    size_t write_count, write_capacity;
    size_t element_size;
    uint64_t epoch;
    uint32_t active_tick;
} tbevy_event_channel_t;

/* State machine */
typedef struct {
    uint64_t type_id;
    uint32_t current_state;
    uint32_t previous_state;
    uint32_t queued_state;
    bool has_queued;
    bool processed_this_frame;
} tbevy_state_machine_t;

/* Trigger data */
typedef struct {
    tbevy_trigger_type_t type;
    tecs_entity_t entity_id;
    tecs_component_id_t component_id;
    const void* component_data;
    bool propagate;
} tbevy_trigger_t;

/* Deferred command */
typedef struct {
    tbevy_command_type_t type;
    tecs_entity_t entity_id;
    tecs_component_id_t component_id;
    void* data;
    size_t data_size;
    union {
        tbevy_observer_fn_t observer_fn;
        void* event_data;
    };
    void* user_data;
} tbevy_deferred_command_t;

/* Commands builder */
struct tbevy_commands_s {
    tbevy_app_t* app;
    tbevy_deferred_command_t* commands;
    size_t command_count;
    size_t command_capacity;
    tecs_entity_t* spawned_entities;
    size_t spawned_count;
    size_t spawned_capacity;
};

/* Entity commands builder */
typedef struct {
    tbevy_commands_t* commands;
    tecs_entity_t entity_id;
} tbevy_entity_commands_t;

/* ============================================================================
 * Public API - App Management
 * ========================================================================= */

/* Create new application */
TBEVY_API tbevy_app_t* tbevy_app_new(tbevy_threading_mode_t threading_mode);

/* Free application and all resources */
TBEVY_API void tbevy_app_free(tbevy_app_t* app);

/* Get underlying world */
TBEVY_API tecs_world_t* tbevy_app_world(tbevy_app_t* app);

/* Run startup systems (once) */
TBEVY_API void tbevy_app_run_startup(tbevy_app_t* app);

/* Run all stages (one frame) */
TBEVY_API void tbevy_app_update(tbevy_app_t* app);

/* Run until should_quit returns true */
TBEVY_API void tbevy_app_run(tbevy_app_t* app, bool (*should_quit)(tbevy_app_t*));

/* ============================================================================
 * Public API - Stages
 * ========================================================================= */

/* Get default stage */
TBEVY_API tbevy_stage_t* tbevy_stage_default(tbevy_stage_id_t stage_id);

/* Create custom stage */
TBEVY_API tbevy_stage_t* tbevy_stage_custom(const char* name);

/* Add stage to app (returns stage for chaining) */
TBEVY_API tbevy_stage_t* tbevy_app_add_stage(tbevy_app_t* app, tbevy_stage_t* stage);

/* Set stage to run after another stage */
TBEVY_API void tbevy_stage_after(tbevy_stage_t* stage, tbevy_stage_t* after);

/* Set stage to run before another stage */
TBEVY_API void tbevy_stage_before(tbevy_stage_t* stage, tbevy_stage_t* before);

/* ============================================================================
 * Public API - Systems
 * ========================================================================= */

/* System builder handle */
typedef struct tbevy_system_builder_s tbevy_system_builder_t;

/* Add system to app (returns builder for configuration) */
TBEVY_API tbevy_system_builder_t* tbevy_app_add_system(tbevy_app_t* app, tbevy_system_fn_t fn,
                                                        void* user_data);

/* Configure system builder */
TBEVY_API tbevy_system_builder_t* tbevy_system_in_stage(tbevy_system_builder_t* builder,
                                                         tbevy_stage_t* stage);
TBEVY_API tbevy_system_builder_t* tbevy_system_label(tbevy_system_builder_t* builder,
                                                      const char* label);
TBEVY_API tbevy_system_builder_t* tbevy_system_after(tbevy_system_builder_t* builder,
                                                      const char* label);
TBEVY_API tbevy_system_builder_t* tbevy_system_before(tbevy_system_builder_t* builder,
                                                       const char* label);
TBEVY_API tbevy_system_builder_t* tbevy_system_single_threaded(tbevy_system_builder_t* builder);
TBEVY_API tbevy_system_builder_t* tbevy_system_run_if(tbevy_system_builder_t* builder,
                                                       tbevy_run_condition_fn_t condition,
                                                       void* user_data);

/* Finalize system builder (must be called!) */
TBEVY_API void tbevy_system_build(tbevy_system_builder_t* builder);

/* ============================================================================
 * Public API - Resources
 * ========================================================================= */

/* Register resource type and return type ID */
TBEVY_API uint64_t tbevy_register_resource_type(const char* name, size_t size,
                                                 void (*destructor)(void*));

/* Insert resource (takes ownership) */
TBEVY_API void tbevy_app_insert_resource(tbevy_app_t* app, uint64_t type_id,
                                          void* data, size_t size);

/* Get immutable resource */
TBEVY_API const void* tbevy_app_get_resource(const tbevy_app_t* app, uint64_t type_id);

/* Get mutable resource */
TBEVY_API void* tbevy_app_get_resource_mut(tbevy_app_t* app, uint64_t type_id);

/* Check if resource exists */
TBEVY_API bool tbevy_app_has_resource(const tbevy_app_t* app, uint64_t type_id);

/* Remove resource */
TBEVY_API void tbevy_app_remove_resource(tbevy_app_t* app, uint64_t type_id);

/* Helper macro for resource registration - automatically stringifies type name */
#define TBEVY_REGISTER_RESOURCE(T) \
    tbevy_register_resource_type(#T, sizeof(T), NULL)

#define TBEVY_INSERT_RESOURCE(app, type_id, value) \
    do { __typeof__(value) _tmp = value; \
         tbevy_app_insert_resource(app, type_id, &_tmp, sizeof(_tmp)); } while(0)

/* Simplified macro using Type##_id convention (auto-appends _id) */
#define TBEVY_APP_INSERT_RESOURCE(app, value, Type) \
    do { Type _tmp = value; \
         tbevy_app_insert_resource(app, Type##_id, &_tmp, sizeof(Type)); } while(0)

/* Simplified macros using Type##_id convention */
#define TBEVY_GET_RESOURCE(app, Type) \
    ((const Type*)tbevy_app_get_resource(app, Type##_id))

#define TBEVY_GET_RESOURCE_MUT(app, Type) \
    ((Type*)tbevy_app_get_resource_mut(app, Type##_id))

/* Context-based resource access macros (use Type##_id convention) */
#define TBEVY_CTX_GET_RESOURCE(ctx, Type) \
    ((const Type*)tbevy_app_get_resource((ctx)->_app, Type##_id))

#define TBEVY_CTX_GET_RESOURCE_MUT(ctx, Type) \
    ((Type*)tbevy_app_get_resource_mut((ctx)->_app, Type##_id))

/* ============================================================================
 * Public API - Commands
 * ========================================================================= */

/* Initialize commands (typically called by system framework) */
TBEVY_API void tbevy_commands_init(tbevy_commands_t* commands, tbevy_app_t* app);

/* Free commands */
TBEVY_API void tbevy_commands_free(tbevy_commands_t* commands);

/* Spawn new entity (returns entity commands for chaining) */
TBEVY_API tbevy_entity_commands_t tbevy_commands_spawn(tbevy_commands_t* commands);

/* Get entity commands for existing entity */
TBEVY_API tbevy_entity_commands_t tbevy_commands_entity(tbevy_commands_t* commands,
                                                         tecs_entity_t entity_id);

/* Entity commands operations */
/* Entity commands - chainable API for spawn pattern */
TBEVY_API tbevy_entity_commands_t* tbevy_entity_insert(tbevy_entity_commands_t* ec,
                                                        tecs_component_id_t component_id,
                                                        const void* data, size_t size);
TBEVY_API tbevy_entity_commands_t* tbevy_entity_remove(tbevy_entity_commands_t* ec,
                                                        tecs_component_id_t component_id);
TBEVY_API tbevy_entity_commands_t* tbevy_entity_despawn(tbevy_entity_commands_t* ec);
TBEVY_API tbevy_entity_commands_t* tbevy_entity_observe(tbevy_entity_commands_t* ec,
                                                         tbevy_trigger_type_t trigger_type,
                                                         tecs_component_id_t component_id,
                                                         tbevy_observer_fn_t callback,
                                                         void* user_data);

/* Entity commands - simplified API for operating on existing entities */
TBEVY_API void tbevy_commands_entity_insert(tbevy_commands_t* commands, tecs_entity_t entity_id,
                                             tecs_component_id_t component_id,
                                             const void* data, size_t size);
TBEVY_API void tbevy_commands_entity_remove(tbevy_commands_t* commands, tecs_entity_t entity_id,
                                             tecs_component_id_t component_id);
TBEVY_API void tbevy_commands_entity_despawn(tbevy_commands_t* commands, tecs_entity_t entity_id);

/* Get spawned entity ID */
TBEVY_API tecs_entity_t tbevy_entity_id(const tbevy_entity_commands_t* ec);

/* Apply all deferred commands */
TBEVY_API void tbevy_commands_apply(tbevy_commands_t* commands);

/* ============================================================================
 * Public API - Observers
 * ========================================================================= */

/* Add global observer */
TBEVY_API void tbevy_app_add_observer(tbevy_app_t* app, tbevy_trigger_type_t trigger_type,
                                       tecs_component_id_t component_id,
                                       tbevy_observer_fn_t callback, void* user_data);

/* Trigger observers manually */
TBEVY_API void tbevy_app_trigger(tbevy_app_t* app, const tbevy_trigger_t* trigger);

/* Flush pending observer triggers */
TBEVY_API void tbevy_app_flush_observers(tbevy_app_t* app);

/* ============================================================================
 * Public API - Events
 * ========================================================================= */

/* Register event type */
TBEVY_API uint64_t tbevy_register_event_type(const char* name, size_t event_size);

/* Send event */
TBEVY_API void tbevy_app_send_event(tbevy_app_t* app, uint64_t event_type_id,
                                     const void* event_data, size_t event_size);

/* Read events (callback invoked for each event) */
TBEVY_API void tbevy_app_read_events(tbevy_app_t* app, uint64_t event_type_id,
                                      tbevy_event_fn_t callback, void* user_data);

/* Clear events for this frame */
TBEVY_API void tbevy_app_clear_events(tbevy_app_t* app);

/* Helper macros - automatically stringifies type name */
#define TBEVY_REGISTER_EVENT(T) \
    tbevy_register_event_type(#T, sizeof(T))

#define TBEVY_SEND_EVENT(app, event_type_id, value) \
    do { __typeof__(value) _tmp = value; \
         tbevy_app_send_event(app, event_type_id, &_tmp, sizeof(_tmp)); } while(0)

/* ============================================================================
 * Public API - State Management
 * ========================================================================= */

/* Add state machine */
TBEVY_API void tbevy_app_add_state(tbevy_app_t* app, uint64_t state_type_id,
                                    uint32_t initial_state);

/* Get current state */
TBEVY_API uint32_t tbevy_app_get_state(const tbevy_app_t* app, uint64_t state_type_id);

/* Queue state transition */
TBEVY_API void tbevy_app_set_state(tbevy_app_t* app, uint64_t state_type_id,
                                    uint32_t new_state);

/* Add system that runs on state enter */
TBEVY_API tbevy_system_builder_t* tbevy_app_add_system_on_enter(tbevy_app_t* app,
                                                                  uint64_t state_type_id,
                                                                  uint32_t state_value,
                                                                  tbevy_system_fn_t fn,
                                                                  void* user_data);

/* Add system that runs on state exit */
TBEVY_API tbevy_system_builder_t* tbevy_app_add_system_on_exit(tbevy_app_t* app,
                                                                 uint64_t state_type_id,
                                                                 uint32_t state_value,
                                                                 tbevy_system_fn_t fn,
                                                                 void* user_data);

/* ============================================================================
 * Public API - Bundles
 * ========================================================================= */

/* Bundle function signature */
typedef void (*tbevy_bundle_insert_fn_t)(void* bundle_data, tecs_world_t* world,
                                          tecs_entity_t entity);

/* Spawn entity with bundle */
tecs_entity_t tbevy_commands_spawn_bundle(tbevy_commands_t* commands,
                                            void* bundle_data,
                                            tbevy_bundle_insert_fn_t insert_fn);

/* Insert bundle on existing entity */
void tbevy_commands_insert_bundle(tbevy_commands_t* commands,
                                   tecs_entity_t entity,
                                   void* bundle_data,
                                   tbevy_bundle_insert_fn_t insert_fn);

/* ============================================================================
 * Helper Macros
 * ========================================================================= */

/* Entity Commands Macros */
#define TBEVY_ENTITY_INSERT(ec, value, Type) \
    do { Type _tmp = value; tbevy_entity_insert(ec, Type##_id, &_tmp, sizeof(Type)); } while(0)

#define TBEVY_ENTITY_REMOVE(ec, Type) \
    tbevy_entity_remove(ec, Type##_id)

/* Observer Macros - Entity-specific */
#define TBEVY_ENTITY_OBSERVE_INSERT(ec, Type, callback, user_data) \
    tbevy_entity_observe(ec, TBEVY_TRIGGER_ON_INSERT, Type##_id, callback, user_data)

#define TBEVY_ENTITY_OBSERVE_REMOVE(ec, Type, callback, user_data) \
    tbevy_entity_observe(ec, TBEVY_TRIGGER_ON_REMOVE, Type##_id, callback, user_data)

#define TBEVY_ENTITY_OBSERVE_ADD(ec, Type, callback, user_data) \
    tbevy_entity_observe(ec, TBEVY_TRIGGER_ON_ADD, Type##_id, callback, user_data)

/* Global Observer Macros */
#define TBEVY_ADD_OBSERVER(app, trigger_type, Type, callback, user_data) \
    tbevy_app_add_observer(app, trigger_type, Type##_id, callback, user_data)

#define TBEVY_ADD_OBSERVER_INSERT(app, Type, callback, user_data) \
    tbevy_app_add_observer(app, TBEVY_TRIGGER_ON_INSERT, Type##_id, callback, user_data)

#define TBEVY_ADD_OBSERVER_REMOVE(app, Type, callback, user_data) \
    tbevy_app_add_observer(app, TBEVY_TRIGGER_ON_REMOVE, Type##_id, callback, user_data)

#define TBEVY_ADD_OBSERVER_ADD(app, Type, callback, user_data) \
    tbevy_app_add_observer(app, TBEVY_TRIGGER_ON_ADD, Type##_id, callback, user_data)

/* ============================================================================
 * Implementation
 * ========================================================================= */

#ifdef TINYECS_BEVY_IMPLEMENTATION

#include <stdlib.h>
#include <string.h>
#include <assert.h>

/* Use same allocators as tinyecs.h */
#ifndef TBEVY_MALLOC
#define TBEVY_MALLOC(size) TECS_MALLOC(size)
#endif

#ifndef TBEVY_CALLOC
#define TBEVY_CALLOC(count, size) TECS_CALLOC(count, size)
#endif

#ifndef TBEVY_REALLOC
#define TBEVY_REALLOC(ptr, size) TECS_REALLOC(ptr, size)
#endif

#ifndef TBEVY_FREE
#define TBEVY_FREE(ptr) TECS_FREE(ptr)
#endif

/* ============================================================================
 * Internal Data Structures
 * ========================================================================= */

/* System descriptor (internal) */
struct tbevy_system_s {
    tbevy_system_fn_t fn;
    void* user_data;
    char label[64];
    tbevy_stage_t* stage;
    tbevy_threading_mode_t threading_mode;

    /* Dependencies */
    tbevy_system_t** before_systems;
    tbevy_system_t** after_systems;
    size_t before_count, before_capacity;
    size_t after_count, after_capacity;

    /* Run conditions */
    tbevy_run_condition_fn_t* run_conditions;
    void** run_condition_data;
    size_t run_condition_count;
    size_t run_condition_capacity;

    /* Metadata */
    int declaration_order;
    bool visited;
    bool visiting;
};

/* System builder */
struct tbevy_system_builder_s {
    tbevy_app_t* app;
    tbevy_system_t* system;
};

/* Observer descriptor */
struct tbevy_observer_s {
    tbevy_trigger_type_t trigger_type;
    tecs_component_id_t component_id;
    tbevy_observer_fn_t callback;
    void* user_data;
    tecs_entity_t entity_id;  /* 0 for global observers */
};

/* Stage descriptor list */
typedef struct {
    tbevy_stage_t** stages;
    size_t count;
    size_t capacity;
} tbevy_stage_list_t;

/* System list */
typedef struct {
    tbevy_system_t** systems;
    size_t count;
    size_t capacity;
} tbevy_system_list_t;

/* Observer list */
typedef struct {
    tbevy_observer_t** observers;
    size_t count;
    size_t capacity;
} tbevy_observer_list_t;

/* Simple hash map entry */
typedef struct {
    uint64_t key;
    void* value;
    bool occupied;
} tbevy_hashmap_entry_t;

/* Simple hash map */
typedef struct {
    tbevy_hashmap_entry_t* entries;
    size_t size;
    size_t capacity;
} tbevy_hashmap_t;

/* Application state */
struct tbevy_app_s {
    tecs_world_t* world;
    tbevy_threading_mode_t threading_mode;

    /* Stages */
    tbevy_stage_list_t stages;
    tbevy_hashmap_t stage_systems;  /* stage_id -> system_list */
    tbevy_stage_t* default_stages[6];  /* Cached default stages */

    /* Systems */
    tbevy_system_list_t all_systems;
    tbevy_hashmap_t labeled_systems;  /* label_hash -> system */

    /* Resources */
    tbevy_hashmap_t resources;  /* type_id -> resource */

    /* Events */
    tbevy_hashmap_t event_channels;  /* type_id -> event_channel */

    /* Observers */
    tbevy_observer_list_t global_observers;
    tbevy_hashmap_t entity_observers;  /* entity_id -> observer_list */

    /* State machines */
    tbevy_hashmap_t state_machines;  /* type_id -> state_machine */
    tbevy_hashmap_t on_enter_systems;  /* (type_id << 32 | state_value) -> system_list */
    tbevy_hashmap_t on_exit_systems;

    /* Deferred operations */
    tbevy_commands_t commands;

    /* Runtime state */
    bool startup_run;
    int system_declaration_counter;
};

/* ============================================================================
 * Hash Map Implementation
 * ========================================================================= */

static uint64_t tbevy_hash_string(const char* str) {
    uint64_t hash = 5381;
    int c;
    while ((c = *str++))
        hash = ((hash << 5) + hash) + c;
    return hash;
}

static void tbevy_hashmap_init(tbevy_hashmap_t* map, size_t capacity) {
    map->capacity = capacity;
    map->size = 0;
    map->entries = TBEVY_CALLOC(capacity, sizeof(tbevy_hashmap_entry_t));
}

static void tbevy_hashmap_free(tbevy_hashmap_t* map) {
    TBEVY_FREE(map->entries);
    map->entries = NULL;
    map->size = 0;
    map->capacity = 0;
}

static void* tbevy_hashmap_get(const tbevy_hashmap_t* map, uint64_t key) {
    size_t index = key % map->capacity;
    size_t start = index;

    do {
        if (map->entries[index].occupied && map->entries[index].key == key)
            return map->entries[index].value;
        if (!map->entries[index].occupied)
            return NULL;
        index = (index + 1) % map->capacity;
    } while (index != start);

    return NULL;
}

static void tbevy_hashmap_set(tbevy_hashmap_t* map, uint64_t key, void* value) {
    /* Resize if needed */
    if (map->size >= map->capacity * 0.75) {
        size_t new_capacity = map->capacity * 2;
        tbevy_hashmap_entry_t* new_entries = TBEVY_CALLOC(new_capacity,
                                                            sizeof(tbevy_hashmap_entry_t));

        for (size_t i = 0; i < map->capacity; i++) {
            if (map->entries[i].occupied) {
                size_t index = map->entries[i].key % new_capacity;
                while (new_entries[index].occupied)
                    index = (index + 1) % new_capacity;
                new_entries[index] = map->entries[i];
            }
        }

        TBEVY_FREE(map->entries);
        map->entries = new_entries;
        map->capacity = new_capacity;
    }

    /* Insert/update */
    size_t index = key % map->capacity;
    while (map->entries[index].occupied && map->entries[index].key != key)
        index = (index + 1) % map->capacity;

    if (!map->entries[index].occupied)
        map->size++;

    map->entries[index].key = key;
    map->entries[index].value = value;
    map->entries[index].occupied = true;
}

static bool tbevy_hashmap_remove(tbevy_hashmap_t* map, uint64_t key) {
    size_t index = key % map->capacity;
    size_t start = index;

    do {
        if (map->entries[index].occupied && map->entries[index].key == key) {
            map->entries[index].occupied = false;
            map->size--;
            return true;
        }
        if (!map->entries[index].occupied)
            return false;
        index = (index + 1) % map->capacity;
    } while (index != start);

    return false;
}

/* ============================================================================
 * List Helpers
 * ========================================================================= */

static void tbevy_system_list_init(tbevy_system_list_t* list) {
    list->capacity = 16;
    list->count = 0;
    list->systems = TBEVY_MALLOC(list->capacity * sizeof(tbevy_system_t*));
}

static void tbevy_system_list_free(tbevy_system_list_t* list) {
    TBEVY_FREE(list->systems);
}

static void tbevy_system_list_add(tbevy_system_list_t* list, tbevy_system_t* system) {
    if (list->count >= list->capacity) {
        list->capacity *= 2;
        list->systems = TBEVY_REALLOC(list->systems,
                                      list->capacity * sizeof(tbevy_system_t*));
    }
    list->systems[list->count++] = system;
}

static void tbevy_observer_list_init(tbevy_observer_list_t* list) {
    list->capacity = 8;
    list->count = 0;
    list->observers = TBEVY_MALLOC(list->capacity * sizeof(tbevy_observer_t*));
}

static void tbevy_observer_list_free(tbevy_observer_list_t* list) {
    for (size_t i = 0; i < list->count; i++)
        TBEVY_FREE(list->observers[i]);
    TBEVY_FREE(list->observers);
}

static void tbevy_observer_list_add(tbevy_observer_list_t* list, tbevy_observer_t* obs) {
    if (list->count >= list->capacity) {
        list->capacity *= 2;
        list->observers = TBEVY_REALLOC(list->observers,
                                        list->capacity * sizeof(tbevy_observer_t*));
    }
    list->observers[list->count++] = obs;
}

/* ============================================================================
 * App Management
 * ========================================================================= */

/* Forward declarations */
static tbevy_stage_t* tbevy_stage_alloc(tbevy_stage_id_t id, const char* name);

/* Global app pointer for tbevy_stage_default() - set by tbevy_app_new() */
static tbevy_app_t* g_current_app = NULL;

tbevy_app_t* tbevy_app_new(tbevy_threading_mode_t threading_mode) {
    tbevy_app_t* app = TBEVY_CALLOC(1, sizeof(tbevy_app_t));

    app->world = tecs_world_new();
    app->threading_mode = threading_mode;
    app->startup_run = false;
    app->system_declaration_counter = 0;

    /* Initialize collections */
    app->stages.capacity = TBEVY_MAX_STAGES;
    app->stages.count = 0;
    app->stages.stages = TBEVY_MALLOC(app->stages.capacity * sizeof(tbevy_stage_t*));

    tbevy_hashmap_init(&app->stage_systems, 64);
    tbevy_hashmap_init(&app->labeled_systems, 128);
    tbevy_hashmap_init(&app->resources, TBEVY_MAX_RESOURCES);
    tbevy_hashmap_init(&app->event_channels, 32);
    tbevy_hashmap_init(&app->entity_observers, 64);
    tbevy_hashmap_init(&app->state_machines, 16);
    tbevy_hashmap_init(&app->on_enter_systems, 32);
    tbevy_hashmap_init(&app->on_exit_systems, 32);

    tbevy_system_list_init(&app->all_systems);
    tbevy_observer_list_init(&app->global_observers);

    tbevy_commands_init(&app->commands, app);

    /* Add and cache default stages */
    app->default_stages[TBEVY_STAGE_STARTUP] = tbevy_stage_alloc(TBEVY_STAGE_STARTUP, "Startup");
    app->default_stages[TBEVY_STAGE_FIRST] = tbevy_stage_alloc(TBEVY_STAGE_FIRST, "First");
    app->default_stages[TBEVY_STAGE_PRE_UPDATE] = tbevy_stage_alloc(TBEVY_STAGE_PRE_UPDATE, "PreUpdate");
    app->default_stages[TBEVY_STAGE_UPDATE] = tbevy_stage_alloc(TBEVY_STAGE_UPDATE, "Update");
    app->default_stages[TBEVY_STAGE_POST_UPDATE] = tbevy_stage_alloc(TBEVY_STAGE_POST_UPDATE, "PostUpdate");
    app->default_stages[TBEVY_STAGE_LAST] = tbevy_stage_alloc(TBEVY_STAGE_LAST, "Last");

    tbevy_app_add_stage(app, app->default_stages[TBEVY_STAGE_STARTUP]);
    tbevy_app_add_stage(app, app->default_stages[TBEVY_STAGE_FIRST]);
    tbevy_app_add_stage(app, app->default_stages[TBEVY_STAGE_PRE_UPDATE]);
    tbevy_app_add_stage(app, app->default_stages[TBEVY_STAGE_UPDATE]);
    tbevy_app_add_stage(app, app->default_stages[TBEVY_STAGE_POST_UPDATE]);
    tbevy_app_add_stage(app, app->default_stages[TBEVY_STAGE_LAST]);

    /* Set global current app for tbevy_stage_default() */
    g_current_app = app;

    return app;
}

void tbevy_app_free(tbevy_app_t* app) {
    if (!app) return;

    /* Clear global app pointer if this is the current app */
    if (g_current_app == app)
        g_current_app = NULL;

    /* Free systems */
    for (size_t i = 0; i < app->all_systems.count; i++) {
        tbevy_system_t* sys = app->all_systems.systems[i];
        TBEVY_FREE(sys->before_systems);
        TBEVY_FREE(sys->after_systems);
        TBEVY_FREE(sys->run_conditions);
        TBEVY_FREE(sys->run_condition_data);
        TBEVY_FREE(sys);
    }
    tbevy_system_list_free(&app->all_systems);

    /* Free stages */
    for (size_t i = 0; i < app->stages.count; i++)
        TBEVY_FREE(app->stages.stages[i]);
    TBEVY_FREE(app->stages.stages);

    /* Free resources */
    for (size_t i = 0; i < app->resources.capacity; i++) {
        if (app->resources.entries[i].occupied) {
            tbevy_resource_t* res = (tbevy_resource_t*)app->resources.entries[i].value;
            if (res->destructor)
                res->destructor(res->data);
            TBEVY_FREE(res->data);
            TBEVY_FREE(res);
        }
    }

    /* Free event channels */
    for (size_t i = 0; i < app->event_channels.capacity; i++) {
        if (app->event_channels.entries[i].occupied) {
            tbevy_event_channel_t* chan = (tbevy_event_channel_t*)app->event_channels.entries[i].value;
            TBEVY_FREE(chan->read_buffer);
            TBEVY_FREE(chan->write_buffer);
            TBEVY_FREE(chan);
        }
    }

    /* Free observers */
    tbevy_observer_list_free(&app->global_observers);

    /* Free state machines */
    for (size_t i = 0; i < app->state_machines.capacity; i++) {
        if (app->state_machines.entries[i].occupied)
            TBEVY_FREE(app->state_machines.entries[i].value);
    }

    /* Free hash maps */
    tbevy_hashmap_free(&app->stage_systems);
    tbevy_hashmap_free(&app->labeled_systems);
    tbevy_hashmap_free(&app->resources);
    tbevy_hashmap_free(&app->event_channels);
    tbevy_hashmap_free(&app->entity_observers);
    tbevy_hashmap_free(&app->state_machines);
    tbevy_hashmap_free(&app->on_enter_systems);
    tbevy_hashmap_free(&app->on_exit_systems);

    tbevy_commands_free(&app->commands);
    tecs_world_free(app->world);
    TBEVY_FREE(app);
}

tecs_world_t* tbevy_app_world(tbevy_app_t* app) {
    return app->world;
}

/* ============================================================================
 * Stages
 * ========================================================================= */

static tbevy_stage_t* tbevy_stage_alloc(tbevy_stage_id_t id, const char* name) {
    tbevy_stage_t* stage = TBEVY_MALLOC(sizeof(tbevy_stage_t));
    stage->id = id;
    strncpy(stage->name, name, 63);
    stage->name[63] = '\0';
    stage->order = 0;
    return stage;
}

tbevy_stage_t* tbevy_stage_default(tbevy_stage_id_t stage_id) {
    /* Return cached stage from current app */
    if (g_current_app && stage_id >= 0 && stage_id < 6) {
        return g_current_app->default_stages[stage_id];
    }

    /* Fallback: create new stage (shouldn't happen in normal use) */
    const char* names[] = {
        "Startup", "First", "PreUpdate", "Update", "PostUpdate", "Last"
    };
    return tbevy_stage_alloc(stage_id, names[stage_id]);
}

tbevy_stage_t* tbevy_stage_custom(const char* name) {
    return tbevy_stage_alloc(TBEVY_STAGE_CUSTOM, name);
}

tbevy_stage_t* tbevy_app_add_stage(tbevy_app_t* app, tbevy_stage_t* stage) {
    if (app->stages.count >= app->stages.capacity) {
        app->stages.capacity *= 2;
        app->stages.stages = TBEVY_REALLOC(app->stages.stages,
            app->stages.capacity * sizeof(tbevy_stage_t*));
    }

    stage->order = app->stages.count;
    app->stages.stages[app->stages.count++] = stage;

    /* Initialize system list for this stage */
    tbevy_system_list_t* sys_list = TBEVY_MALLOC(sizeof(tbevy_system_list_t));
    tbevy_system_list_init(sys_list);
    tbevy_hashmap_set(&app->stage_systems, (uintptr_t)stage, sys_list);

    return stage;
}

void tbevy_stage_after(tbevy_stage_t* stage, tbevy_stage_t* after) {
    stage->order = after->order + 1;
}

void tbevy_stage_before(tbevy_stage_t* stage, tbevy_stage_t* before) {
    stage->order = before->order - 1;
}

/* ============================================================================
 * Systems
 * ========================================================================= */

tbevy_system_builder_t* tbevy_app_add_system(tbevy_app_t* app, tbevy_system_fn_t fn,
                                               void* user_data) {
    tbevy_system_t* system = TBEVY_CALLOC(1, sizeof(tbevy_system_t));
    system->fn = fn;
    system->user_data = user_data;
    system->threading_mode = TBEVY_THREADING_AUTO;
    system->stage = NULL;
    system->label[0] = '\0';
    system->declaration_order = app->system_declaration_counter++;

    system->before_capacity = 4;
    system->before_systems = TBEVY_MALLOC(system->before_capacity * sizeof(tbevy_system_t*));
    system->before_count = 0;

    system->after_capacity = 4;
    system->after_systems = TBEVY_MALLOC(system->after_capacity * sizeof(tbevy_system_t*));
    system->after_count = 0;

    system->run_condition_capacity = 2;
    system->run_conditions = TBEVY_MALLOC(system->run_condition_capacity *
                                           sizeof(tbevy_run_condition_fn_t));
    system->run_condition_data = TBEVY_MALLOC(system->run_condition_capacity * sizeof(void*));
    system->run_condition_count = 0;

    tbevy_system_list_add(&app->all_systems, system);

    tbevy_system_builder_t* builder = TBEVY_MALLOC(sizeof(tbevy_system_builder_t));
    builder->app = app;
    builder->system = system;

    return builder;
}

tbevy_system_builder_t* tbevy_system_in_stage(tbevy_system_builder_t* builder,
                                                tbevy_stage_t* stage) {
    builder->system->stage = stage;
    return builder;
}

tbevy_system_builder_t* tbevy_system_label(tbevy_system_builder_t* builder,
                                             const char* label) {
    strncpy(builder->system->label, label, 63);
    builder->system->label[63] = '\0';

    uint64_t label_hash = tbevy_hash_string(label);
    tbevy_hashmap_set(&builder->app->labeled_systems, label_hash, builder->system);

    return builder;
}

tbevy_system_builder_t* tbevy_system_after(tbevy_system_builder_t* builder,
                                             const char* label) {
    uint64_t label_hash = tbevy_hash_string(label);
    tbevy_system_t* after_sys = (tbevy_system_t*)tbevy_hashmap_get(
        &builder->app->labeled_systems, label_hash);

    if (after_sys) {
        if (builder->system->after_count >= builder->system->after_capacity) {
            builder->system->after_capacity *= 2;
            builder->system->after_systems = TBEVY_REALLOC(
                builder->system->after_systems,
                builder->system->after_capacity * sizeof(tbevy_system_t*));
        }
        builder->system->after_systems[builder->system->after_count++] = after_sys;
    }

    return builder;
}

tbevy_system_builder_t* tbevy_system_before(tbevy_system_builder_t* builder,
                                              const char* label) {
    uint64_t label_hash = tbevy_hash_string(label);
    tbevy_system_t* before_sys = (tbevy_system_t*)tbevy_hashmap_get(
        &builder->app->labeled_systems, label_hash);

    if (before_sys) {
        if (builder->system->before_count >= builder->system->before_capacity) {
            builder->system->before_capacity *= 2;
            builder->system->before_systems = TBEVY_REALLOC(
                builder->system->before_systems,
                builder->system->before_capacity * sizeof(tbevy_system_t*));
        }
        builder->system->before_systems[builder->system->before_count++] = before_sys;
    }

    return builder;
}

tbevy_system_builder_t* tbevy_system_single_threaded(tbevy_system_builder_t* builder) {
    builder->system->threading_mode = TBEVY_THREADING_SINGLE;
    return builder;
}

tbevy_system_builder_t* tbevy_system_run_if(tbevy_system_builder_t* builder,
                                              tbevy_run_condition_fn_t condition,
                                              void* user_data) {
    tbevy_system_t* sys = builder->system;

    if (sys->run_condition_count >= sys->run_condition_capacity) {
        sys->run_condition_capacity *= 2;
        sys->run_conditions = TBEVY_REALLOC(sys->run_conditions,
            sys->run_condition_capacity * sizeof(tbevy_run_condition_fn_t));
        sys->run_condition_data = TBEVY_REALLOC(sys->run_condition_data,
            sys->run_condition_capacity * sizeof(void*));
    }

    sys->run_conditions[sys->run_condition_count] = condition;
    sys->run_condition_data[sys->run_condition_count] = user_data;
    sys->run_condition_count++;

    return builder;
}

void tbevy_system_build(tbevy_system_builder_t* builder) {
    tbevy_system_t* system = builder->system;

    /* Default to UPDATE stage if not specified */
    if (!system->stage) {
        for (size_t i = 0; i < builder->app->stages.count; i++) {
            if (builder->app->stages.stages[i]->id == TBEVY_STAGE_UPDATE) {
                system->stage = builder->app->stages.stages[i];
                break;
            }
        }
    }

    /* Add to stage's system list */
    if (system->stage) {
        tbevy_system_list_t* sys_list = (tbevy_system_list_t*)tbevy_hashmap_get(
            &builder->app->stage_systems, (uintptr_t)system->stage);
        if (sys_list)
            tbevy_system_list_add(sys_list, system);
    }

    TBEVY_FREE(builder);
}

/* Topological sort for system ordering */
static void tbevy_visit_system(tbevy_system_t* system, tbevy_system_list_t* result,
                                const tbevy_system_list_t* all_systems) {
    if (system->visited) return;
    if (system->visiting) {
        /* Circular dependency detected - ignore for now */
        return;
    }

    system->visiting = true;

    /* Visit before systems first (in declaration order) */
    for (size_t i = 0; i < system->before_count; i++) {
        tbevy_visit_system(system->before_systems[i], result, all_systems);
    }

    system->visiting = false;
    system->visited = true;
    tbevy_system_list_add(result, system);
}

static void tbevy_sort_systems(tbevy_system_list_t* systems) {
    /* Reset visited flags */
    for (size_t i = 0; i < systems->count; i++) {
        systems->systems[i]->visited = false;
        systems->systems[i]->visiting = false;
    }

    /* Create result list */
    tbevy_system_list_t sorted;
    tbevy_system_list_init(&sorted);

    /* Visit systems in declaration order */
    for (size_t i = 0; i < systems->count; i++) {
        tbevy_visit_system(systems->systems[i], &sorted, systems);
    }

    /* Replace original list */
    TBEVY_FREE(systems->systems);
    *systems = sorted;
}

/* Run systems in a stage */
static void tbevy_run_stage_systems(tbevy_app_t* app, tbevy_stage_t* stage) {
    tbevy_system_list_t* sys_list = (tbevy_system_list_t*)tbevy_hashmap_get(
        &app->stage_systems, (uintptr_t)stage);

    if (!sys_list || sys_list->count == 0) return;

    /* Sort systems by dependencies */
    tbevy_sort_systems(sys_list);

    /* Execute systems (single-threaded for now) */
    for (size_t i = 0; i < sys_list->count; i++) {
        tbevy_system_t* sys = sys_list->systems[i];

        /* Check run conditions */
        bool should_run = true;
        for (size_t j = 0; j < sys->run_condition_count; j++) {
            if (!sys->run_conditions[j](app, sys->run_condition_data[j])) {
                should_run = false;
                break;
            }
        }

        if (should_run) {
            /* Create per-system context with commands */
            tbevy_commands_t sys_commands;
            tbevy_commands_init(&sys_commands, app);

            tbevy_system_ctx_t ctx = {
                .world = app->world,
                .commands = &sys_commands,
                ._app = app
            };

            /* Execute system with context (queues commands) */
            sys->fn(&ctx, sys->user_data);

            /* Apply system's commands in deferred mode */
            tbevy_commands_apply(&sys_commands);
            tbevy_commands_free(&sys_commands);
        }
    }

    /* Flush observers */
    tbevy_app_flush_observers(app);
}

void tbevy_app_run_startup(tbevy_app_t* app) {
    if (app->startup_run) return;

    for (size_t i = 0; i < app->stages.count; i++) {
        if (app->stages.stages[i]->id == TBEVY_STAGE_STARTUP) {
            tbevy_run_stage_systems(app, app->stages.stages[i]);
            break;
        }
    }

    app->startup_run = true;
}

void tbevy_app_update(tbevy_app_t* app) {
    /* Run startup if not yet run */
    if (!app->startup_run)
        tbevy_app_run_startup(app);

    /* Process state transitions first */
    /* (Implementation omitted for brevity - would call process_state_transitions) */

    /* Run stages in order (skip Startup) */
    for (size_t i = 0; i < app->stages.count; i++) {
        tbevy_stage_t* stage = app->stages.stages[i];
        if (stage->id != TBEVY_STAGE_STARTUP)
            tbevy_run_stage_systems(app, stage);
    }

    /* Clear events */
    tbevy_app_clear_events(app);

    /* Increment world tick */
    tecs_world_update(app->world);
}

void tbevy_app_run(tbevy_app_t* app, bool (*should_quit)(tbevy_app_t*)) {
    tbevy_app_run_startup(app);

    while (!should_quit(app)) {
        tbevy_app_update(app);
    }
}

/* ============================================================================
 * Resources
 * ========================================================================= */

static uint64_t tbevy_next_resource_id = 1;

uint64_t tbevy_register_resource_type(const char* name, size_t size,
                                       void (*destructor)(void*)) {
    (void)name;  /* Unused in this implementation */
    (void)size;
    (void)destructor;
    return tbevy_next_resource_id++;
}

void tbevy_app_insert_resource(tbevy_app_t* app, uint64_t type_id,
                                void* data, size_t size) {
    tbevy_resource_t* res = TBEVY_MALLOC(sizeof(tbevy_resource_t));
    res->data = TBEVY_MALLOC(size);
    memcpy(res->data, data, size);
    res->size = size;
    res->type_id = type_id;
    res->destructor = NULL;

    /* Remove old resource if exists */
    tbevy_resource_t* old = (tbevy_resource_t*)tbevy_hashmap_get(&app->resources, type_id);
    if (old) {
        if (old->destructor)
            old->destructor(old->data);
        TBEVY_FREE(old->data);
        TBEVY_FREE(old);
    }

    tbevy_hashmap_set(&app->resources, type_id, res);
}

const void* tbevy_app_get_resource(const tbevy_app_t* app, uint64_t type_id) {
    tbevy_resource_t* res = (tbevy_resource_t*)tbevy_hashmap_get(
        (tbevy_hashmap_t*)&app->resources, type_id);
    return res ? res->data : NULL;
}

void* tbevy_app_get_resource_mut(tbevy_app_t* app, uint64_t type_id) {
    tbevy_resource_t* res = (tbevy_resource_t*)tbevy_hashmap_get(&app->resources, type_id);
    return res ? res->data : NULL;
}

bool tbevy_app_has_resource(const tbevy_app_t* app, uint64_t type_id) {
    return tbevy_hashmap_get((tbevy_hashmap_t*)&app->resources, type_id) != NULL;
}

void tbevy_app_remove_resource(tbevy_app_t* app, uint64_t type_id) {
    tbevy_resource_t* res = (tbevy_resource_t*)tbevy_hashmap_get(&app->resources, type_id);
    if (res) {
        if (res->destructor)
            res->destructor(res->data);
        TBEVY_FREE(res->data);
        TBEVY_FREE(res);
        tbevy_hashmap_remove(&app->resources, type_id);
    }
}

/* ============================================================================
 * Commands (Basic Implementation)
 * ========================================================================= */

void tbevy_commands_init(tbevy_commands_t* commands, tbevy_app_t* app) {
    commands->app = app;
    commands->command_capacity = 64;
    commands->commands = TBEVY_MALLOC(commands->command_capacity *
                                       sizeof(tbevy_deferred_command_t));
    commands->command_count = 0;
    commands->spawned_capacity = 16;
    commands->spawned_entities = TBEVY_MALLOC(commands->spawned_capacity *
                                               sizeof(tecs_entity_t));
    commands->spawned_count = 0;
}

void tbevy_commands_free(tbevy_commands_t* commands) {
    for (size_t i = 0; i < commands->command_count; i++) {
        if (commands->commands[i].data)
            TBEVY_FREE(commands->commands[i].data);
    }
    TBEVY_FREE(commands->commands);
    TBEVY_FREE(commands->spawned_entities);
}

/* Helper: Queue a command */
static void tbevy_commands_queue(tbevy_commands_t* commands, tbevy_command_type_t type,
                                  tecs_entity_t entity_id, tecs_component_id_t component_id,
                                  const void* data, size_t data_size) {
    if (commands->command_count >= commands->command_capacity) {
        commands->command_capacity *= 2;
        commands->commands = TBEVY_REALLOC(commands->commands,
            commands->command_capacity * sizeof(tbevy_deferred_command_t));
    }

    tbevy_deferred_command_t* cmd = &commands->commands[commands->command_count++];
    cmd->type = type;
    cmd->entity_id = entity_id;
    cmd->component_id = component_id;
    cmd->data_size = data_size;
    cmd->user_data = NULL;

    /* Copy component data if provided */
    if (data && data_size > 0) {
        cmd->data = TBEVY_MALLOC(data_size);
        memcpy(cmd->data, data, data_size);
    } else {
        cmd->data = NULL;
    }
}

tbevy_entity_commands_t tbevy_commands_spawn(tbevy_commands_t* commands) {
    tecs_entity_t entity = tecs_entity_new(commands->app->world);

    if (commands->spawned_count >= commands->spawned_capacity) {
        commands->spawned_capacity *= 2;
        commands->spawned_entities = TBEVY_REALLOC(commands->spawned_entities,
            commands->spawned_capacity * sizeof(tecs_entity_t));
    }
    commands->spawned_entities[commands->spawned_count++] = entity;

    tbevy_entity_commands_t ec = { commands, entity };
    return ec;
}

tbevy_entity_commands_t tbevy_commands_entity(tbevy_commands_t* commands,
                                                tecs_entity_t entity_id) {
    tbevy_entity_commands_t ec = { commands, entity_id };
    return ec;
}

tbevy_entity_commands_t* tbevy_entity_insert(tbevy_entity_commands_t* ec,
                                               tecs_component_id_t component_id,
                                               const void* data, size_t size) {
    /* Queue deferred command */
    tbevy_commands_queue(ec->commands, TBEVY_CMD_INSERT, ec->entity_id, component_id, data, size);
    return ec;
}

tbevy_entity_commands_t* tbevy_entity_remove(tbevy_entity_commands_t* ec,
                                               tecs_component_id_t component_id) {
    /* Queue deferred command */
    tbevy_commands_queue(ec->commands, TBEVY_CMD_REMOVE, ec->entity_id, component_id, NULL, 0);
    return ec;
}

tbevy_entity_commands_t* tbevy_entity_despawn(tbevy_entity_commands_t* ec) {
    /* Queue deferred command */
    tbevy_commands_queue(ec->commands, TBEVY_CMD_DESPAWN, ec->entity_id, 0, NULL, 0);
    return ec;
}

/* Simplified API implementations - create entity commands internally */
void tbevy_commands_entity_insert(tbevy_commands_t* commands, tecs_entity_t entity_id,
                                   tecs_component_id_t component_id,
                                   const void* data, size_t size) {
    tbevy_commands_queue(commands, TBEVY_CMD_INSERT, entity_id, component_id, data, size);
}

void tbevy_commands_entity_remove(tbevy_commands_t* commands, tecs_entity_t entity_id,
                                   tecs_component_id_t component_id) {
    tbevy_commands_queue(commands, TBEVY_CMD_REMOVE, entity_id, component_id, NULL, 0);
}

void tbevy_commands_entity_despawn(tbevy_commands_t* commands, tecs_entity_t entity_id) {
    tbevy_commands_queue(commands, TBEVY_CMD_DESPAWN, entity_id, 0, NULL, 0);
}

tbevy_entity_commands_t* tbevy_entity_observe(tbevy_entity_commands_t* ec,
                                                tbevy_trigger_type_t trigger_type,
                                                tecs_component_id_t component_id,
                                                tbevy_observer_fn_t callback,
                                                void* user_data) {
    tbevy_observer_t* obs = TBEVY_MALLOC(sizeof(tbevy_observer_t));
    obs->trigger_type = trigger_type;
    obs->component_id = component_id;
    obs->callback = callback;
    obs->user_data = user_data;
    obs->entity_id = ec->entity_id;

    /* Add to entity observers */
    tbevy_observer_list_t* list = (tbevy_observer_list_t*)tbevy_hashmap_get(
        &ec->commands->app->entity_observers, ec->entity_id);

    if (!list) {
        list = TBEVY_MALLOC(sizeof(tbevy_observer_list_t));
        tbevy_observer_list_init(list);
        tbevy_hashmap_set(&ec->commands->app->entity_observers, ec->entity_id, list);
    }

    tbevy_observer_list_add(list, obs);
    return ec;
}

tecs_entity_t tbevy_entity_id(const tbevy_entity_commands_t* ec) {
    return ec->entity_id;
}

void tbevy_commands_apply(tbevy_commands_t* commands) {
    if (commands->command_count == 0) {
        commands->spawned_count = 0;
        return;
    }

    tecs_world_t* world = commands->app->world;

    /* Begin deferred mode */
    tecs_begin_deferred(world);

    /* Process commands in order */
    for (size_t i = 0; i < commands->command_count; i++) {
        tbevy_deferred_command_t* cmd = &commands->commands[i];

        switch (cmd->type) {
            case TBEVY_CMD_SPAWN:
                /* Already spawned immediately for ID assignment */
                break;

            case TBEVY_CMD_INSERT:
                if (cmd->data && cmd->data_size > 0) {
                    tecs_set(world, cmd->entity_id, cmd->component_id, cmd->data, cmd->data_size);
                }
                break;

            case TBEVY_CMD_REMOVE:
                tecs_unset(world, cmd->entity_id, cmd->component_id);
                break;

            case TBEVY_CMD_DESPAWN:
                tecs_entity_delete(world, cmd->entity_id);
                break;

            case TBEVY_CMD_INSERT_RESOURCE:
                /* Resource insertion (if implemented) */
                break;

            case TBEVY_CMD_TRIGGER_EVENT:
                /* Event triggering (if implemented) */
                break;

            case TBEVY_CMD_ATTACH_OBSERVER:
                /* Observer attachment (if implemented) */
                break;
        }

        /* Free allocated data */
        if (cmd->data) {
            TBEVY_FREE(cmd->data);
            cmd->data = NULL;
        }
    }

    /* End deferred mode - applies all changes */
    tecs_end_deferred(world);

    /* Reset for next batch */
    commands->command_count = 0;
    commands->spawned_count = 0;
}

/* ============================================================================
 * Observers (Simplified Implementation)
 * ========================================================================= */

void tbevy_app_add_observer(tbevy_app_t* app, tbevy_trigger_type_t trigger_type,
                             tecs_component_id_t component_id,
                             tbevy_observer_fn_t callback, void* user_data) {
    tbevy_observer_t* obs = TBEVY_MALLOC(sizeof(tbevy_observer_t));
    obs->trigger_type = trigger_type;
    obs->component_id = component_id;
    obs->callback = callback;
    obs->user_data = user_data;
    obs->entity_id = 0;  /* Global observer */

    tbevy_observer_list_add(&app->global_observers, obs);
}

void tbevy_app_trigger(tbevy_app_t* app, const tbevy_trigger_t* trigger) {
    /* Fire global observers */
    for (size_t i = 0; i < app->global_observers.count; i++) {
        tbevy_observer_t* obs = app->global_observers.observers[i];
        if (obs->trigger_type == trigger->type &&
            (obs->component_id == 0 || obs->component_id == trigger->component_id)) {
            obs->callback(app, trigger->entity_id, trigger->component_id,
                         trigger->component_data, obs->user_data);
        }
    }

    /* Fire entity-specific observers */
    tbevy_observer_list_t* list = (tbevy_observer_list_t*)tbevy_hashmap_get(
        &app->entity_observers, trigger->entity_id);

    if (list) {
        for (size_t i = 0; i < list->count; i++) {
            tbevy_observer_t* obs = list->observers[i];
            if (obs->trigger_type == trigger->type &&
                (obs->component_id == 0 || obs->component_id == trigger->component_id)) {
                obs->callback(app, trigger->entity_id, trigger->component_id,
                             trigger->component_data, obs->user_data);
            }
        }
    }
}

void tbevy_app_flush_observers(tbevy_app_t* app) {
    /* In full implementation, would process queued observer triggers */
    (void)app;
}

/* ============================================================================
 * Events (Simplified Implementation)
 * ========================================================================= */

static uint64_t tbevy_next_event_id = 1;

uint64_t tbevy_register_event_type(const char* name, size_t event_size) {
    (void)name;
    (void)event_size;
    return tbevy_next_event_id++;
}

void tbevy_app_send_event(tbevy_app_t* app, uint64_t event_type_id,
                           const void* event_data, size_t event_size) {
    tbevy_event_channel_t* chan = (tbevy_event_channel_t*)tbevy_hashmap_get(
        &app->event_channels, event_type_id);

    if (!chan) {
        chan = TBEVY_CALLOC(1, sizeof(tbevy_event_channel_t));
        chan->element_size = event_size;
        chan->write_capacity = 16;
        chan->write_buffer = TBEVY_MALLOC(chan->write_capacity * event_size);
        chan->read_capacity = 16;
        chan->read_buffer = TBEVY_MALLOC(chan->read_capacity * event_size);
        tbevy_hashmap_set(&app->event_channels, event_type_id, chan);
    }

    /* Add to write buffer */
    if (chan->write_count >= chan->write_capacity) {
        chan->write_capacity *= 2;
        chan->write_buffer = TBEVY_REALLOC(chan->write_buffer,
                                           chan->write_capacity * event_size);
    }

    memcpy((char*)chan->write_buffer + chan->write_count * event_size,
           event_data, event_size);
    chan->write_count++;
}

void tbevy_app_read_events(tbevy_app_t* app, uint64_t event_type_id,
                            tbevy_event_fn_t callback, void* user_data) {
    tbevy_event_channel_t* chan = (tbevy_event_channel_t*)tbevy_hashmap_get(
        &app->event_channels, event_type_id);

    if (!chan) return;

    /* Read from read buffer */
    for (size_t i = 0; i < chan->read_count; i++) {
        const void* event_data = (const char*)chan->read_buffer +
                                 i * chan->element_size;
        callback(app, event_data, user_data);
    }
}

void tbevy_app_clear_events(tbevy_app_t* app) {
    /* Swap buffers and clear write buffer */
    for (size_t i = 0; i < app->event_channels.capacity; i++) {
        if (!app->event_channels.entries[i].occupied) continue;

        tbevy_event_channel_t* chan = (tbevy_event_channel_t*)app->event_channels.entries[i].value;

        /* Swap buffers */
        void* temp = chan->read_buffer;
        chan->read_buffer = chan->write_buffer;
        chan->write_buffer = temp;

        size_t temp_capacity = chan->read_capacity;
        chan->read_capacity = chan->write_capacity;
        chan->write_capacity = temp_capacity;

        chan->read_count = chan->write_count;
        chan->write_count = 0;
        chan->epoch++;
    }
}

/* ============================================================================
 * State Management (Simplified Implementation)
 * ========================================================================= */

void tbevy_app_add_state(tbevy_app_t* app, uint64_t state_type_id,
                          uint32_t initial_state) {
    tbevy_state_machine_t* sm = TBEVY_MALLOC(sizeof(tbevy_state_machine_t));
    sm->type_id = state_type_id;
    sm->current_state = initial_state;
    sm->previous_state = initial_state;
    sm->queued_state = 0;
    sm->has_queued = false;
    sm->processed_this_frame = false;

    tbevy_hashmap_set(&app->state_machines, state_type_id, sm);
}

uint32_t tbevy_app_get_state(const tbevy_app_t* app, uint64_t state_type_id) {
    tbevy_state_machine_t* sm = (tbevy_state_machine_t*)tbevy_hashmap_get(
        (tbevy_hashmap_t*)&app->state_machines, state_type_id);
    return sm ? sm->current_state : 0;
}

void tbevy_app_set_state(tbevy_app_t* app, uint64_t state_type_id,
                          uint32_t new_state) {
    tbevy_state_machine_t* sm = (tbevy_state_machine_t*)tbevy_hashmap_get(
        &app->state_machines, state_type_id);
    if (sm) {
        sm->queued_state = new_state;
        sm->has_queued = true;
    }
}

tbevy_system_builder_t* tbevy_app_add_system_on_enter(tbevy_app_t* app,
                                                        uint64_t state_type_id,
                                                        uint32_t state_value,
                                                        tbevy_system_fn_t fn,
                                                        void* user_data) {
    (void)state_type_id;
    (void)state_value;
    /* Simplified: would store in on_enter_systems hashmap */
    return tbevy_app_add_system(app, fn, user_data);
}

tbevy_system_builder_t* tbevy_app_add_system_on_exit(tbevy_app_t* app,
                                                       uint64_t state_type_id,
                                                       uint32_t state_value,
                                                       tbevy_system_fn_t fn,
                                                       void* user_data) {
    (void)state_type_id;
    (void)state_value;
    /* Simplified: would store in on_exit_systems hashmap */
    return tbevy_app_add_system(app, fn, user_data);
}

/* ============================================================================
 * Bundles
 * ========================================================================= */

tecs_entity_t tbevy_commands_spawn_bundle(tbevy_commands_t* commands,
                                            void* bundle_data,
                                            tbevy_bundle_insert_fn_t insert_fn) {
    tecs_entity_t entity = tecs_entity_new(commands->app->world);
    insert_fn(bundle_data, commands->app->world, entity);
    return entity;
}

void tbevy_commands_insert_bundle(tbevy_commands_t* commands,
                                   tecs_entity_t entity,
                                   void* bundle_data,
                                   tbevy_bundle_insert_fn_t insert_fn) {
    insert_fn(bundle_data, commands->app->world, entity);
}

#endif /* TINYECS_BEVY_IMPLEMENTATION */

#ifdef __cplusplus
}
#endif

#endif /* TINYECS_BEVY_H */
