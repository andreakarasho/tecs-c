/*
 * TinyEcs.h - Single-header Entity Component System for C
 *
 * A high-performance, cache-friendly ECS implementation with:
 * - Archetype-based storage for optimal memory layout
 * - Entity recycling with generation counters
 * - Zero-allocation query iteration
 * - Chunk-based memory management (4096 entities per chunk)
 * - Change detection with tick tracking
 * - Deferred command buffers for thread-safe operations
 *
 * Usage:
 *   Header-only (default):
 *     #define TINYECS_IMPLEMENTATION
 *     #include "tinyecs.h"
 *
 *   Shared library export:
 *     #define TINYECS_SHARED_LIBRARY
 *     #define TINYECS_IMPLEMENTATION
 *     #include "tinyecs.h"
 *
 *   Shared library import:
 *     #define TINYECS_SHARED_LIBRARY
 *     #include "tinyecs.h"
 *
 * License: MIT
 * Based on TinyEcs C# implementation
 */

#ifndef TINYECS_H
#define TINYECS_H

#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

/* ============================================================================
 * DLL Export/Import Configuration
 * ========================================================================= */

#ifndef TECS_API
    #ifdef TINYECS_SHARED_LIBRARY
        #if defined(_WIN32) || defined(__CYGWIN__)
            #ifdef TINYECS_IMPLEMENTATION
                #ifdef __GNUC__
                    #define TECS_API __attribute__((dllexport))
                #else
                    #define TECS_API __declspec(dllexport)
                #endif
            #else
                #ifdef __GNUC__
                    #define TECS_API __attribute__((dllimport))
                #else
                    #define TECS_API __declspec(dllimport)
                #endif
            #endif
        #else
            #if __GNUC__ >= 4
                #define TECS_API __attribute__((visibility("default")))
            #else
                #define TECS_API
            #endif
        #endif
    #else
        #define TECS_API
    #endif
#endif

/* ============================================================================
 * Configuration
 * ========================================================================= */

#ifndef TECS_CHUNK_SIZE
#define TECS_CHUNK_SIZE 4096  /* Entities per chunk (must be power of 2) */
#endif

#ifndef TECS_MAX_COMPONENTS
#define TECS_MAX_COMPONENTS 1024  /* Maximum unique component types */
#endif

#ifndef TECS_MAX_QUERY_TERMS
#define TECS_MAX_QUERY_TERMS 16  /* Maximum components per query */
#endif

#ifndef TECS_INITIAL_ARCHETYPES
#define TECS_INITIAL_ARCHETYPES 32  /* Initial archetype table size */
#endif

#ifndef TECS_INITIAL_CHUNKS
#define TECS_INITIAL_CHUNKS 4  /* Initial chunks per archetype */
#endif

/* ============================================================================
 * Type Definitions
 * ========================================================================= */

/* Entity ID: 64-bit value with embedded generation counter
 * Bits 0-31:   Entity index (32 bits)
 * Bits 32-47:  Generation counter (16 bits)
 * Bits 48-63:  Unused/flags
 */
typedef uint64_t tecs_entity_t;

/* Component ID: 64-bit unique identifier per component type */
typedef uint64_t tecs_component_id_t;

/* Tick counter for change detection */
typedef uint32_t tecs_tick_t;

/* Forward declarations */
typedef struct tecs_world_s tecs_world_t;
typedef struct tecs_query_s tecs_query_t;
typedef struct tecs_query_iter_s tecs_query_iter_t;
typedef struct tecs_archetype_s tecs_archetype_t;
typedef struct tecs_storage_provider_s tecs_storage_provider_t;

/* ============================================================================
 * Pluggable Storage Provider Interface
 * ========================================================================= */

/* Storage provider operations - allows custom storage backends (e.g., managed C# arrays) */
struct tecs_storage_provider_s {
    /* Allocate storage for a chunk (TECS_CHUNK_SIZE entities) */
    void* (*alloc_chunk)(void* user_data, int component_size, int chunk_capacity);
    
    /* Free chunk storage */
    void (*free_chunk)(void* user_data, void* chunk_data);
    
    /* Get pointer to component at index */
    void* (*get_ptr)(void* user_data, void* chunk_data, int index, int component_size);
    
    /* Set component data at index */
    void (*set_data)(void* user_data, void* chunk_data, int index, 
                     const void* data, int component_size);
    
    /* Copy component from src[src_idx] to dst[dst_idx] */
    void (*copy_data)(void* user_data, 
                      void* src_chunk, int src_idx,
                      void* dst_chunk, int dst_idx,
                      int component_size);
    
    /* Swap components at two indices (for entity removal optimization) */
    void (*swap_data)(void* user_data, void* chunk_data, 
                      int idx_a, int idx_b, int component_size);
    
    /* User-provided context data */
    void* user_data;
    
    /* Storage provider name (for debugging) */
    const char* name;
};

/* Entity ID manipulation macros */
#define TECS_ENTITY_INDEX(e)      ((uint32_t)((e) & 0xFFFFFFFFULL))
#define TECS_ENTITY_GENERATION(e) ((uint16_t)(((e) >> 32) & 0xFFFFULL))
#define TECS_ENTITY_MAKE(idx, gen) ((uint64_t)(idx) | ((uint64_t)(gen) << 32))
#define TECS_ENTITY_NULL 0

/* Query term types */
typedef enum {
    TECS_TERM_WITH,      /* Component must be present */
    TECS_TERM_WITHOUT,   /* Component must not be present */
    TECS_TERM_OPTIONAL,  /* Component may or may not be present */
    TECS_TERM_CHANGED,   /* Component must be present and changed */
    TECS_TERM_ADDED      /* Component must be present and just added */
} tecs_term_type_t;

/* Component info stored in archetype */
typedef struct {
    tecs_component_id_t id;
    int size;               /* Size in bytes (0 for tags) */
    int column_index;       /* Index in chunk columns array */
} tecs_component_info_t;

/* Query term for filtering */
typedef struct {
    tecs_term_type_t type;
    tecs_component_id_t component_id;
    int data_index;  /* Index in data components array (-1 if not a data component) */
} tecs_query_term_t;

/* ============================================================================
 * Public API
 * ========================================================================= */

/* World Management */
TECS_API tecs_world_t* tecs_world_new(void);
TECS_API void tecs_world_free(tecs_world_t* world);
TECS_API void tecs_world_update(tecs_world_t* world);
TECS_API tecs_tick_t tecs_world_tick(const tecs_world_t* world);
TECS_API int tecs_world_entity_count(const tecs_world_t* world);
TECS_API void tecs_world_clear(tecs_world_t* world);

/* Component Registration */
TECS_API tecs_component_id_t tecs_register_component(tecs_world_t* world, const char* name, int size);
TECS_API tecs_component_id_t tecs_register_component_ex(tecs_world_t* world, const char* name, int size, 
                                                         tecs_storage_provider_t* storage_provider);
TECS_API tecs_component_id_t tecs_get_component_id(const tecs_world_t* world, const char* name);
TECS_API tecs_storage_provider_t* tecs_get_default_storage_provider(void);

/* Entity Operations */
TECS_API tecs_entity_t tecs_entity_new(tecs_world_t* world);
TECS_API tecs_entity_t tecs_entity_new_with_id(tecs_world_t* world, tecs_entity_t id);
TECS_API void tecs_entity_delete(tecs_world_t* world, tecs_entity_t entity);
TECS_API bool tecs_entity_exists(const tecs_world_t* world, tecs_entity_t entity);

/* Component Operations */
TECS_API void tecs_set(tecs_world_t* world, tecs_entity_t entity, tecs_component_id_t component_id,
                       const void* data, int size);
TECS_API void* tecs_get(tecs_world_t* world, tecs_entity_t entity, tecs_component_id_t component_id);
TECS_API const void* tecs_get_const(const tecs_world_t* world, tecs_entity_t entity, tecs_component_id_t component_id);
TECS_API bool tecs_has(const tecs_world_t* world, tecs_entity_t entity, tecs_component_id_t component_id);
TECS_API void tecs_unset(tecs_world_t* world, tecs_entity_t entity, tecs_component_id_t component_id);
TECS_API void tecs_add_tag(tecs_world_t* world, tecs_entity_t entity, tecs_component_id_t tag_id);
TECS_API void tecs_mark_changed(tecs_world_t* world, tecs_entity_t entity, tecs_component_id_t component_id);

/* Hierarchy Components */
typedef struct {
    tecs_entity_t parent;
} tecs_parent_t;

typedef struct {
    tecs_entity_t* entities;
    int count;
    int capacity;
} tecs_children_t;

/* Hierarchy Component IDs - Stored per-world, accessed via functions */
TECS_API tecs_component_id_t tecs_get_parent_component_id(const tecs_world_t* world);
TECS_API tecs_component_id_t tecs_get_children_component_id(const tecs_world_t* world);

/* Hierarchy Operations */
TECS_API void tecs_add_child(tecs_world_t* world, tecs_entity_t parent, tecs_entity_t child);
TECS_API void tecs_remove_child(tecs_world_t* world, tecs_entity_t parent, tecs_entity_t child);
TECS_API void tecs_remove_all_children(tecs_world_t* world, tecs_entity_t parent);
TECS_API tecs_entity_t tecs_get_parent(const tecs_world_t* world, tecs_entity_t child);
TECS_API bool tecs_has_parent(const tecs_world_t* world, tecs_entity_t child);
TECS_API const tecs_children_t* tecs_get_children(const tecs_world_t* world, tecs_entity_t parent);
TECS_API int tecs_child_count(const tecs_world_t* world, tecs_entity_t parent);
TECS_API bool tecs_is_ancestor_of(const tecs_world_t* world, tecs_entity_t ancestor, tecs_entity_t descendant);
TECS_API bool tecs_is_descendant_of(const tecs_world_t* world, tecs_entity_t descendant, tecs_entity_t ancestor);
TECS_API int tecs_get_hierarchy_depth(const tecs_world_t* world, tecs_entity_t entity);

/* Hierarchy Traversal */
typedef void (*tecs_hierarchy_visitor_t)(tecs_world_t* world, tecs_entity_t entity, void* user_data);
TECS_API void tecs_traverse_children(tecs_world_t* world, tecs_entity_t parent,
                                      tecs_hierarchy_visitor_t visitor, void* user_data, bool recursive);
TECS_API void tecs_traverse_ancestors(tecs_world_t* world, tecs_entity_t child,
                                       tecs_hierarchy_visitor_t visitor, void* user_data);

