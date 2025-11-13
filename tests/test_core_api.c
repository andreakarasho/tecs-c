/*
 * Test: Core ECS API
 * Comprehensive tests for all core TinyECS functionality
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
    float dx, dy;
} Velocity;

typedef struct {
    int value;
} Health;

typedef struct {
    char name[32];
} Name;

/* Tag component */
typedef struct {} Player;
typedef struct {} Enemy;

/* ========================================================================
 * World Management Tests
 * ======================================================================== */

static void test_world_new_free(void) {
    printf("Testing tecs_world_new() and tecs_world_free()...\n");
    
    tecs_world_t* world = tecs_world_new();
    assert(world != NULL);
    assert(tecs_world_entity_count(world) == 0);
    assert(tecs_world_tick(world) == 0);
    
    tecs_world_free(world);
    printf("  ✓ World creation and cleanup works\n");
}

static void test_world_update(void) {
    printf("Testing tecs_world_update()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_tick_t tick0 = tecs_world_tick(world);
    assert(tick0 == 0);
    
    tecs_world_update(world);
    tecs_tick_t tick1 = tecs_world_tick(world);
    assert(tick1 == 1);
    
    tecs_world_update(world);
    tecs_tick_t tick2 = tecs_world_tick(world);
    assert(tick2 == 2);
    
    printf("  ✓ World tick increments correctly: %llu -> %llu -> %llu\n", 
           (unsigned long long)tick0, (unsigned long long)tick1, (unsigned long long)tick2);
    
    tecs_world_free(world);
}

static void test_world_clear(void) {
    printf("Testing tecs_world_clear()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    
    /* Create some entities */
    for (int i = 0; i < 10; i++) {
        tecs_entity_t e = tecs_entity_new(world);
        Position pos = {(float)i, (float)i};
        tecs_set(world, e, pos_id, &pos, sizeof(Position));
    }
    
    assert(tecs_world_entity_count(world) == 10);
    
    tecs_world_clear(world);
    assert(tecs_world_entity_count(world) == 0);
    
    /* Should be able to create entities after clear */
    tecs_entity_t e = tecs_entity_new(world);
    assert(tecs_entity_exists(world, e));
    
    printf("  ✓ World clear removes all entities\n");
    
    tecs_world_free(world);
}

/* ========================================================================
 * Component Registration Tests
 * ======================================================================== */

static void test_register_component(void) {
    printf("Testing tecs_register_component()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t vel_id = tecs_register_component(world, "Velocity", sizeof(Velocity));
    tecs_component_id_t health_id = tecs_register_component(world, "Health", sizeof(Health));
    
    assert(pos_id != 0);
    assert(vel_id != 0);
    assert(health_id != 0);
    assert(pos_id != vel_id);
    assert(vel_id != health_id);
    
    printf("  ✓ Component registration returns unique IDs: %llu, %llu, %llu\n",
           (unsigned long long)pos_id, (unsigned long long)vel_id, (unsigned long long)health_id);
    
    tecs_world_free(world);
}

static void test_get_component_id(void) {
    printf("Testing tecs_get_component_id()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t found_id = tecs_get_component_id(world, "Position");
    
    assert(found_id == pos_id);
    
    tecs_component_id_t not_found = tecs_get_component_id(world, "NotRegistered");
    assert(not_found == 0);
    
    printf("  ✓ Component ID lookup works\n");
    
    tecs_world_free(world);
}

/* ========================================================================
 * Entity Management Tests
 * ======================================================================== */

static void test_entity_new(void) {
    printf("Testing tecs_entity_new()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_entity_t e1 = tecs_entity_new(world);
    tecs_entity_t e2 = tecs_entity_new(world);
    tecs_entity_t e3 = tecs_entity_new(world);
    
    assert(e1 != e2);
    assert(e2 != e3);
    assert(e1 != e3);
    assert(tecs_world_entity_count(world) == 3);
    
    printf("  ✓ Entity creation: %llu, %llu, %llu\n",
           (unsigned long long)e1, (unsigned long long)e2, (unsigned long long)e3);
    
    tecs_world_free(world);
}

static void test_entity_new_with_id(void) {
    printf("Testing tecs_entity_new_with_id()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_entity_t specific_id = 12345;
    tecs_entity_t e = tecs_entity_new_with_id(world, specific_id);
    
    assert(e == specific_id);
    assert(tecs_entity_exists(world, e));
    
    printf("  ✓ Entity created with specific ID: %llu\n", (unsigned long long)e);
    
    tecs_world_free(world);
}

static void test_entity_delete(void) {
    printf("Testing tecs_entity_delete()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_entity_t e1 = tecs_entity_new(world);
    tecs_entity_t e2 = tecs_entity_new(world);
    
    assert(tecs_entity_exists(world, e1));
    assert(tecs_entity_exists(world, e2));
    assert(tecs_world_entity_count(world) == 2);
    
    tecs_entity_delete(world, e1);
    
    assert(!tecs_entity_exists(world, e1));
    assert(tecs_entity_exists(world, e2));
    assert(tecs_world_entity_count(world) == 1);
    
    printf("  ✓ Entity deletion works\n");
    
    tecs_world_free(world);
}

static void test_entity_exists(void) {
    printf("Testing tecs_entity_exists()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_entity_t e = tecs_entity_new(world);
    assert(tecs_entity_exists(world, e));
    
    tecs_entity_delete(world, e);
    assert(!tecs_entity_exists(world, e));
    
    tecs_entity_t never_created = 99999;
    assert(!tecs_entity_exists(world, never_created));
    
    printf("  ✓ Entity existence checking works\n");
    
    tecs_world_free(world);
}

/* ========================================================================
 * Component Operations Tests
 * ======================================================================== */

static void test_tecs_set_get(void) {
    printf("Testing tecs_set() and tecs_get()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_entity_t e = tecs_entity_new(world);
    
    Position pos = {10.5f, 20.5f};
    tecs_set(world, e, pos_id, &pos, sizeof(Position));
    
    Position* retrieved = (Position*)tecs_get(world, e, pos_id);
    assert(retrieved != NULL);
    assert(retrieved->x == 10.5f);
    assert(retrieved->y == 20.5f);
    
    /* Modify through pointer */
    retrieved->x = 15.0f;
    Position* check = (Position*)tecs_get(world, e, pos_id);
    assert(check->x == 15.0f);
    
    printf("  ✓ Component set/get works\n");
    
    tecs_world_free(world);
}

static void test_tecs_has(void) {
    printf("Testing tecs_has()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t vel_id = tecs_register_component(world, "Velocity", sizeof(Velocity));
    
    tecs_entity_t e = tecs_entity_new(world);
    
    assert(!tecs_has(world, e, pos_id));
    assert(!tecs_has(world, e, vel_id));
    
    Position pos = {1.0f, 2.0f};
    tecs_set(world, e, pos_id, &pos, sizeof(Position));
    
    assert(tecs_has(world, e, pos_id));
    assert(!tecs_has(world, e, vel_id));
    
    printf("  ✓ Component presence checking works\n");
    
    tecs_world_free(world);
}

static void test_tecs_unset(void) {
    printf("Testing tecs_unset()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t vel_id = tecs_register_component(world, "Velocity", sizeof(Velocity));
    
    tecs_entity_t e = tecs_entity_new(world);
    
    Position pos = {10.0f, 20.0f};
    Velocity vel = {1.0f, 2.0f};
    tecs_set(world, e, pos_id, &pos, sizeof(Position));
    tecs_set(world, e, vel_id, &vel, sizeof(Velocity));
    
    assert(tecs_has(world, e, pos_id));
    assert(tecs_has(world, e, vel_id));
    
    tecs_unset(world, e, vel_id);
    
    assert(tecs_has(world, e, pos_id));
    assert(!tecs_has(world, e, vel_id));
    
    printf("  ✓ Component removal works\n");
    
    tecs_world_free(world);
}

static void test_tecs_mark_changed(void) {
    printf("Testing tecs_mark_changed()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_entity_t e = tecs_entity_new(world);
    
    Position pos = {10.0f, 20.0f};
    tecs_set(world, e, pos_id, &pos, sizeof(Position));
    
    tecs_tick_t tick_before = tecs_world_tick(world);
    
    tecs_world_update(world);
    tecs_mark_changed(world, e, pos_id);
    
    tecs_tick_t tick_after = tecs_world_tick(world);
    assert(tick_after > tick_before);
    
    printf("  ✓ Manual change detection works: tick %llu -> %llu\n",
           (unsigned long long)tick_before, (unsigned long long)tick_after);
    
    tecs_world_free(world);
}

/* ========================================================================
 * Query Tests
 * ======================================================================== */

static void test_query_basic(void) {
    printf("Testing basic query...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t vel_id = tecs_register_component(world, "Velocity", sizeof(Velocity));
    
    /* Create entities with different component combinations */
    for (int i = 0; i < 5; i++) {
        tecs_entity_t e = tecs_entity_new(world);
        Position pos = {(float)i, (float)(i * 2)};
        Velocity vel = {1.0f, 2.0f};
        tecs_set(world, e, pos_id, &pos, sizeof(Position));
        tecs_set(world, e, vel_id, &vel, sizeof(Velocity));
    }
    
    /* Create entities with only Position */
    for (int i = 0; i < 3; i++) {
        tecs_entity_t e = tecs_entity_new(world);
        Position pos = {100.0f + i, 200.0f + i};
        tecs_set(world, e, pos_id, &pos, sizeof(Position));
    }
    
    /* Query for entities with Position AND Velocity */
    tecs_query_t* query = tecs_query_new(world);
    tecs_query_with(query, pos_id);
    tecs_query_with(query, vel_id);
    tecs_query_build(query);
    
    tecs_query_iter_t* iter = tecs_query_iter(query);
    
    int count = 0;
    while (tecs_iter_next(iter)) {
        int iter_count = tecs_iter_count(iter);
        Position* positions = (Position*)tecs_iter_column(iter, 0);
        Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);
        
        for (int i = 0; i < iter_count; i++) {
            assert(positions != NULL);
            assert(velocities != NULL);
            count++;
        }
    }
    
    tecs_query_iter_free(iter);
    
    assert(count == 5);
    printf("  ✓ Query matched %d entities (expected 5)\n", count);
    
    tecs_query_free(query);
    tecs_world_free(world);
}

static void test_query_without(void) {
    printf("Testing query with WITHOUT filter...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t vel_id = tecs_register_component(world, "Velocity", sizeof(Velocity));
    
    /* Create 5 entities with both */
    for (int i = 0; i < 5; i++) {
        tecs_entity_t e = tecs_entity_new(world);
        Position pos = {(float)i, (float)i};
        Velocity vel = {1.0f, 1.0f};
        tecs_set(world, e, pos_id, &pos, sizeof(Position));
        tecs_set(world, e, vel_id, &vel, sizeof(Velocity));
    }
    
    /* Create 3 entities with only Position */
    for (int i = 0; i < 3; i++) {
        tecs_entity_t e = tecs_entity_new(world);
        Position pos = {100.0f, 100.0f};
        tecs_set(world, e, pos_id, &pos, sizeof(Position));
    }
    
    /* Query for Position WITHOUT Velocity */
    tecs_query_t* query = tecs_query_new(world);
    tecs_query_with(query, pos_id);
    tecs_query_without(query, vel_id);
    tecs_query_build(query);
    
    tecs_query_iter_t* iter = tecs_query_iter(query);
    
    int count = 0;
    while (tecs_iter_next(iter)) {
        count += tecs_iter_count(iter);
    }
    
    tecs_query_iter_free(iter);
    
    assert(count == 3);
    printf("  ✓ WITHOUT query matched %d entities (expected 3)\n", count);
    
    tecs_query_free(query);
    tecs_world_free(world);
}

static void test_query_changed(void) {
    printf("Testing query with CHANGED filter...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    
    /* Create entities */
    tecs_entity_t entities[5];
    for (int i = 0; i < 5; i++) {
        entities[i] = tecs_entity_new(world);
        Position pos = {(float)i, (float)i};
        tecs_set(world, entities[i], pos_id, &pos, sizeof(Position));
    }
    
    tecs_world_update(world);
    
    /* Mark only some as changed */
    tecs_mark_changed(world, entities[1], pos_id);
    tecs_mark_changed(world, entities[3], pos_id);
    
    /* Query for changed positions */
    tecs_query_t* query = tecs_query_new(world);
    tecs_query_with(query, pos_id);
    tecs_query_changed(query, pos_id);
    tecs_query_build(query);
    
    tecs_query_iter_t* iter = tecs_query_iter(query);
    
    int count = 0;
    while (tecs_iter_next(iter)) {
        count += tecs_iter_count(iter);
    }
    
    tecs_query_iter_free(iter);
    
    assert(count == 2);
    printf("  ✓ CHANGED query matched %d entities (expected 2)\n", count);
    
    tecs_query_free(query);
    tecs_world_free(world);
}

static void test_query_entities(void) {
    printf("Testing tecs_iter_entities()...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    
    tecs_entity_t created[3];
    for (int i = 0; i < 3; i++) {
        created[i] = tecs_entity_new(world);
        Position pos = {(float)i, (float)i};
        tecs_set(world, created[i], pos_id, &pos, sizeof(Position));
    }
    
    tecs_query_t* query = tecs_query_new(world);
    tecs_query_with(query, pos_id);
    tecs_query_build(query);
    
    tecs_query_iter_t* iter = tecs_query_iter(query);
    
    int found_count = 0;
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        
        for (int i = 0; i < count; i++) {
            int found = 0;
            for (int j = 0; j < 3; j++) {
                if (entities[i] == created[j]) {
                    found = 1;
                    break;
                }
            }
            assert(found);
            found_count++;
        }
    }
    
    tecs_query_iter_free(iter);
    
    assert(found_count == 3);
    printf("  ✓ Entity iteration returns correct entities\n");
    
    tecs_query_free(query);
    tecs_world_free(world);
}

/* ========================================================================
 * Tag Component Tests
 * ======================================================================== */

static void test_tag_components(void) {
    printf("Testing tag components (zero-size)...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t player_tag = tecs_register_component(world, "Player", 0);
    tecs_component_id_t enemy_tag = tecs_register_component(world, "Enemy", 0);
    
    /* Create player entity */
    tecs_entity_t player = tecs_entity_new(world);
    Position player_pos = {0.0f, 0.0f};
    tecs_set(world, player, pos_id, &player_pos, sizeof(Position));
    tecs_set(world, player, player_tag, NULL, 0);
    
    /* Create enemy entities */
    for (int i = 0; i < 3; i++) {
        tecs_entity_t enemy = tecs_entity_new(world);
        Position enemy_pos = {(float)(i * 10), 0.0f};
        tecs_set(world, enemy, pos_id, &enemy_pos, sizeof(Position));
        tecs_set(world, enemy, enemy_tag, NULL, 0);
    }
    
    /* Query for enemies */
    tecs_query_t* query = tecs_query_new(world);
    tecs_query_with(query, enemy_tag);
    tecs_query_build(query);
    
    tecs_query_iter_t* iter = tecs_query_iter(query);
    
    int enemy_count = 0;
    while (tecs_iter_next(iter)) {
        enemy_count += tecs_iter_count(iter);
    }
    
    tecs_query_iter_free(iter);
    
    assert(enemy_count == 3);
    printf("  ✓ Tag components work: found %d enemies\n", enemy_count);
    
    tecs_query_free(query);
    tecs_world_free(world);
}

/* ========================================================================
 * Stress Tests
 * ======================================================================== */

static void test_many_entities(void) {
    printf("Testing with many entities...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    
    const int COUNT = 10000;
    
    for (int i = 0; i < COUNT; i++) {
        tecs_entity_t e = tecs_entity_new(world);
        Position pos = {(float)i, (float)(i * 2)};
        tecs_set(world, e, pos_id, &pos, sizeof(Position));
    }
    
    assert(tecs_world_entity_count(world) == COUNT);
    
    /* Query all entities */
    tecs_query_t* query = tecs_query_new(world);
    tecs_query_with(query, pos_id);
    tecs_query_build(query);
    
    tecs_query_iter_t* iter = tecs_query_iter(query);
    
    int count = 0;
    while (tecs_iter_next(iter)) {
        count += tecs_iter_count(iter);
    }
    
    tecs_query_iter_free(iter);
    
    assert(count == COUNT);
    printf("  ✓ Created and queried %d entities successfully\n", COUNT);
    
    tecs_query_free(query);
    tecs_world_free(world);
}

static void test_archetype_transitions(void) {
    printf("Testing archetype transitions...\n");
    
    tecs_world_t* world = tecs_world_new();
    
    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t vel_id = tecs_register_component(world, "Velocity", sizeof(Velocity));
    tecs_component_id_t health_id = tecs_register_component(world, "Health", sizeof(Health));
    
    tecs_entity_t e = tecs_entity_new(world);
    
    /* Start with Position */
    Position pos = {10.0f, 20.0f};
    tecs_set(world, e, pos_id, &pos, sizeof(Position));
    assert(tecs_has(world, e, pos_id));
    
    /* Add Velocity - archetype change */
    Velocity vel = {1.0f, 2.0f};
    tecs_set(world, e, vel_id, &vel, sizeof(Velocity));
    assert(tecs_has(world, e, pos_id));
    assert(tecs_has(world, e, vel_id));
    
    /* Verify data persists */
    Position* p = (Position*)tecs_get(world, e, pos_id);
    assert(p->x == 10.0f && p->y == 20.0f);
    
    /* Add Health - another archetype change */
    Health health = {100};
    tecs_set(world, e, health_id, &health, sizeof(Health));
    assert(tecs_has(world, e, pos_id));
    assert(tecs_has(world, e, vel_id));
    assert(tecs_has(world, e, health_id));
    
    /* Verify all data persists */
    p = (Position*)tecs_get(world, e, pos_id);
    Velocity* v = (Velocity*)tecs_get(world, e, vel_id);
    Health* h = (Health*)tecs_get(world, e, health_id);
    assert(p->x == 10.0f && p->y == 20.0f);
    assert(v->dx == 1.0f && v->dy == 2.0f);
    assert(h->value == 100);
    
    /* Remove component - archetype change */
    tecs_unset(world, e, vel_id);
    assert(tecs_has(world, e, pos_id));
    assert(!tecs_has(world, e, vel_id));
    assert(tecs_has(world, e, health_id));
    
    printf("  ✓ Archetype transitions preserve component data\n");
    
    tecs_world_free(world);
}

/* ========================================================================
 * Main Test Runner
 * ======================================================================== */

int main(void) {
    printf("=== TinyECS Core API Tests ===\n\n");
    
    /* World Management */
    test_world_new_free();
    test_world_update();
    test_world_clear();
    
    /* Component Registration */
    test_register_component();
    test_get_component_id();
    
    /* Entity Management */
    test_entity_new();
    test_entity_new_with_id();
    test_entity_delete();
    test_entity_exists();
    
    /* Component Operations */
    test_tecs_set_get();
    test_tecs_has();
    test_tecs_unset();
    test_tecs_mark_changed();
    
    /* Queries */
    test_query_basic();
    test_query_without();
    test_query_changed();
    test_query_entities();
    
    /* Tag Components */
    test_tag_components();
    
    /* Stress Tests */
    test_many_entities();
    test_archetype_transitions();
    
    printf("\n=== All Core API Tests Passed ✓ ===\n");
    return 0;
}
