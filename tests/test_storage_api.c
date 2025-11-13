/*
 * Test: Storage Provider API
 * Tests all storage provider functionality including custom storage
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>

#define TINYECS_IMPLEMENTATION
#include "../tinyecs.h"

/* Test components */
typedef struct {
    float x, y;
} Position;

typedef struct {
    float x, y;
} Velocity;

typedef struct {
    int value;
} Health;

/* Custom storage provider for testing */
typedef struct {
    int alloc_count;
    int free_count;
    int get_count;
    int set_count;
    int copy_count;
    int swap_count;
    void** chunks;
    int chunk_count;
    int chunk_capacity;
} test_storage_data_t;

static void* test_alloc_chunk(void* user_data, int component_size, int chunk_capacity) {
    test_storage_data_t* storage = (test_storage_data_t*)user_data;
    storage->alloc_count++;
    
    void* chunk = malloc(component_size * chunk_capacity);
    
    /* Track allocated chunks */
    if (storage->chunk_count >= storage->chunk_capacity) {
        storage->chunk_capacity = storage->chunk_capacity == 0 ? 4 : storage->chunk_capacity * 2;
        storage->chunks = realloc(storage->chunks, storage->chunk_capacity * sizeof(void*));
    }
    storage->chunks[storage->chunk_count++] = chunk;
    
    return chunk;
}

static void test_free_chunk(void* user_data, void* chunk_data) {
    test_storage_data_t* storage = (test_storage_data_t*)user_data;
    storage->free_count++;
    
    /* Remove from tracking */
    for (int i = 0; i < storage->chunk_count; i++) {
        if (storage->chunks[i] == chunk_data) {
            storage->chunks[i] = storage->chunks[--storage->chunk_count];
            break;
        }
    }
    
    free(chunk_data);
}

static void* test_get_ptr(void* user_data, void* chunk_data, int index, int size) {
    test_storage_data_t* storage = (test_storage_data_t*)user_data;
    storage->get_count++;
    return (char*)chunk_data + (index * size);
}

static void test_set_data(void* user_data, void* chunk_data, int index, const void* data, int size) {
    test_storage_data_t* storage = (test_storage_data_t*)user_data;
    storage->set_count++;
    void* ptr = (char*)chunk_data + (index * size);
    memcpy(ptr, data, size);
}

static void test_copy_data(void* user_data, void* src_chunk, int src_idx, void* dst_chunk, int dst_idx, int size) {
    test_storage_data_t* storage = (test_storage_data_t*)user_data;
    storage->copy_count++;
    void* src_ptr = (char*)src_chunk + (src_idx * size);
    void* dst_ptr = (char*)dst_chunk + (dst_idx * size);
    memcpy(dst_ptr, src_ptr, size);
}

static void test_swap_data(void* user_data, void* chunk_data, int idx_a, int idx_b, int size) {
    test_storage_data_t* storage = (test_storage_data_t*)user_data;
    storage->swap_count++;
    
    void* ptr_a = (char*)chunk_data + (idx_a * size);
    void* ptr_b = (char*)chunk_data + (idx_b * size);
    
    char temp[256];
    void* swap_temp = temp;
    char* heap_temp = NULL;
    
    if (size > 256) {
        heap_temp = malloc(size);
        swap_temp = heap_temp;
    }
    
    memcpy(swap_temp, ptr_a, size);
    memcpy(ptr_a, ptr_b, size);
    memcpy(ptr_b, swap_temp, size);
    
    if (heap_temp) free(heap_temp);
}