/* Query Operations */
TECS_API tecs_query_t* tecs_query_new(tecs_world_t* world);
TECS_API void tecs_query_free(tecs_query_t* query);
TECS_API void tecs_query_with(tecs_query_t* query, tecs_component_id_t component_id);
TECS_API void tecs_query_without(tecs_query_t* query, tecs_component_id_t component_id);
TECS_API void tecs_query_optional(tecs_query_t* query, tecs_component_id_t component_id);
TECS_API void tecs_query_changed(tecs_query_t* query, tecs_component_id_t component_id);
TECS_API void tecs_query_added(tecs_query_t* query, tecs_component_id_t component_id);
TECS_API void tecs_query_build(tecs_query_t* query);

/* Query Iteration */
TECS_API tecs_query_iter_t* tecs_query_iter(tecs_query_t* query);
TECS_API tecs_query_iter_t* tecs_query_iter_cached(tecs_query_t* query);
TECS_API void tecs_query_iter_init(tecs_query_iter_t* iter, tecs_query_t* query);
TECS_API bool tecs_iter_next(tecs_query_iter_t* iter);
TECS_API void tecs_query_iter_free(tecs_query_iter_t* iter);
TECS_API int tecs_iter_count(const tecs_query_iter_t* iter);
TECS_API tecs_entity_t* tecs_iter_entities(const tecs_query_iter_t* iter);
TECS_API void* tecs_iter_column(const tecs_query_iter_t* iter, int index);
TECS_API void* tecs_iter_chunk_data(const tecs_query_iter_t* iter, int column_index);  /* Get chunk storage data for pluggable storage */
TECS_API tecs_storage_provider_t* tecs_iter_storage_provider(const tecs_query_iter_t* iter, int index);
TECS_API tecs_tick_t* tecs_iter_changed_ticks(const tecs_query_iter_t* iter, int index);
TECS_API tecs_tick_t* tecs_iter_added_ticks(const tecs_query_iter_t* iter, int index);

/* Deferred Operations (Thread-safe command buffers) */
TECS_API void tecs_begin_deferred(tecs_world_t* world);
TECS_API void tecs_end_deferred(tecs_world_t* world);

/* Memory Management */
TECS_API int tecs_remove_empty_archetypes(tecs_world_t* world);

/* Helper Macros */
#define TECS_REGISTER_COMPONENT(world, T) \
    tecs_register_component(world, #T, sizeof(T))

#define TECS_SET(world, entity, T, value) \
    do { T _tmp = value; tecs_set(world, entity, T##_id, &_tmp, sizeof(T)); } while(0)

#define TECS_GET(world, entity, T) \
    ((T*)tecs_get(world, entity, T##_id))

#define TECS_HAS(world, entity, T) \
    tecs_has(world, entity, T##_id)

#define TECS_UNSET(world, entity, T) \
    tecs_unset(world, entity, T##_id)

/* Component Declaration & Registration Macros */
#define TECS_DECLARE_COMPONENT(Name) \
    typedef struct Name Name; \
    static tecs_component_id_t Name##_id = 0

#define TECS_COMPONENT_REGISTER(world, Name) \
    (Name##_id = tecs_register_component(world, #Name, sizeof(Name)))

/* Query Building Macros */
#define TECS_QUERY_WITH(query, Type) \
    tecs_query_with(query, Type##_id)

#define TECS_QUERY_WITHOUT(query, Type) \
    tecs_query_without(query, Type##_id)

#define TECS_QUERY_OPTIONAL(query, Type) \
    tecs_query_optional(query, Type##_id)

#define TECS_QUERY_CHANGED(query, Type) \
    tecs_query_changed(query, Type##_id)

#define TECS_QUERY_ADDED(query, Type) \
    tecs_query_added(query, Type##_id)

/* Tag Component Macros */
#define TECS_ADD_TAG(world, entity, Type) \
    tecs_add_tag(world, entity, Type##_id)

/* Change Detection Macros */
#define TECS_MARK_CHANGED(world, entity, Type) \
    tecs_mark_changed(world, entity, Type##_id)

/* Hierarchy Macros */
#define TECS_ADD_CHILD(world, parent, child) \
    tecs_add_child(world, parent, child)

#define TECS_REMOVE_CHILD(world, parent, child) \
    tecs_remove_child(world, parent, child)

#define TECS_REMOVE_ALL_CHILDREN(world, parent) \
    tecs_remove_all_children(world, parent)

#define TECS_GET_PARENT(world, child) \
    tecs_get_parent(world, child)

#define TECS_HAS_PARENT(world, child) \
    tecs_has_parent(world, child)

#define TECS_GET_CHILDREN(world, parent) \
    tecs_get_children(world, parent)

#define TECS_CHILD_COUNT(world, parent) \
    tecs_child_count(world, parent)

/* ============================================================================
 * Implementation
 * ========================================================================= */

#ifdef TINYECS_IMPLEMENTATION

#include <stdlib.h>
#include <string.h>
#include <assert.h>

/* Memory allocation wrappers (can be overridden) */
#ifndef TECS_MALLOC
#define TECS_MALLOC(size) malloc(size)
#endif

#ifndef TECS_CALLOC
#define TECS_CALLOC(count, size) calloc(count, size)
#endif

#ifndef TECS_REALLOC
#define TECS_REALLOC(ptr, size) realloc(ptr, size)
#endif

#ifndef TECS_FREE
#define TECS_FREE(ptr) free(ptr)
#endif

/* ============================================================================
 * Default Native Storage Provider
 * ========================================================================= */

/* Native storage wrapper */
typedef struct {
    void* data;  /* Raw memory block */
} tecs_native_storage_t;

static void* tecs_native_alloc_chunk(void* user_data, int component_size, int capacity) {
    (void)user_data;
    tecs_native_storage_t* storage = TECS_MALLOC(sizeof(tecs_native_storage_t));
    storage->data = TECS_MALLOC(component_size * capacity);
    return storage;
}

static void tecs_native_free_chunk(void* user_data, void* chunk_data) {
    (void)user_data;
    if (!chunk_data) return;
    tecs_native_storage_t* storage = (tecs_native_storage_t*)chunk_data;
    TECS_FREE(storage->data);
    TECS_FREE(storage);
}

static void* tecs_native_get_ptr(void* user_data, void* chunk_data, int index, int size) {
    (void)user_data;
    tecs_native_storage_t* storage = (tecs_native_storage_t*)chunk_data;
    return (char*)storage->data + (index * size);
}

static void tecs_native_set_data(void* user_data, void* chunk_data, int index,
                                 const void* data, int size) {
    void* ptr = tecs_native_get_ptr(user_data, chunk_data, index, size);
    memcpy(ptr, data, size);
}

static void tecs_native_copy_data(void* user_data,
                                   void* src_chunk, int src_idx,
                                   void* dst_chunk, int dst_idx,
                                   int size) {
    void* src_ptr = tecs_native_get_ptr(user_data, src_chunk, src_idx, size);
    void* dst_ptr = tecs_native_get_ptr(user_data, dst_chunk, dst_idx, size);
    memcpy(dst_ptr, src_ptr, size);
}

static void tecs_native_swap_data(void* user_data, void* chunk_data,
                                   int idx_a, int idx_b, int size) {
    if (idx_a == idx_b) return;
    
    void* ptr_a = tecs_native_get_ptr(user_data, chunk_data, idx_a, size);
    void* ptr_b = tecs_native_get_ptr(user_data, chunk_data, idx_b, size);
    
    /* Swap using temporary buffer on stack (assuming reasonable component sizes) */
    char temp[256];
    char* heap_temp = NULL;
    void* swap_temp = temp;
    
    if (size > 256) {
        heap_temp = TECS_MALLOC(size);
        swap_temp = heap_temp;
    }
    
    memcpy(swap_temp, ptr_a, size);
    memcpy(ptr_a, ptr_b, size);
    memcpy(ptr_b, swap_temp, size);
    
    if (heap_temp) {
        TECS_FREE(heap_temp);
    }
}

static tecs_storage_provider_t tecs_default_storage = {
    .alloc_chunk = tecs_native_alloc_chunk,
    .free_chunk = tecs_native_free_chunk,
    .get_ptr = tecs_native_get_ptr,
    .set_data = tecs_native_set_data,
    .copy_data = tecs_native_copy_data,
    .swap_data = tecs_native_swap_data,
    .user_data = NULL,
    .name = "native"
};

tecs_storage_provider_t* tecs_get_default_storage_provider(void) {
    return &tecs_default_storage;
}

/* ============================================================================
 * Internal Data Structures
 * ========================================================================= */

/* Component column data within a chunk */
typedef struct {
    void* storage_data;             /* Storage-specific data (opaque pointer) */
    tecs_storage_provider_t* provider; /* Storage provider for this column */
    bool is_native_storage;         /* Fast path optimization flag */
    tecs_tick_t* changed_ticks;     /* Per-entity change ticks */
    tecs_tick_t* added_ticks;       /* Per-entity added ticks */
} tecs_column_t;

/* Archetype chunk: stores up to TECS_CHUNK_SIZE entities */
typedef struct {
    tecs_entity_t entities[TECS_CHUNK_SIZE];  /* Entity IDs */
    tecs_column_t* columns;                    /* One column per component */
    int count;                                 /* Active entity count */
    int capacity;                              /* Always TECS_CHUNK_SIZE */
} tecs_chunk_t;

/* Archetype graph edge for fast component add/remove transitions */
typedef struct {
    tecs_component_id_t component_id;
    tecs_archetype_t* target;
} tecs_archetype_edge_t;

/* Simple hash map entry for component lookups */
typedef struct {
    tecs_component_id_t key;
    int value;
    bool occupied;
} tecs_component_map_entry_t;

/* Simple hash map for O(1) component index lookups */
typedef struct {
    tecs_component_map_entry_t* entries;
    int capacity;
} tecs_component_map_t;

/* Simple hash map for O(1) edge lookups */
typedef struct {
    tecs_component_id_t key;
    tecs_archetype_t* value;
    bool occupied;
} tecs_edge_map_entry_t;

typedef struct {
    tecs_edge_map_entry_t* entries;
    int capacity;
} tecs_edge_map_t;

/* Archetype: collection of entities with identical component sets */
struct tecs_archetype_s {
    uint64_t id;                              /* Hash of component set */
    tecs_component_info_t* components;        /* All components (data + tags) */
    int component_count;
    tecs_component_info_t* data_components;   /* Only data components (size > 0) */
    int data_component_count;
    tecs_component_info_t* tags;              /* Only tags (size == 0) */
    int tag_count;

    tecs_chunk_t** chunks;                    /* Dynamic array of chunks */
    int chunk_count;
    int chunk_capacity;
    int entity_count;                         /* Total entities across all chunks */

    tecs_archetype_edge_t* add_edges;         /* Edges for adding components */
    int add_edge_count;
    int add_edge_capacity;

    tecs_archetype_edge_t* remove_edges;      /* Edges for removing components */
    int remove_edge_count;
    int remove_edge_capacity;

    /* Hash maps for O(1) lookups */
    tecs_component_map_t component_map;       /* component_id -> index in components array */
    tecs_component_map_t data_component_map;  /* component_id -> column index (data components only) */
    tecs_edge_map_t add_edge_map;             /* component_id -> target archetype */
    tecs_edge_map_t remove_edge_map;          /* component_id -> target archetype */
};

/* Entity record: maps entity ID to archetype location */
typedef struct {
    tecs_archetype_t* archetype;
    int chunk_index;
    int row;  /* Global row index across all chunks */
} tecs_entity_record_t;

/* Sparse set for entity storage with O(1) lookup */
typedef struct {
    uint32_t* sparse;        /* Sparse array (chunked) */
    size_t sparse_capacity;
    tecs_entity_record_t* dense;  /* Dense array of records */
    int dense_count;
    int dense_capacity;
    uint32_t* recycled;      /* Stack of recycled entity indices */
    int recycled_count;
    int recycled_capacity;
    uint16_t* generations;   /* Generation counter per entity index */
    size_t generation_capacity;
} tecs_entity_sparse_set_t;

/* Deferred command types */
typedef enum {
    TECS_CMD_SET_COMPONENT,
    TECS_CMD_UNSET_COMPONENT,
    TECS_CMD_DELETE_ENTITY
} tecs_command_type_t;

/* Deferred command buffer entry */
typedef struct {
    tecs_command_type_t type;
    tecs_entity_t entity;
    tecs_component_id_t component_id;
    void* data;
    int size;
} tecs_command_t;

/* Component registry entry */
typedef struct {
    tecs_component_id_t id;
    char name[64];
    int size;
    tecs_storage_provider_t* storage_provider;  /* NULL = use default native storage */
} tecs_component_registry_entry_t;

/* Archetype hash table entry */
typedef struct {
    uint64_t hash;
    tecs_archetype_t* archetype;
} tecs_archetype_table_entry_t;

/* World: main ECS container */
struct tecs_world_s {
    tecs_entity_sparse_set_t entities;

    tecs_archetype_t* root_archetype;  /* Empty archetype */
    tecs_archetype_table_entry_t* archetype_table;
    int archetype_table_size;
    int archetype_table_capacity;

    tecs_component_registry_entry_t* component_registry;
    int component_count;
    int component_capacity;
    tecs_component_map_t component_registry_map;  /* component_id -> registry index for O(1) lookup */

    tecs_tick_t tick;
    uint64_t structural_change_version;

    /* Deferred command buffer */
    tecs_command_t* command_buffer;
    int command_count;
    int command_capacity;
    bool in_deferred;

    /* Hierarchy: entity children storage (maps entity_id -> tecs_children_t*) */
    struct {
        tecs_entity_t* keys;
        tecs_children_t** values;
        int count;
        int capacity;
    } entity_children;

    /* Hierarchy component IDs */
    tecs_component_id_t parent_component_id;
    tecs_component_id_t children_component_id;
};

/* Query iterator (defined before query for embedding) */
struct tecs_query_iter_s {
    tecs_query_t* query;
    int archetype_index;
    int chunk_index;
    tecs_chunk_t* current_chunk;
    tecs_archetype_t* current_archetype;
};

/* Query structure */
struct tecs_query_s {
    tecs_world_t* world;
    tecs_query_term_t terms[TECS_MAX_QUERY_TERMS];
    int term_count;

    tecs_archetype_t** matched_archetypes;
    int matched_count;
    int matched_capacity;

    uint64_t last_structural_version;
    bool built;

    /* Cached iterator for zero-allocation iteration */
    tecs_query_iter_t cached_iter;
};

/* ============================================================================
 * Hashing and Utilities
 * ========================================================================= */

/* FNV-1a hash for component ID arrays (unordered set hash) */
static uint64_t tecs_hash_component_set(const tecs_component_id_t* ids, int count) {
    uint64_t hash = 14695981039346656037ULL;

    /* Sort IDs first for consistent hashing */
    tecs_component_id_t* sorted = TECS_MALLOC(count * sizeof(tecs_component_id_t));
    memcpy(sorted, ids, count * sizeof(tecs_component_id_t));

    /* Simple insertion sort (typically small arrays) */
    for (int i = 1; i < count; i++) {
        tecs_component_id_t key = sorted[i];
        int j = i - 1;
        while (j >= 0 && sorted[j] > key) {
            sorted[j + 1] = sorted[j];
            j--;
        }
        sorted[j + 1] = key;
    }

    /* Hash sorted IDs */
    for (int i = 0; i < count; i++) {
        hash ^= sorted[i];
        hash *= 1099511628211ULL;
    }

    TECS_FREE(sorted);
    return hash;
}

/* Compare component info for sorting */
static int tecs_compare_component_info(const void* a, const void* b) {
    const tecs_component_info_t* ca = (const tecs_component_info_t*)a;
    const tecs_component_info_t* cb = (const tecs_component_info_t*)b;
    if (ca->id < cb->id) return -1;
    if (ca->id > cb->id) return 1;
    return 0;
}

/* ============================================================================
 * Entity Sparse Set
 * ========================================================================= */

static void tecs_sparse_set_init(tecs_entity_sparse_set_t* set) {
    set->sparse = TECS_CALLOC(1024, sizeof(uint32_t));
    set->sparse_capacity = 1024;
    set->dense = TECS_MALLOC(64 * sizeof(tecs_entity_record_t));
    set->dense_count = 0;
    set->dense_capacity = 64;
    set->recycled = TECS_MALLOC(64 * sizeof(uint32_t));
    set->recycled_count = 0;
    set->recycled_capacity = 64;
    set->generations = TECS_CALLOC(1024, sizeof(uint16_t));
    set->generation_capacity = 1024;
}

static void tecs_sparse_set_free(tecs_entity_sparse_set_t* set) {
    TECS_FREE(set->sparse);
    TECS_FREE(set->dense);
    TECS_FREE(set->recycled);
    TECS_FREE(set->generations);
}

static void tecs_sparse_set_ensure_capacity(tecs_entity_sparse_set_t* set, uint32_t index) {
    if (index >= set->sparse_capacity) {
        size_t new_capacity = set->sparse_capacity * 2;
        while (index >= new_capacity) new_capacity *= 2;
        set->sparse = TECS_REALLOC(set->sparse, new_capacity * sizeof(uint32_t));
        memset(set->sparse + set->sparse_capacity, 0,
               (new_capacity - set->sparse_capacity) * sizeof(uint32_t));
        set->sparse_capacity = new_capacity;
    }

    if (index >= set->generation_capacity) {
        size_t new_capacity = set->generation_capacity * 2;
        while (index >= new_capacity) new_capacity *= 2;
        set->generations = TECS_REALLOC(set->generations, new_capacity * sizeof(uint16_t));
        memset(set->generations + set->generation_capacity, 0,
               (new_capacity - set->generation_capacity) * sizeof(uint16_t));
        set->generation_capacity = new_capacity;
    }
}

static tecs_entity_t tecs_sparse_set_create(tecs_entity_sparse_set_t* set) {
    uint32_t index;
    uint16_t generation;

    /* Try to reuse recycled entity */
    if (set->recycled_count > 0) {
        index = set->recycled[--set->recycled_count];
        generation = ++set->generations[index];
    } else {
        index = set->dense_count;
        generation = 0;
    }

    tecs_sparse_set_ensure_capacity(set, index);

    /* Expand dense array if needed */
    if (set->dense_count >= set->dense_capacity) {
        set->dense_capacity *= 2;
        set->dense = TECS_REALLOC(set->dense, set->dense_capacity * sizeof(tecs_entity_record_t));
    }

    set->sparse[index] = set->dense_count;
    set->dense[set->dense_count].archetype = NULL;
    set->dense[set->dense_count].chunk_index = -1;
    set->dense[set->dense_count].row = -1;
    set->dense_count++;

    return TECS_ENTITY_MAKE(index, generation);
}

static tecs_entity_record_t* tecs_sparse_set_get(const tecs_entity_sparse_set_t* set,
                                                   tecs_entity_t entity) {
    uint32_t index = TECS_ENTITY_INDEX(entity);
    uint16_t generation = TECS_ENTITY_GENERATION(entity);

    if (index >= set->sparse_capacity) return NULL;
    if (set->generations[index] != generation) return NULL;

    uint32_t dense_index = set->sparse[index];
    if (dense_index >= (uint32_t)set->dense_count) return NULL;

    return &set->dense[dense_index];
}

static void tecs_sparse_set_remove(tecs_entity_sparse_set_t* set, tecs_entity_t entity) {
    uint32_t index = TECS_ENTITY_INDEX(entity);
    if (index >= set->sparse_capacity) return;

    uint32_t dense_index = set->sparse[index];
    if (dense_index >= (uint32_t)set->dense_count) return;

    /* Swap with last element in dense array */
    if (dense_index < (uint32_t)(set->dense_count - 1)) {
        set->dense[dense_index] = set->dense[set->dense_count - 1];
        /* Update sparse index for swapped entity */
        /* Note: We'd need to track entity IDs in dense array to do this properly */
    }
    set->dense_count--;

    /* Add to recycle list */
    if (set->recycled_count >= set->recycled_capacity) {
        set->recycled_capacity *= 2;
        set->recycled = TECS_REALLOC(set->recycled, set->recycled_capacity * sizeof(uint32_t));
    }
    set->recycled[set->recycled_count++] = index;
}

/* ============================================================================
 * Component Hash Map Implementation
 * ========================================================================= */

static void tecs_component_map_init(tecs_component_map_t* map, int capacity) {
    map->capacity = capacity;
    map->entries = TECS_CALLOC(capacity, sizeof(tecs_component_map_entry_t));
}

static void tecs_component_map_free(tecs_component_map_t* map) {
    TECS_FREE(map->entries);
    map->entries = NULL;
    map->capacity = 0;
}

static int tecs_component_map_get(const tecs_component_map_t* map,
                                   tecs_component_id_t component_id) {
    if (map->capacity == 0) return -1;

    size_t index = component_id % map->capacity;
    size_t start = index;

    do {
        if (!map->entries[index].occupied)
            return -1;
        if (map->entries[index].key == component_id)
            return map->entries[index].value;
        index = (index + 1) % map->capacity;
    } while (index != start);

    return -1;
}

static void tecs_component_map_set(tecs_component_map_t* map,
                                    tecs_component_id_t component_id, int value) {
    if (map->capacity == 0) return;

    size_t index = component_id % map->capacity;

    while (map->entries[index].occupied && map->entries[index].key != component_id)
        index = (index + 1) % map->capacity;

    map->entries[index].key = component_id;
    map->entries[index].value = value;
    map->entries[index].occupied = true;
}

/* ============================================================================
 * Edge Hash Map Implementation
 * ========================================================================= */

static void tecs_edge_map_init(tecs_edge_map_t* map, int capacity) {
    map->capacity = capacity;
    map->entries = TECS_CALLOC(capacity, sizeof(tecs_edge_map_entry_t));
}

static void tecs_edge_map_free(tecs_edge_map_t* map) {
    TECS_FREE(map->entries);
    map->entries = NULL;
    map->capacity = 0;
}

static tecs_archetype_t* tecs_edge_map_get(const tecs_edge_map_t* map,
                                            tecs_component_id_t component_id) {
    if (map->capacity == 0) return NULL;

    size_t index = component_id % map->capacity;
    size_t start = index;

    do {
        if (!map->entries[index].occupied)
            return NULL;
        if (map->entries[index].key == component_id)
            return map->entries[index].value;
        index = (index + 1) % map->capacity;
    } while (index != start);

    return NULL;
}

static void tecs_edge_map_set(tecs_edge_map_t* map,
                               tecs_component_id_t component_id,
                               tecs_archetype_t* target) {
    if (map->capacity == 0) return;

    size_t index = component_id % map->capacity;

    while (map->entries[index].occupied && map->entries[index].key != component_id)
        index = (index + 1) % map->capacity;

    map->entries[index].key = component_id;
    map->entries[index].value = target;
    map->entries[index].occupied = true;
}

/* ============================================================================
 * Archetype Management
 * ========================================================================= */

static tecs_archetype_t* tecs_archetype_new(const tecs_component_info_t* components,
                                             int component_count) {
    tecs_archetype_t* arch = TECS_CALLOC(1, sizeof(tecs_archetype_t));

    arch->component_count = component_count;
    arch->components = TECS_MALLOC(component_count * sizeof(tecs_component_info_t));
    memcpy(arch->components, components, component_count * sizeof(tecs_component_info_t));

    /* Sort components by ID */
    qsort(arch->components, component_count, sizeof(tecs_component_info_t),
          tecs_compare_component_info);

    /* Separate data components and tags */
    arch->data_component_count = 0;
    arch->tag_count = 0;
    for (int i = 0; i < component_count; i++) {
        if (components[i].size > 0) arch->data_component_count++;
        else arch->tag_count++;
    }

    arch->data_components = TECS_MALLOC(arch->data_component_count * sizeof(tecs_component_info_t));
    arch->tags = TECS_MALLOC(arch->tag_count * sizeof(tecs_component_info_t));

    int data_idx = 0, tag_idx = 0;
    for (int i = 0; i < component_count; i++) {
        if (arch->components[i].size > 0) {
            arch->data_components[data_idx] = arch->components[i];
            arch->data_components[data_idx].column_index = data_idx;
            data_idx++;
        } else {
            arch->tags[tag_idx++] = arch->components[i];
        }
    }

    /* Compute archetype hash */
    tecs_component_id_t* ids = TECS_MALLOC(component_count * sizeof(tecs_component_id_t));
    for (int i = 0; i < component_count; i++) {
        ids[i] = components[i].id;
    }
    arch->id = tecs_hash_component_set(ids, component_count);
    TECS_FREE(ids);

    /* Initialize chunk storage */
    arch->chunk_capacity = TECS_INITIAL_CHUNKS;
    arch->chunks = TECS_MALLOC(arch->chunk_capacity * sizeof(tecs_chunk_t*));
    arch->chunk_count = 0;
    arch->entity_count = 0;

    /* Initialize graph edges */
    arch->add_edge_capacity = 8;
    arch->add_edges = TECS_MALLOC(arch->add_edge_capacity * sizeof(tecs_archetype_edge_t));
    arch->add_edge_count = 0;

    arch->remove_edge_capacity = 8;
    arch->remove_edges = TECS_MALLOC(arch->remove_edge_capacity * sizeof(tecs_archetype_edge_t));
    arch->remove_edge_count = 0;

    /* Initialize hash maps for O(1) lookups */
    /* Use next power of 2 >= count * 2 for good load factor */
    int map_capacity = component_count * 2;
    if (map_capacity < 8) map_capacity = 8;

    tecs_component_map_init(&arch->component_map, map_capacity);
    
    /* Initialize data component map for O(1) column lookups */
    int data_map_capacity = arch->data_component_count * 2;
    if (data_map_capacity < 8) data_map_capacity = 8;
    tecs_component_map_init(&arch->data_component_map, data_map_capacity);
    
    tecs_edge_map_init(&arch->add_edge_map, 16);
    tecs_edge_map_init(&arch->remove_edge_map, 16);

    /* Populate component map */
    for (int i = 0; i < component_count; i++) {
        tecs_component_map_set(&arch->component_map, arch->components[i].id, i);
    }
    
    /* Populate data component map (component_id -> column index) */
    for (int i = 0; i < arch->data_component_count; i++) {
        tecs_component_map_set(&arch->data_component_map, arch->data_components[i].id, i);
    }

    return arch;
}

static void tecs_chunk_free(tecs_chunk_t* chunk, int column_count) {
    for (int i = 0; i < column_count; i++) {
        /* Free storage using provider */
        if (chunk->columns[i].provider && chunk->columns[i].provider->free_chunk) {
            chunk->columns[i].provider->free_chunk(
                chunk->columns[i].provider->user_data,
                chunk->columns[i].storage_data
            );
        }
        TECS_FREE(chunk->columns[i].changed_ticks);
        TECS_FREE(chunk->columns[i].added_ticks);
    }
    TECS_FREE(chunk->columns);
    TECS_FREE(chunk);
}

static void tecs_archetype_free(tecs_archetype_t* arch) {
    for (int i = 0; i < arch->chunk_count; i++) {
        tecs_chunk_free(arch->chunks[i], arch->data_component_count);
    }
    TECS_FREE(arch->chunks);
    TECS_FREE(arch->components);
    TECS_FREE(arch->data_components);
    TECS_FREE(arch->tags);
    TECS_FREE(arch->add_edges);
    TECS_FREE(arch->remove_edges);

    /* Free hash maps */
    tecs_component_map_free(&arch->component_map);
    tecs_component_map_free(&arch->data_component_map);
    tecs_edge_map_free(&arch->add_edge_map);
    tecs_edge_map_free(&arch->remove_edge_map);

    TECS_FREE(arch);
}

static tecs_chunk_t* tecs_chunk_new(tecs_world_t* world,
                                     int data_component_count,
                                     const tecs_component_info_t* data_components) {
    tecs_chunk_t* chunk = TECS_MALLOC(sizeof(tecs_chunk_t));
    chunk->count = 0;
    chunk->capacity = TECS_CHUNK_SIZE;
    chunk->columns = TECS_MALLOC(data_component_count * sizeof(tecs_column_t));

    for (int i = 0; i < data_component_count; i++) {
        tecs_component_id_t comp_id = data_components[i].id;
        
        /* Find storage provider for this component - O(1) lookup */
        tecs_storage_provider_t* provider = NULL;
        int registry_index = tecs_component_map_get(&world->component_registry_map, comp_id);
        if (registry_index >= 0) {
            provider = world->component_registry[registry_index].storage_provider;
        }
        
        /* Use default storage if none specified */
        if (!provider) {
            provider = &tecs_default_storage;
        }
        
        /* Allocate storage using provider */
        chunk->columns[i].storage_data = provider->alloc_chunk(
            provider->user_data,
            data_components[i].size,
            TECS_CHUNK_SIZE
        );
        chunk->columns[i].provider = provider;
        chunk->columns[i].is_native_storage = (provider == &tecs_default_storage);
        chunk->columns[i].changed_ticks = TECS_CALLOC(TECS_CHUNK_SIZE, sizeof(tecs_tick_t));
        chunk->columns[i].added_ticks = TECS_CALLOC(TECS_CHUNK_SIZE, sizeof(tecs_tick_t));
    }

    return chunk;
}

static void tecs_archetype_add_entity(tecs_world_t* world, tecs_archetype_t* arch, tecs_entity_t entity,
                                      tecs_entity_record_t* record, tecs_tick_t tick) {
    /* Find or create chunk with space */
    tecs_chunk_t* chunk = NULL;
    int chunk_idx = -1;

    for (int i = 0; i < arch->chunk_count; i++) {
        if (arch->chunks[i]->count < TECS_CHUNK_SIZE) {
            chunk = arch->chunks[i];
            chunk_idx = i;
            break;
        }
    }

    if (!chunk) {
        /* Allocate new chunk */
        if (arch->chunk_count >= arch->chunk_capacity) {
            arch->chunk_capacity *= 2;
            arch->chunks = TECS_REALLOC(arch->chunks,
                                        arch->chunk_capacity * sizeof(tecs_chunk_t*));
        }

        chunk = tecs_chunk_new(world, arch->data_component_count, arch->data_components);
        arch->chunks[arch->chunk_count] = chunk;
        chunk_idx = arch->chunk_count;
        arch->chunk_count++;
    }

    /* Add entity to chunk */
    int row = chunk->count;
    chunk->entities[row] = entity;
    chunk->count++;
    arch->entity_count++;

    /* Initialize ticks */
    for (int i = 0; i < arch->data_component_count; i++) {
        chunk->columns[i].added_ticks[row] = tick;
        chunk->columns[i].changed_ticks[row] = tick;
    }

    /* Update entity record */
    record->archetype = arch;
    record->chunk_index = chunk_idx;
    record->row = arch->entity_count - 1;  /* Global row index */
}

static void tecs_archetype_remove_entity(tecs_archetype_t* arch, int chunk_idx, int row) {
    tecs_chunk_t* chunk = arch->chunks[chunk_idx];

    /* Swap with last entity in chunk */
    int last_row = chunk->count - 1;
    if (row != last_row) {
        chunk->entities[row] = chunk->entities[last_row];

        /* Swap component data using storage provider */
        for (int i = 0; i < arch->data_component_count; i++) {
            tecs_column_t* column = &chunk->columns[i];
            int size = arch->data_components[i].size;
            
            /* Use provider's swap or copy operation */
            if (column->provider->swap_data) {
                /* Optimized swap if available */
                column->provider->swap_data(
                    column->provider->user_data,
                    column->storage_data,
                    row,
                    last_row,
                    size
                );
            } else {
                /* Fallback to copy */
                column->provider->copy_data(
                    column->provider->user_data,
                    column->storage_data,
                    last_row,
                    column->storage_data,
                    row,
                    size
                );
            }
            
            column->changed_ticks[row] = column->changed_ticks[last_row];
            column->added_ticks[row] = column->added_ticks[last_row];
        }
    }

    chunk->count--;
    arch->entity_count--;
}

static int tecs_archetype_find_component(const tecs_archetype_t* arch,
                                         tecs_component_id_t component_id) {
    /* Use hash map for O(1) lookup */
    return tecs_component_map_get(&arch->component_map, component_id);
}

static bool tecs_archetype_has_component(const tecs_archetype_t* arch,
                                         tecs_component_id_t component_id) {
    return tecs_archetype_find_component(arch, component_id) >= 0;
}

static void tecs_archetype_add_edge(tecs_archetype_t* arch, tecs_component_id_t component_id,
                                    tecs_archetype_t* target, bool is_add) {
    tecs_archetype_edge_t** edges = is_add ? &arch->add_edges : &arch->remove_edges;
    int* count = is_add ? &arch->add_edge_count : &arch->remove_edge_count;
    int* capacity = is_add ? &arch->add_edge_capacity : &arch->remove_edge_capacity;
    tecs_edge_map_t* edge_map = is_add ? &arch->add_edge_map : &arch->remove_edge_map;

    if (*count >= *capacity) {
        *capacity *= 2;
        *edges = TECS_REALLOC(*edges, *capacity * sizeof(tecs_archetype_edge_t));
    }

    (*edges)[*count].component_id = component_id;
    (*edges)[*count].target = target;
    (*count)++;

    /* Also add to hash map for O(1) lookup */
    tecs_edge_map_set(edge_map, component_id, target);
}

static tecs_archetype_t* tecs_archetype_find_edge(const tecs_archetype_t* arch,
                                                   tecs_component_id_t component_id,
                                                   bool is_add) {
    /* Use hash map for O(1) lookup */
    const tecs_edge_map_t* edge_map = is_add ? &arch->add_edge_map : &arch->remove_edge_map;
    return tecs_edge_map_get(edge_map, component_id);
}

/* ============================================================================
 * World Management
 * ========================================================================= */

tecs_world_t* tecs_world_new(void) {
    tecs_world_t* world = TECS_CALLOC(1, sizeof(tecs_world_t));

    tecs_sparse_set_init(&world->entities);

    /* Create root archetype (empty) */
    world->root_archetype = tecs_archetype_new(NULL, 0);

    /* Initialize archetype hash table */
    world->archetype_table_capacity = TECS_INITIAL_ARCHETYPES;
    world->archetype_table = TECS_CALLOC(world->archetype_table_capacity,
                                         sizeof(tecs_archetype_table_entry_t));
    world->archetype_table_size = 0;
    
    /* Insert root archetype using hash table logic */
    size_t index = world->root_archetype->id % world->archetype_table_capacity;
    world->archetype_table[index].hash = world->root_archetype->id;
    world->archetype_table[index].archetype = world->root_archetype;
    world->archetype_table_size = 1;

    /* Initialize component registry */
    world->component_capacity = TECS_MAX_COMPONENTS;
    world->component_registry = TECS_MALLOC(world->component_capacity *
                                            sizeof(tecs_component_registry_entry_t));
    world->component_count = 0;
    tecs_component_map_init(&world->component_registry_map, TECS_MAX_COMPONENTS);

    /* Initialize deferred command buffer */
    world->command_capacity = 256;
    world->command_buffer = TECS_MALLOC(world->command_capacity * sizeof(tecs_command_t));
    world->command_count = 0;
    world->in_deferred = false;

    world->tick = 0;
    world->structural_change_version = 0;

    /* Initialize entity children hashmap */
    world->entity_children.capacity = 32;
    world->entity_children.keys = TECS_MALLOC(world->entity_children.capacity * sizeof(tecs_entity_t));
    world->entity_children.values = TECS_MALLOC(world->entity_children.capacity * sizeof(tecs_children_t*));
    world->entity_children.count = 0;

    /* Auto-register hierarchy components (stored in world, not globals) */
    world->parent_component_id = tecs_register_component(world, "tecs_parent_t", sizeof(tecs_parent_t));
    world->children_component_id = tecs_register_component(world, "tecs_children_t", sizeof(tecs_children_t));

    return world;
}

void tecs_world_free(tecs_world_t* world) {
    if (!world) return;

    /* Free all archetypes - iterate through hash table capacity */
    for (int i = 0; i < world->archetype_table_capacity; i++) {
        if (world->archetype_table[i].archetype) {
            tecs_archetype_free(world->archetype_table[i].archetype);
        }
    }

    TECS_FREE(world->archetype_table);
    TECS_FREE(world->component_registry);
    tecs_component_map_free(&world->component_registry_map);

    /* Free command buffer */
    for (int i = 0; i < world->command_count; i++) {
        if (world->command_buffer[i].data) {
            TECS_FREE(world->command_buffer[i].data);
        }
    }
    TECS_FREE(world->command_buffer);

    /* Free entity children hashmap */
    for (int i = 0; i < world->entity_children.count; i++) {
        if (world->entity_children.values[i]) {
            TECS_FREE(world->entity_children.values[i]->entities);
            TECS_FREE(world->entity_children.values[i]);
        }
    }
    TECS_FREE(world->entity_children.keys);
    TECS_FREE(world->entity_children.values);

    tecs_sparse_set_free(&world->entities);
    TECS_FREE(world);
}

void tecs_world_update(tecs_world_t* world) {
    world->tick++;
}

tecs_tick_t tecs_world_tick(const tecs_world_t* world) {
    return world->tick;
}

int tecs_world_entity_count(const tecs_world_t* world) {
    return world->entities.dense_count;
}

void tecs_world_clear(tecs_world_t* world) {
    /* Clear all entities and reset to root archetype */
    world->entities.dense_count = 0;
    world->entities.recycled_count = 0;
    world->tick = 0;
    world->structural_change_version++;

    /* Clear all archetypes except root - iterate through hash table capacity */
    for (int i = 0; i < world->archetype_table_capacity; i++) {
        if (world->archetype_table[i].archetype && 
            world->archetype_table[i].archetype != world->root_archetype) {
            tecs_archetype_free(world->archetype_table[i].archetype);
            world->archetype_table[i].archetype = NULL;
            world->archetype_table[i].hash = 0;
        }
    }
    world->archetype_table_size = 1;  /* Only root remains */

    /* Clear root archetype chunks */
    for (int i = 0; i < world->root_archetype->chunk_count; i++) {
        world->root_archetype->chunks[i]->count = 0;
    }
    world->root_archetype->entity_count = 0;
}

/* ============================================================================
 * Component Registration
 * ========================================================================= */

tecs_component_id_t tecs_register_component_ex(tecs_world_t* world, const char* name, int size,
                                                tecs_storage_provider_t* storage_provider) {
    if (world->component_count >= world->component_capacity) {
        world->component_capacity *= 2;
        world->component_registry = TECS_REALLOC(world->component_registry,
                                                 world->component_capacity *
                                                 sizeof(tecs_component_registry_entry_t));
    }

    tecs_component_id_t id = world->component_count + 1;  /* Start at 1, 0 is reserved */

    int registry_index = world->component_count;
    world->component_registry[registry_index].id = id;
    strncpy(world->component_registry[registry_index].name, name, 63);
    world->component_registry[registry_index].name[63] = '\0';
    world->component_registry[registry_index].size = size;
    world->component_registry[registry_index].storage_provider = storage_provider;
    world->component_count++;
    
    /* Add to hashmap for O(1) lookup */
    tecs_component_map_set(&world->component_registry_map, id, registry_index);

    return id;
}

tecs_component_id_t tecs_register_component(tecs_world_t* world, const char* name, int size) {
    return tecs_register_component_ex(world, name, size, NULL);
}

tecs_component_id_t tecs_get_component_id(const tecs_world_t* world, const char* name) {
    if (!world || !name) {
        return 0;
    }

    for (int i = 0; i < world->component_count; i++) {
        if (strcmp(world->component_registry[i].name, name) == 0) {
            return world->component_registry[i].id;
        }
    }

    return 0;  /* Component not found */
}

/* ============================================================================
 * Archetype Hash Table
 * ========================================================================= */

static tecs_archetype_t* tecs_world_find_archetype(const tecs_world_t* world, uint64_t hash) {
    if (world->archetype_table_capacity == 0) return NULL;
    
    /* O(1) hash table lookup with linear probing */
    size_t index = hash % world->archetype_table_capacity;
    size_t start = index;
    
    do {
        if (world->archetype_table[index].archetype == NULL) {
            return NULL;  /* Empty slot, archetype doesn't exist */
        }
        if (world->archetype_table[index].hash == hash) {
            return world->archetype_table[index].archetype;
        }
        index = (index + 1) % world->archetype_table_capacity;
    } while (index != start);
    
    return NULL;  /* Table is full and archetype not found */
}

static void tecs_world_add_archetype(tecs_world_t* world, tecs_archetype_t* arch) {
    /* Rehash if load factor exceeds 0.7 */
    if (world->archetype_table_size >= (world->archetype_table_capacity * 7) / 10) {
        int old_capacity = world->archetype_table_capacity;
        int new_capacity = old_capacity * 2;
        tecs_archetype_table_entry_t* old_table = world->archetype_table;
        
        /* Allocate new table and zero-initialize */
        world->archetype_table = TECS_CALLOC(new_capacity, sizeof(tecs_archetype_table_entry_t));
        world->archetype_table_capacity = new_capacity;
        world->archetype_table_size = 0;
        
        /* Rehash all existing entries */
        for (int i = 0; i < old_capacity; i++) {
            if (old_table[i].archetype != NULL) {
                /* Insert into new table with linear probing */
                size_t index = old_table[i].hash % new_capacity;
                while (world->archetype_table[index].archetype != NULL) {
                    index = (index + 1) % new_capacity;
                }
                world->archetype_table[index] = old_table[i];
                world->archetype_table_size++;
            }
        }
        
        TECS_FREE(old_table);
    }
    
    /* Insert new archetype with linear probing */
    size_t index = arch->id % world->archetype_table_capacity;
    while (world->archetype_table[index].archetype != NULL) {
        index = (index + 1) % world->archetype_table_capacity;
    }
    
    world->archetype_table[index].hash = arch->id;
    world->archetype_table[index].archetype = arch;
    world->archetype_table_size++;
    world->structural_change_version++;
}

/* ============================================================================
 * Entity Operations
 * ========================================================================= */

tecs_entity_t tecs_entity_new(tecs_world_t* world) {
    tecs_entity_t entity = tecs_sparse_set_create(&world->entities);
    tecs_entity_record_t* record = tecs_sparse_set_get(&world->entities, entity);

    /* Add to root archetype */
    tecs_archetype_add_entity(world, world->root_archetype, entity, record, world->tick);

    return entity;
}

tecs_entity_t tecs_entity_new_with_id(tecs_world_t* world, tecs_entity_t id) {
    (void)id;  /* Unused parameter */
    /* For now, just use the provided ID without validation */
    /* In production, should validate and handle collisions */
    return tecs_entity_new(world);
}

void tecs_entity_delete(tecs_world_t* world, tecs_entity_t entity) {
    tecs_entity_record_t* record = tecs_sparse_set_get(&world->entities, entity);
    if (!record || !record->archetype) return;

    /* Remove from archetype */
    tecs_archetype_remove_entity(record->archetype, record->chunk_index,
                                 record->row % TECS_CHUNK_SIZE);

    /* Remove from sparse set */
    tecs_sparse_set_remove(&world->entities, entity);
}

bool tecs_entity_exists(const tecs_world_t* world, tecs_entity_t entity) {
    return tecs_sparse_set_get(&world->entities, entity) != NULL;
}

/* ============================================================================
 * Component Operations
 * ========================================================================= */

static tecs_archetype_t* tecs_world_get_or_create_archetype_with_component(
    tecs_world_t* world, tecs_archetype_t* current, tecs_component_id_t component_id, int size) {

    /* Check graph edge cache */
    tecs_archetype_t* target = tecs_archetype_find_edge(current, component_id, true);
    if (target) return target;

    /* Build new component set */
    int new_count = current->component_count + 1;
    tecs_component_info_t* new_components = TECS_MALLOC(new_count * sizeof(tecs_component_info_t));
    memcpy(new_components, current->components,
           current->component_count * sizeof(tecs_component_info_t));
    new_components[new_count - 1].id = component_id;
    new_components[new_count - 1].size = size;
    new_components[new_count - 1].column_index = -1;  /* Will be set in tecs_archetype_new */

    /* Compute hash */
    tecs_component_id_t* ids = TECS_MALLOC(new_count * sizeof(tecs_component_id_t));
    for (int i = 0; i < new_count; i++) {
        ids[i] = new_components[i].id;
    }
    uint64_t hash = tecs_hash_component_set(ids, new_count);
    TECS_FREE(ids);

    /* Check if archetype exists */
    target = tecs_world_find_archetype(world, hash);
    if (!target) {
        target = tecs_archetype_new(new_components, new_count);
        tecs_world_add_archetype(world, target);
    }

    TECS_FREE(new_components);

    /* Add graph edge */
    tecs_archetype_add_edge(current, component_id, target, true);
    tecs_archetype_add_edge(target, component_id, current, false);

    return target;
}

static tecs_archetype_t* tecs_world_get_or_create_archetype_without_component(
    tecs_world_t* world, tecs_archetype_t* current, tecs_component_id_t component_id) {

    /* Check graph edge cache */
    tecs_archetype_t* target = tecs_archetype_find_edge(current, component_id, false);
    if (target) return target;

    /* Build new component set (remove component) */
    int new_count = current->component_count - 1;
    if (new_count < 0) return current;

    tecs_component_info_t* new_components = TECS_MALLOC(new_count * sizeof(tecs_component_info_t));
    int idx = 0;
    for (int i = 0; i < current->component_count; i++) {
        if (current->components[i].id != component_id) {
            new_components[idx++] = current->components[i];
        }
    }

    if (idx != new_count) {
        TECS_FREE(new_components);
        return current;  /* Component not found */
    }

    /* Compute hash */
    uint64_t hash = 0;
    if (new_count > 0) {
        tecs_component_id_t* ids = TECS_MALLOC(new_count * sizeof(tecs_component_id_t));
        for (int i = 0; i < new_count; i++) {
            ids[i] = new_components[i].id;
        }
        hash = tecs_hash_component_set(ids, new_count);
        TECS_FREE(ids);
    }

    /* Check if archetype exists (or return root if empty) */
    target = (new_count == 0) ? world->root_archetype : tecs_world_find_archetype(world, hash);
    if (!target && new_count > 0) {
        target = tecs_archetype_new(new_components, new_count);
        tecs_world_add_archetype(world, target);
    }

    TECS_FREE(new_components);

    /* Add graph edge */
    tecs_archetype_add_edge(current, component_id, target, false);
    tecs_archetype_add_edge(target, component_id, current, true);

    return target;
}

static void tecs_copy_component_data(tecs_archetype_t* src_arch, tecs_chunk_t* src_chunk, int src_row,
                                     tecs_archetype_t* dst_arch, tecs_chunk_t* dst_chunk, int dst_row) {
    /* Copy matching components from source to destination - O(n) with hashmap */
    for (int i = 0; i < src_arch->data_component_count; i++) {
        tecs_component_id_t comp_id = src_arch->data_components[i].id;

        /* O(1) hashmap lookup instead of O(n) inner loop */
        int dst_column_idx = tecs_component_map_get(&dst_arch->data_component_map, comp_id);
        if (dst_column_idx < 0) continue;  /* Component not in destination archetype */

        int src_size = src_arch->data_components[i].size;
        int dst_size = dst_arch->data_components[dst_column_idx].size;
        assert(src_size == dst_size);

        /* Use storage provider copy_data API */
        tecs_column_t* src_column = &src_chunk->columns[i];
        tecs_column_t* dst_column = &dst_chunk->columns[dst_column_idx];
        
        dst_column->provider->copy_data(
            dst_column->provider->user_data,
            src_column->storage_data,
            src_row,
            dst_column->storage_data,
            dst_row,
            src_size
        );

        /* Copy ticks */
        dst_column->changed_ticks[dst_row] = src_column->changed_ticks[src_row];
        dst_column->added_ticks[dst_row] = src_column->added_ticks[src_row];
    }
}

void tecs_set(tecs_world_t* world, tecs_entity_t entity, tecs_component_id_t component_id,
              const void* data, int size) {
    tecs_entity_record_t* record = tecs_sparse_set_get(&world->entities, entity);
    if (!record) return;

    tecs_archetype_t* current_arch = record->archetype;

    /* Check if component already exists */
    int comp_idx = tecs_archetype_find_component(current_arch, component_id);
    if (comp_idx >= 0) {
        /* Update existing component - O(1) hashmap lookup */
        int column_idx = tecs_component_map_get(&current_arch->data_component_map, component_id);
        if (column_idx < 0) {
            return;  /* Tag component, no data to update */
        }
        
        int chunk_idx = record->chunk_index;
        int row = record->row % TECS_CHUNK_SIZE;
        tecs_chunk_t* chunk = current_arch->chunks[chunk_idx];
        tecs_column_t* column = &chunk->columns[column_idx];
        
        /* Use storage provider API */
        column->provider->set_data(
            column->provider->user_data,
            column->storage_data,
            row,
            data,
            size
        );
        column->changed_ticks[row] = world->tick;
        return;
    }

    /* Need to add component (archetype transition) */
    tecs_archetype_t* new_arch = tecs_world_get_or_create_archetype_with_component(
        world, current_arch, component_id, size);

    if (new_arch == current_arch) return;

    /* Get old chunk location */
    int old_chunk_idx = record->chunk_index;
    int old_row = record->row % TECS_CHUNK_SIZE;
    tecs_chunk_t* old_chunk = current_arch->chunks[old_chunk_idx];
    tecs_entity_t entity_id = old_chunk->entities[old_row];

    /* Add to new archetype */
    tecs_archetype_add_entity(world, new_arch, entity_id, record, world->tick);

    /* Copy existing component data */
    int new_chunk_idx = record->chunk_index;
    int new_row = record->row % TECS_CHUNK_SIZE;
    tecs_chunk_t* new_chunk = new_arch->chunks[new_chunk_idx];

    tecs_copy_component_data(current_arch, old_chunk, old_row,
                            new_arch, new_chunk, new_row);

    /* Set new component data - O(1) hashmap lookup */
    int new_column_idx = tecs_component_map_get(&new_arch->data_component_map, component_id);
    if (new_column_idx >= 0) {
        tecs_column_t* new_column = &new_chunk->columns[new_column_idx];
        
        /* Use storage provider API */
        new_column->provider->set_data(
            new_column->provider->user_data,
            new_column->storage_data,
            new_row,
            data,
            size
        );
        new_column->changed_ticks[new_row] = world->tick;
        new_column->added_ticks[new_row] = world->tick;
    }

    /* Remove from old archetype */
    tecs_archetype_remove_entity(current_arch, old_chunk_idx, old_row);
}

void* tecs_get(tecs_world_t* world, tecs_entity_t entity, tecs_component_id_t component_id) {
    tecs_entity_record_t* record = tecs_sparse_set_get(&world->entities, entity);
    if (!record || !record->archetype) return NULL;

    tecs_archetype_t* arch = record->archetype;

    /* O(1) hashmap lookup instead of O(n) linear search */
    int column_idx = tecs_component_map_get(&arch->data_component_map, component_id);
    if (column_idx < 0) return NULL;  /* Component not found or is a tag */

    int chunk_idx = record->chunk_index;
    int row = record->row % TECS_CHUNK_SIZE;
    tecs_chunk_t* chunk = arch->chunks[chunk_idx];
    tecs_column_t* column = &chunk->columns[column_idx];
    
    /* Use storage provider API */
    return column->provider->get_ptr(
        column->provider->user_data,
        column->storage_data,
        row,
        arch->data_components[column_idx].size
    );
}

const void* tecs_get_const(const tecs_world_t* world, tecs_entity_t entity,
                           tecs_component_id_t component_id) {
    return tecs_get((tecs_world_t*)world, entity, component_id);
}

bool tecs_has(const tecs_world_t* world, tecs_entity_t entity, tecs_component_id_t component_id) {
    tecs_entity_record_t* record = tecs_sparse_set_get(&world->entities, entity);
    if (!record || !record->archetype) return false;

    return tecs_archetype_has_component(record->archetype, component_id);
}

void tecs_unset(tecs_world_t* world, tecs_entity_t entity, tecs_component_id_t component_id) {
    tecs_entity_record_t* record = tecs_sparse_set_get(&world->entities, entity);
    if (!record || !record->archetype) return;

    tecs_archetype_t* current_arch = record->archetype;
    if (!tecs_archetype_has_component(current_arch, component_id)) return;

    /* Get new archetype without component */
    tecs_archetype_t* new_arch = tecs_world_get_or_create_archetype_without_component(
        world, current_arch, component_id);

    if (new_arch == current_arch) return;

    /* Get old chunk location */
    int old_chunk_idx = record->chunk_index;
    int old_row = record->row % TECS_CHUNK_SIZE;
    tecs_chunk_t* old_chunk = current_arch->chunks[old_chunk_idx];
    tecs_entity_t entity_id = old_chunk->entities[old_row];

    /* Add to new archetype */
    tecs_archetype_add_entity(world, new_arch, entity_id, record, world->tick);

    /* Copy remaining component data */
    int new_chunk_idx = record->chunk_index;
    int new_row = record->row % TECS_CHUNK_SIZE;
    tecs_chunk_t* new_chunk = new_arch->chunks[new_chunk_idx];

    tecs_copy_component_data(current_arch, old_chunk, old_row,
                            new_arch, new_chunk, new_row);

    /* Remove from old archetype */
    tecs_archetype_remove_entity(current_arch, old_chunk_idx, old_row);
}

void tecs_add_tag(tecs_world_t* world, tecs_entity_t entity, tecs_component_id_t tag_id) {
    /* Tags are zero-sized components */
    tecs_set(world, entity, tag_id, NULL, 0);
}

void tecs_mark_changed(tecs_world_t* world, tecs_entity_t entity,
                      tecs_component_id_t component_id) {
    tecs_entity_record_t* record = tecs_sparse_set_get(&world->entities, entity);
    if (!record || !record->archetype) return;

    tecs_archetype_t* arch = record->archetype;
    
    /* O(1) hashmap lookup instead of O(n) linear search */
    int column_idx = tecs_component_map_get(&arch->data_component_map, component_id);
    if (column_idx < 0) return;  /* Component not found or is a tag */
    
    int chunk_idx = record->chunk_index;
    int row = record->row % TECS_CHUNK_SIZE;
    tecs_chunk_t* chunk = arch->chunks[chunk_idx];
    chunk->columns[column_idx].changed_ticks[row] = world->tick;
}

/* ============================================================================
 * Query Operations
 * ========================================================================= */

tecs_query_t* tecs_query_new(tecs_world_t* world) {
    tecs_query_t* query = TECS_CALLOC(1, sizeof(tecs_query_t));
    query->world = world;
    query->term_count = 0;
    query->matched_capacity = 16;
    query->matched_archetypes = TECS_MALLOC(query->matched_capacity * sizeof(tecs_archetype_t*));
    query->matched_count = 0;
    query->last_structural_version = 0;
    query->built = false;
    return query;
}

void tecs_query_free(tecs_query_t* query) {
    if (!query) return;
    TECS_FREE(query->matched_archetypes);
    TECS_FREE(query);
}

static void tecs_query_add_term(tecs_query_t* query, tecs_term_type_t type,
                               tecs_component_id_t component_id) {
    if (query->term_count >= TECS_MAX_QUERY_TERMS) return;

    query->terms[query->term_count].type = type;
    query->terms[query->term_count].component_id = component_id;
    query->terms[query->term_count].data_index = -1;
    query->term_count++;
}

void tecs_query_with(tecs_query_t* query, tecs_component_id_t component_id) {
    tecs_query_add_term(query, TECS_TERM_WITH, component_id);
}

void tecs_query_without(tecs_query_t* query, tecs_component_id_t component_id) {
    tecs_query_add_term(query, TECS_TERM_WITHOUT, component_id);
}

void tecs_query_optional(tecs_query_t* query, tecs_component_id_t component_id) {
    tecs_query_add_term(query, TECS_TERM_OPTIONAL, component_id);
}

void tecs_query_changed(tecs_query_t* query, tecs_component_id_t component_id) {
    tecs_query_add_term(query, TECS_TERM_CHANGED, component_id);
}

void tecs_query_added(tecs_query_t* query, tecs_component_id_t component_id) {
    tecs_query_add_term(query, TECS_TERM_ADDED, component_id);
}

static bool tecs_archetype_matches_query(const tecs_archetype_t* arch, const tecs_query_t* query) {
    for (int i = 0; i < query->term_count; i++) {
        const tecs_query_term_t* term = &query->terms[i];
        bool has = tecs_archetype_has_component(arch, term->component_id);

        switch (term->type) {
            case TECS_TERM_WITH:
            case TECS_TERM_CHANGED:
            case TECS_TERM_ADDED:
                if (!has) return false;
                break;
            case TECS_TERM_WITHOUT:
                if (has) return false;
                break;
            case TECS_TERM_OPTIONAL:
                /* Always matches */
                break;
        }
    }
    return true;
}

void tecs_query_build(tecs_query_t* query) {
    query->matched_count = 0;

    /* Match against all archetypes - iterate through hash table capacity */
    for (int i = 0; i < query->world->archetype_table_capacity; i++) {
        tecs_archetype_t* arch = query->world->archetype_table[i].archetype;
        if (arch && tecs_archetype_matches_query(arch, query)) {
            if (query->matched_count >= query->matched_capacity) {
                query->matched_capacity *= 2;
                query->matched_archetypes = TECS_REALLOC(query->matched_archetypes,
                    query->matched_capacity * sizeof(tecs_archetype_t*));
            }
            query->matched_archetypes[query->matched_count++] = arch;
        }
    }

    query->last_structural_version = query->world->structural_change_version;
    query->built = true;
}

/* ============================================================================
 * Query Iteration
 * ========================================================================= */

void tecs_query_iter_init(tecs_query_iter_t* iter, tecs_query_t* query) {
    /* Rebuild if world structure changed */
    if (!query->built ||
        query->last_structural_version != query->world->structural_change_version) {
        tecs_query_build(query);
    }

    iter->query = query;
    iter->archetype_index = 0;
    iter->chunk_index = -1;
    iter->current_chunk = NULL;
    iter->current_archetype = NULL;
}

tecs_query_iter_t* tecs_query_iter(tecs_query_t* query) {
    tecs_query_iter_t* iter = TECS_CALLOC(1, sizeof(tecs_query_iter_t));
    tecs_query_iter_init(iter, query);
    return iter;
}

tecs_query_iter_t* tecs_query_iter_cached(tecs_query_t* query) {
    tecs_query_iter_init(&query->cached_iter, query);
    return &query->cached_iter;
}

bool tecs_iter_next(tecs_query_iter_t* iter) {
    if (!iter || !iter->query) return false;

    /* Advance to next chunk */
    iter->chunk_index++;

    /* Find next non-empty chunk */
    while (iter->archetype_index < iter->query->matched_count) {
        iter->current_archetype = iter->query->matched_archetypes[iter->archetype_index];

        if (iter->chunk_index < iter->current_archetype->chunk_count) {
            iter->current_chunk = iter->current_archetype->chunks[iter->chunk_index];
            if (iter->current_chunk->count > 0) {
                return true;
            }
            iter->chunk_index++;
        } else {
            /* Move to next archetype */
            iter->archetype_index++;
            iter->chunk_index = 0;
        }
    }

    return false;
}

void tecs_query_iter_free(tecs_query_iter_t* iter) {
    TECS_FREE(iter);
}

int tecs_iter_count(const tecs_query_iter_t* iter) {
    return iter->current_chunk ? iter->current_chunk->count : 0;
}

tecs_entity_t* tecs_iter_entities(const tecs_query_iter_t* iter) {
    return iter->current_chunk ? iter->current_chunk->entities : NULL;
}

void* tecs_iter_column(const tecs_query_iter_t* iter, int index) {
    if (!iter->current_chunk || !iter->current_archetype) return NULL;
    if (index < 0 || index >= iter->current_archetype->data_component_count) return NULL;

    tecs_column_t* column = &iter->current_chunk->columns[index];
    
    /* Fast path for native storage - return raw pointer to array */
    if (column->is_native_storage) {
        tecs_native_storage_t* storage = (tecs_native_storage_t*)column->storage_data;
        return storage->data;
    }
    
    /* Custom storage - return NULL (caller should use tecs_iter_get_at instead) */
    return NULL;
}

tecs_tick_t* tecs_iter_changed_ticks(const tecs_query_iter_t* iter, int index) {
    if (!iter->current_chunk || !iter->current_archetype) return NULL;
    if (index < 0 || index >= iter->current_archetype->data_component_count) return NULL;

    return iter->current_chunk->columns[index].changed_ticks;
}

tecs_tick_t* tecs_iter_added_ticks(const tecs_query_iter_t* iter, int index) {
    if (!iter->current_chunk || !iter->current_archetype) return NULL;
    if (index < 0 || index >= iter->current_archetype->data_component_count) return NULL;

    return iter->current_chunk->columns[index].added_ticks;
}

TECS_API void* tecs_iter_chunk_data(const tecs_query_iter_t* iter, int column_index) {
    if (!iter->current_chunk || !iter->current_archetype) return NULL;
    if (column_index < 0 || column_index >= iter->current_archetype->data_component_count) return NULL;

    return iter->current_chunk->columns[column_index].storage_data;
}

tecs_storage_provider_t* tecs_iter_storage_provider(const tecs_query_iter_t* iter, int index) {
    if (!iter->current_chunk || !iter->current_archetype) return NULL;
    if (index < 0 || index >= iter->current_archetype->data_component_count) return NULL;

    return iter->current_chunk->columns[index].provider;
}

/* ============================================================================
 * Deferred Operations
 * ========================================================================= */

void tecs_begin_deferred(tecs_world_t* world) {
    world->in_deferred = true;
}

void tecs_end_deferred(tecs_world_t* world) {
    world->in_deferred = false;

    /* Apply all deferred commands */
    for (int i = 0; i < world->command_count; i++) {
        tecs_command_t* cmd = &world->command_buffer[i];

        switch (cmd->type) {
            case TECS_CMD_SET_COMPONENT:
                tecs_set(world, cmd->entity, cmd->component_id, cmd->data, cmd->size);
                TECS_FREE(cmd->data);
                break;

            case TECS_CMD_UNSET_COMPONENT:
                tecs_unset(world, cmd->entity, cmd->component_id);
                break;

            case TECS_CMD_DELETE_ENTITY:
                tecs_entity_delete(world, cmd->entity);
                break;
        }
    }

    world->command_count = 0;
}

/* ============================================================================
 * Memory Management
 * ========================================================================= */

int tecs_remove_empty_archetypes(tecs_world_t* world) {
    int removed = 0;

    /* Iterate through hash table capacity */
    for (int i = 0; i < world->archetype_table_capacity; i++) {
        tecs_archetype_t* arch = world->archetype_table[i].archetype;
        if (arch && arch->entity_count == 0 && arch != world->root_archetype) {
            tecs_archetype_free(arch);
            world->archetype_table[i].archetype = NULL;
            world->archetype_table[i].hash = 0;
            world->archetype_table_size--;
            removed++;
        }
    }

    if (removed > 0) {
        world->structural_change_version++;
    }

    return removed;
}

/* ============================================================================
 * Hierarchy Operations Implementation
 * ========================================================================= */

tecs_component_id_t tecs_get_parent_component_id(const tecs_world_t* world) {
    return world ? world->parent_component_id : 0;
}

tecs_component_id_t tecs_get_children_component_id(const tecs_world_t* world) {
    return world ? world->children_component_id : 0;
}

/* Helper macros for internal use */
#define PARENT_ID (world->parent_component_id)
#define CHILDREN_ID (world->children_component_id)

/* Internal: Find children list for an entity in the hashmap */
static tecs_children_t* tecs_find_children(const tecs_world_t* world, tecs_entity_t entity) {
    for (int i = 0; i < world->entity_children.count; i++) {
        if (world->entity_children.keys[i] == entity) {
            return world->entity_children.values[i];
        }
    }
    return NULL;
}

/* Internal: Get or create children list for an entity */
static tecs_children_t* tecs_ensure_children(tecs_world_t* world, tecs_entity_t entity) {
    /* Try to find existing */
    tecs_children_t* children = tecs_find_children(world, entity);
    if (children) return children;

    /* Need to grow hashmap? */
    if (world->entity_children.count >= world->entity_children.capacity) {
        int new_capacity = world->entity_children.capacity * 2;
        world->entity_children.keys = TECS_REALLOC(world->entity_children.keys,
                                                    new_capacity * sizeof(tecs_entity_t));
        world->entity_children.values = TECS_REALLOC(world->entity_children.values,
                                                      new_capacity * sizeof(tecs_children_t*));
        world->entity_children.capacity = new_capacity;
    }

    /* Create new children list */
    children = TECS_MALLOC(sizeof(tecs_children_t));
    children->capacity = 4;
    children->count = 0;
    children->entities = TECS_MALLOC(children->capacity * sizeof(tecs_entity_t));

    /* Insert into hashmap */
    int idx = world->entity_children.count++;
    world->entity_children.keys[idx] = entity;
    world->entity_children.values[idx] = children;

    return children;
}

/* Internal: Remove children list from hashmap */
static void tecs_remove_children_entry(tecs_world_t* world, tecs_entity_t entity) {
    for (int i = 0; i < world->entity_children.count; i++) {
        if (world->entity_children.keys[i] == entity) {
            /* Free the children list */
            if (world->entity_children.values[i]) {
                TECS_FREE(world->entity_children.values[i]->entities);
                TECS_FREE(world->entity_children.values[i]);
            }

            /* Swap with last element */
            int last = world->entity_children.count - 1;
            if (i != last) {
                world->entity_children.keys[i] = world->entity_children.keys[last];
                world->entity_children.values[i] = world->entity_children.values[last];
            }
            world->entity_children.count--;
            return;
        }
    }
}

void tecs_add_child(tecs_world_t* world, tecs_entity_t parent, tecs_entity_t child) {
    if (!world || !tecs_entity_exists(world, parent) || !tecs_entity_exists(world, child) || parent == child) return;

    /* Check for cycles - child cannot be ancestor of parent */
    if (tecs_is_ancestor_of(world, child, parent)) {
        return; /* Would create cycle */
    }

    /* Get current parent if exists */
    tecs_parent_t* current_parent = (tecs_parent_t*)tecs_get(world, child, PARENT_ID);
    if (current_parent && current_parent->parent == parent) {
        return; /* Already parented to this entity */
    }

    /* Remove from old parent if exists (use hashmap) */
    if (current_parent) {
        tecs_entity_t old_parent = current_parent->parent;
        tecs_children_t* old_children = tecs_find_children(world, old_parent);
        if (old_children) {
            /* Remove child from old parent's children array */
            for (int i = 0; i < old_children->count; i++) {
                if (old_children->entities[i] == child) {
                    /* Swap with last element */
                    old_children->entities[i] = old_children->entities[--old_children->count];

                    /* Mirror to ECS (or unset if empty) */
                    if (old_children->count > 0) {
                        tecs_set(world, old_parent, CHILDREN_ID, old_children, sizeof(tecs_children_t));
                    } else {
                        tecs_remove_children_entry(world, old_parent);
                        tecs_unset(world, old_parent, CHILDREN_ID);
                    }
                    break;
                }
            }
        }
    }

    /* Set new Parent component on child */
    tecs_parent_t new_parent = { parent };
    tecs_set(world, child, PARENT_ID, &new_parent, sizeof(tecs_parent_t));

    /* Add child to new parent's Children list (use hashmap) */
    tecs_children_t* children = tecs_ensure_children(world, parent);

    /* Ensure capacity */
    if (children->count >= children->capacity) {
        int new_capacity = children->capacity * 2;
        tecs_entity_t* new_array = (tecs_entity_t*)TECS_REALLOC(children->entities,
                                                                  new_capacity * sizeof(tecs_entity_t));
        if (!new_array) return; /* OOM */

        children->entities = new_array;
        children->capacity = new_capacity;
    }

    /* Add child to array */
    children->entities[children->count++] = child;

    /* Mirror to ECS component (for queries) */
    tecs_set(world, parent, CHILDREN_ID, children, sizeof(tecs_children_t));
}

void tecs_remove_child(tecs_world_t* world, tecs_entity_t parent, tecs_entity_t child) {
    if (!world || !tecs_entity_exists(world, parent) || !tecs_entity_exists(world, child)) return;

    /* Verify child actually has this parent */
    tecs_parent_t* parent_comp = (tecs_parent_t*)tecs_get(world, child, PARENT_ID);
    if (!parent_comp || parent_comp->parent != parent) {
        return; /* Not a child of this parent */
    }

    /* Remove Parent component from child */
    tecs_unset(world, child, PARENT_ID);

    /* Remove child from parent's Children list (use hashmap) */
    tecs_children_t* children = tecs_find_children(world, parent);
    if (!children) return;

    for (int i = 0; i < children->count; i++) {
        if (children->entities[i] == child) {
            /* Swap with last element */
            children->entities[i] = children->entities[--children->count];

            /* Mirror to ECS (or unset if empty) */
            if (children->count > 0) {
                tecs_set(world, parent, CHILDREN_ID, children, sizeof(tecs_children_t));
            } else {
                tecs_remove_children_entry(world, parent);
                tecs_unset(world, parent, CHILDREN_ID);
            }
            break;
        }
    }
}

void tecs_remove_all_children(tecs_world_t* world, tecs_entity_t parent) {
    if (!world || !tecs_entity_exists(world, parent)) return;

    tecs_children_t* children = tecs_find_children(world, parent);
    if (!children || children->count == 0) return;

    /* Remove Parent component from all children */
    for (int i = 0; i < children->count; i++) {
        tecs_unset(world, children->entities[i], PARENT_ID);
    }

    /* Free from hashmap and remove ECS component */
    tecs_remove_children_entry(world, parent);
    tecs_unset(world, parent, CHILDREN_ID);
}

tecs_entity_t tecs_get_parent(const tecs_world_t* world, tecs_entity_t child) {
    if (!world || !tecs_entity_exists(world, child)) return TECS_ENTITY_NULL;

    const tecs_parent_t* parent = (const tecs_parent_t*)tecs_get_const(world, child, PARENT_ID);
    return parent ? parent->parent : 0;
}

bool tecs_has_parent(const tecs_world_t* world, tecs_entity_t child) {
    return tecs_has(world, child, PARENT_ID);
}

const tecs_children_t* tecs_get_children(const tecs_world_t* world, tecs_entity_t parent) {
    if (!world || !tecs_entity_exists(world, parent)) return NULL;
    return tecs_find_children(world, parent);
}

int tecs_child_count(const tecs_world_t* world, tecs_entity_t parent) {
    const tecs_children_t* children = tecs_get_children(world, parent);
    return children ? children->count : 0;
}

bool tecs_is_ancestor_of(const tecs_world_t* world, tecs_entity_t ancestor, tecs_entity_t descendant) {
    if (!world || !tecs_entity_exists(world, ancestor) || !tecs_entity_exists(world, descendant)) return false;

    tecs_entity_t current = descendant;
    int depth = 0;
    const int max_depth = 256; /* Prevent infinite loops */

    while (current != 0 && depth < max_depth) {
        const tecs_parent_t* parent = (const tecs_parent_t*)tecs_get_const(world, current, PARENT_ID);
        if (!parent) break;

        current = parent->parent;
        if (current == ancestor) {
            return true;
        }
        depth++;
    }

    return false;
}

bool tecs_is_descendant_of(const tecs_world_t* world, tecs_entity_t descendant, tecs_entity_t ancestor) {
    return tecs_is_ancestor_of(world, ancestor, descendant);
}

int tecs_get_hierarchy_depth(const tecs_world_t* world, tecs_entity_t entity) {
    if (!world || !tecs_entity_exists(world, entity)) return 0;

    int depth = 0;
    tecs_entity_t current = entity;
    const int max_depth = 256;

    while (current != 0 && depth < max_depth) {
        const tecs_parent_t* parent = (const tecs_parent_t*)tecs_get_const(world, current, PARENT_ID);
        if (!parent) break;

        current = parent->parent;
        depth++;
    }

    return depth;
}

void tecs_traverse_children(tecs_world_t* world, tecs_entity_t parent,
                             tecs_hierarchy_visitor_t visitor, void* user_data, bool recursive) {
    if (!world || !tecs_entity_exists(world, parent) || !visitor) return;

    const tecs_children_t* children = tecs_get_children(world, parent);
    if (!children) return;

    /* Visit each child */
    for (int i = 0; i < children->count; i++) {
        tecs_entity_t child = children->entities[i];
        visitor(world, child, user_data);

        /* Recurse if requested */
        if (recursive) {
            tecs_traverse_children(world, child, visitor, user_data, true);
        }
    }
}

void tecs_traverse_ancestors(tecs_world_t* world, tecs_entity_t child,
                              tecs_hierarchy_visitor_t visitor, void* user_data) {
    if (!world || !tecs_entity_exists(world, child) || !visitor) return;

    tecs_entity_t current = child;
    int depth = 0;
    const int max_depth = 256;

    while (current != 0 && depth < max_depth) {
        const tecs_parent_t* parent = (const tecs_parent_t*)tecs_get_const(world, current, PARENT_ID);
        if (!parent) break;

        current = parent->parent;
        if (current != 0) {
            visitor(world, current, user_data);
        }
        depth++;
    }
}

#endif /* TINYECS_IMPLEMENTATION */

#ifdef __cplusplus
}
#endif

#endif /* TINYECS_H */