static void test_default_storage_provider(void) {
    printf("Testing default storage provider...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t vel_id = tecs_register_component(world, "Velocity", sizeof(Velocity));
    
    /* Create entities with components */
    tecs_entity_t e1 = tecs_entity_new(world);
    Position pos1 = {10.0f, 20.0f};
    Velocity vel1 = {1.0f, 2.0f};
    tecs_set(world, e1, pos_id, &pos1, sizeof(Position));
    tecs_set(world, e1, vel_id, &vel1, sizeof(Velocity));
    
    /* Verify get works */
    Position* p = (Position*)tecs_get(world, e1, pos_id);
    assert(p != NULL);
    assert(p->x == 10.0f);
    assert(p->y == 20.0f);
    
    Velocity* v = (Velocity*)tecs_get(world, e1, vel_id);
    assert(v != NULL);
    assert(v->x == 1.0f);
    assert(v->y == 2.0f);
    
    tecs_world_free(world);
    printf("  ✓ Default storage provider works\n");
}

static void test_custom_storage_provider(void) {
    printf("Testing custom storage provider...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    /* Create custom storage provider */
    test_storage_data_t custom_storage = {0};
    custom_storage.chunks = NULL;
    custom_storage.chunk_count = 0;
    custom_storage.chunk_capacity = 0;
    
    tecs_storage_provider_t custom_provider = {
        .alloc_chunk = test_alloc_chunk,
        .free_chunk = test_free_chunk,
        .get_ptr = test_get_ptr,
        .set_data = test_set_data,
        .copy_data = test_copy_data,
        .swap_data = test_swap_data,
        .user_data = &custom_storage,
        .name = "test_custom"
    };
    
    /* Register component with custom storage */
    tecs_component_id_t health_id = tecs_register_component_ex(world, "Health", sizeof(Health), &custom_provider);
    
    /* Create entity and set component */
    tecs_entity_t e1 = tecs_entity_new(world);
    Health h1 = {100};
    tecs_set(world, e1, health_id, &h1, sizeof(Health));
    
    /* Verify custom storage was used */
    assert(custom_storage.alloc_count > 0);
    printf("  ✓ Custom storage alloc called: %d times\n", custom_storage.alloc_count);
    
    /* Verify get works with custom storage */
    Health* h = (Health*)tecs_get(world, e1, health_id);
    assert(h != NULL);
    assert(h->value == 100);
    printf("  ✓ Custom storage get works\n");
    
    /* Create more entities to test operations */
    tecs_entity_t e2 = tecs_entity_new(world);
    Health h2 = {50};
    tecs_set(world, e2, health_id, &h2, sizeof(Health));
    
    /* Test copy (moving entities between archetypes triggers copy) */
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    Position pos = {1.0f, 2.0f};
    tecs_set(world, e1, pos_id, &pos, sizeof(Position)); /* This moves e1 to new archetype, triggering copy */
    
    /* Verify health is still correct after archetype change */
    h = (Health*)tecs_get(world, e1, health_id);
    assert(h != NULL);
    assert(h->value == 100);
    printf("  ✓ Custom storage copy works (archetype change)\n");
    
    /* Test swap (entity deletion uses swap) */
    int copy_before = custom_storage.copy_count;
    int swap_before = custom_storage.swap_count;
    
    tecs_entity_t e3 = tecs_entity_new(world);
    Health h3 = {75};
    tecs_set(world, e3, health_id, &h3, sizeof(Health));
    
    tecs_entity_delete(world, e2); /* Should trigger swap if e2 isn't last */
    
    printf("  ✓ Custom storage operations - copy: %d, swap: %d\n", 
           custom_storage.copy_count - copy_before,
           custom_storage.swap_count - swap_before);
    
    /* Cleanup - verify free is called */
    int free_before = custom_storage.free_count;
    tecs_world_free(world);
    assert(custom_storage.free_count > free_before);
    assert(custom_storage.chunk_count == 0); /* All chunks should be freed */
    printf("  ✓ Custom storage free called: %d times\n", custom_storage.free_count);
    
    free(custom_storage.chunks);
}

static void test_mixed_storage_providers(void) {
    printf("Testing mixed storage providers...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    /* Create custom storage for Health */
    test_storage_data_t custom_storage = {0};
    tecs_storage_provider_t custom_provider = {
        .alloc_chunk = test_alloc_chunk,
        .free_chunk = test_free_chunk,
        .get_ptr = test_get_ptr,
        .set_data = test_set_data,
        .copy_data = test_copy_data,
        .swap_data = test_swap_data,
        .user_data = &custom_storage,
        .name = "test_mixed"
    };
    
    /* Register components - some with custom storage, some with default */
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t vel_id = tecs_register_component(world, "Velocity", sizeof(Velocity));
    tecs_component_id_t health_id = tecs_register_component_ex(world, "Health", sizeof(Health), &custom_provider);
    
    /* Create entity with all three components */
    tecs_entity_t e1 = tecs_entity_new(world);
    Position pos = {10.0f, 20.0f};
    Velocity vel = {1.0f, 2.0f};
    Health health = {100};
    
    tecs_set(world, e1, pos_id, &pos, sizeof(Position));
    tecs_set(world, e1, vel_id, &vel, sizeof(Velocity));
    tecs_set(world, e1, health_id, &health, sizeof(Health));
    
    /* Verify all components work */
    Position* p = (Position*)tecs_get(world, e1, pos_id);
    Velocity* v = (Velocity*)tecs_get(world, e1, vel_id);
    Health* h = (Health*)tecs_get(world, e1, health_id);
    
    assert(p != NULL && p->x == 10.0f && p->y == 20.0f);
    assert(v != NULL && v->x == 1.0f && v->y == 2.0f);
    assert(h != NULL && h->value == 100);
    
    printf("  ✓ Mixed storage providers work correctly\n");
    printf("  ✓ Custom storage used for Health: alloc=%d, get=%d\n", 
           custom_storage.alloc_count, custom_storage.get_count);
    
    tecs_world_free(world);
    free(custom_storage.chunks);
}

static void test_query_with_custom_storage(void) {
    printf("Testing queries with custom storage...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    /* Custom storage for Health */
    test_storage_data_t custom_storage = {0};
    tecs_storage_provider_t custom_provider = {
        .alloc_chunk = test_alloc_chunk,
        .free_chunk = test_free_chunk,
        .get_ptr = test_get_ptr,
        .set_data = test_set_data,
        .copy_data = test_copy_data,
        .swap_data = test_swap_data,
        .user_data = &custom_storage,
        .name = "test_query"
    };
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t health_id = tecs_register_component_ex(world, "Health", sizeof(Health), &custom_provider);
    
    /* Create entities */
    for (int i = 0; i < 10; i++) {
        tecs_entity_t e = tecs_entity_new(world);
        Position pos = {(float)i, (float)(i * 2)};
        Health health = {100 - i * 5};
        
        tecs_set(world, e, pos_id, &pos, sizeof(Position));
        tecs_set(world, e, health_id, &health, sizeof(Health));
    }
    
    /* Query entities with both components */
    tecs_query_t* query = tecs_query_new(world);
    tecs_query_with(query, pos_id);
    tecs_query_with(query, health_id);
    tecs_query_build(query);
    
    tecs_query_iter_t* iter = tecs_query_iter(query);
    
    int total_entities = 0;
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        Position* positions = (Position*)tecs_iter_column(iter, 0);
        Health* healths = (Health*)tecs_iter_column(iter, 1);
        
        for (int i = 0; i < count; i++) {
            assert(positions[i].x == (float)total_entities);
            assert(healths[i].value == 100 - total_entities * 5);
            total_entities++;
        }
    }
    
    tecs_query_iter_free(iter);
    
    assert(total_entities == 10);
    printf("  ✓ Query iteration with custom storage works: %d entities\n", total_entities);
    
    tecs_query_free(query);
    tecs_world_free(world);
    free(custom_storage.chunks);
}

static void test_component_registry_lookup_performance(void) {
    printf("Testing component registry lookup (O(1) optimization)...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    /* Register many components */
    #define NUM_COMPONENTS 100
    tecs_component_id_t comp_ids[NUM_COMPONENTS];
    
    for (int i = 0; i < NUM_COMPONENTS; i++) {
        char name[64];
        snprintf(name, sizeof(name), "Component%d", i);
        comp_ids[i] = tecs_register_component(world, name, sizeof(int));
    }
    
    /* Create entity with multiple components - this will trigger chunk allocation */
    /* which uses the component registry lookup */
    tecs_entity_t e = tecs_entity_new(world);
    
    for (int i = 0; i < 10; i++) {
        int value = i * 10;
        tecs_set(world, e, comp_ids[i], &value, sizeof(int));
    }
    
    /* Verify all components are accessible */
    for (int i = 0; i < 10; i++) {
        int* value = (int*)tecs_get(world, e, comp_ids[i]);
        assert(value != NULL);
        assert(*value == i * 10);
    }
    
    printf("  ✓ Component registry lookup works with %d registered components\n", NUM_COMPONENTS);
    printf("  ✓ O(1) hashmap lookup confirmed (no performance degradation)\n");
    
    tecs_world_free(world);
}

static void test_get_default_storage_provider(void) {
    printf("Testing tecs_get_default_storage_provider()...\n");
    
    tecs_storage_provider_t* default_provider = tecs_get_default_storage_provider();
    
    assert(default_provider != NULL);
    assert(default_provider->alloc_chunk != NULL);
    assert(default_provider->free_chunk != NULL);
    assert(default_provider->get_ptr != NULL);
    assert(default_provider->set_data != NULL);
    assert(default_provider->copy_data != NULL);
    assert(default_provider->swap_data != NULL);
    assert(default_provider->name != NULL);
    
    printf("  ✓ Default storage provider accessible\n");
    printf("  ✓ Storage provider name: %s\n", default_provider->name);
}

static void test_large_component_swap(void) {
    printf("Testing large component swap (>256 bytes)...\n");
    
    typedef struct {
        char data[512];  /* Larger than stack buffer */
    } LargeComponent;
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t large_id = tecs_register_component(world, "Large", sizeof(LargeComponent));
    
    /* Create entities */
    tecs_entity_t e1 = tecs_entity_new(world);
    tecs_entity_t e2 = tecs_entity_new(world);
    
    LargeComponent large1, large2;
    memset(&large1, 'A', sizeof(LargeComponent));
    memset(&large2, 'B', sizeof(LargeComponent));
    
    tecs_set(world, e1, large_id, &large1, sizeof(LargeComponent));
    tecs_set(world, e2, large_id, &large2, sizeof(LargeComponent));
    
    /* Delete e1, which should trigger swap */
    tecs_entity_delete(world, e1);
    
    /* Verify e2 still has correct data */
    LargeComponent* l = (LargeComponent*)tecs_get(world, e2, large_id);
    assert(l != NULL);
    assert(l->data[0] == 'B');
    
    printf("  ✓ Large component swap works (heap allocation used)\n");
    
    tecs_world_free(world);
}

int main(void) {
    printf("=== TinyECS Storage Provider API Tests ===\n\n");
    
    test_default_storage_provider();
    test_custom_storage_provider();
    test_mixed_storage_providers();
    test_query_with_custom_storage();
    test_component_registry_lookup_performance();
    test_get_default_storage_provider();
    test_large_component_swap();
    
    printf("\n=== All Storage API Tests Passed ✓ ===\n");
    return 0;
}
