/*
 * TinyEcs C API Example
 *
 * Demonstrates the core ECS functionality:
 * - Component registration
 * - Entity creation and manipulation
 * - Query iteration
 * - Change detection
 */

#define TINYECS_IMPLEMENTATION
#include "tinyecs.h"

#include <stdio.h>
#include <math.h>

/* Define component types using macros */
TECS_DECLARE_COMPONENT(Position);
struct Position {
    float x, y;
};

TECS_DECLARE_COMPONENT(Velocity);
struct Velocity {
    float x, y;
};

TECS_DECLARE_COMPONENT(Health);
struct Health {
    float value;
};

TECS_DECLARE_COMPONENT(Player);
struct Player {
    /* Empty struct for tag component */
};

/* System: Apply velocity to position */
void move_system(tecs_world_t* world, float delta_time) {
    tecs_query_t* query = tecs_query_new(world);
    TECS_QUERY_WITH(query, Position);
    TECS_QUERY_WITH(query, Velocity);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_query_next(iter)) {
        int count = tecs_iter_count(iter);
        Position* positions = (Position*)tecs_iter_column(iter, 0);
        Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            positions[i].x += velocities[i].x * delta_time;
            positions[i].y += velocities[i].y * delta_time;
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* System: Print all entities with Position */
void print_positions(tecs_world_t* world) {
    tecs_query_t* query = tecs_query_new(world);
    TECS_QUERY_WITH(query, Position);
    tecs_query_build(query);

    printf("\nEntity positions:\n");
    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_query_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Position* positions = (Position*)tecs_iter_column(iter, 0);

        for (int i = 0; i < count; i++) {
            printf("  Entity %llu: (%.2f, %.2f)\n",
                   (unsigned long long)entities[i],
                   positions[i].x, positions[i].y);
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* System: Print players with health */
void print_player_health(tecs_world_t* world) {
    tecs_query_t* query = tecs_query_new(world);
    TECS_QUERY_WITH(query, Health);
    TECS_QUERY_WITH(query, Player);  /* Tag filter */
    tecs_query_build(query);

    printf("\nPlayer health:\n");
    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_query_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Health* healths = (Health*)tecs_iter_column(iter, 0);

        for (int i = 0; i < count; i++) {
            printf("  Player %llu: %.0f HP\n",
                   (unsigned long long)entities[i],
                   healths[i].value);
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* System: Detect changed positions */
void detect_changed_positions(tecs_world_t* world) {
    tecs_query_t* query = tecs_query_new(world);
    TECS_QUERY_CHANGED(query, Position);
    tecs_query_build(query);

    tecs_tick_t current_tick = tecs_world_tick(world);

    printf("\nChanged positions (tick %u):\n", current_tick);
    int changed_count = 0;

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_query_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Position* positions = (Position*)tecs_iter_column(iter, 0);
        tecs_tick_t* changed_ticks = tecs_iter_changed_ticks(iter, 0);

        for (int i = 0; i < count; i++) {
            /* Filter by changed tick */
            if (changed_ticks[i] == current_tick) {
                printf("  Entity %llu: (%.2f, %.2f) changed at tick %u\n",
                       (unsigned long long)entities[i],
                       positions[i].x, positions[i].y,
                       changed_ticks[i]);
                changed_count++;
            }
        }
    }

    if (changed_count == 0) {
        printf("  (none)\n");
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

int main(void) {
    printf("=== TinyEcs C API Example ===\n\n");

    /* Create world */
    tecs_world_t* world = tecs_world_new();

    /* Register components */
    TECS_COMPONENT_REGISTER(world, Position);
    TECS_COMPONENT_REGISTER(world, Velocity);
    TECS_COMPONENT_REGISTER(world, Health);
    TECS_COMPONENT_REGISTER(world, Player);  /* Tag component */

    printf("Registered components:\n");
    printf("  Position (ID: %llu, size: %zu)\n", (unsigned long long)Position_id, sizeof(Position));
    printf("  Velocity (ID: %llu, size: %zu)\n", (unsigned long long)Velocity_id, sizeof(Velocity));
    printf("  Health (ID: %llu, size: %zu)\n", (unsigned long long)Health_id, sizeof(Health));
    printf("  Player (ID: %llu, tag)\n", (unsigned long long)Player_id);

    /* Create entities */
    printf("\n--- Creating entities ---\n");

    /* Player entity with position, velocity, and health */
    tecs_entity_t player = tecs_entity_new(world);
    Position player_pos = {100.0f, 100.0f};
    Velocity player_vel = {10.0f, 5.0f};
    Health player_health = {100.0f};

    TECS_SET(world, player, Position, player_pos);
    TECS_SET(world, player, Velocity, player_vel);
    TECS_SET(world, player, Health, player_health);
    TECS_ADD_TAG(world, player, Player);

    printf("Created player entity %llu\n", (unsigned long long)player);

    /* Enemy entity with position and velocity (no Player tag) */
    tecs_entity_t enemy = tecs_entity_new(world);
    Position enemy_pos = {200.0f, 150.0f};
    Velocity enemy_vel = {-5.0f, 3.0f};

    TECS_SET(world, enemy, Position, enemy_pos);
    TECS_SET(world, enemy, Velocity, enemy_vel);

    printf("Created enemy entity %llu\n", (unsigned long long)enemy);

    /* Static object with position only */
    tecs_entity_t statue = tecs_entity_new(world);
    Position statue_pos = {0.0f, 0.0f};
    TECS_SET(world, statue, Position, statue_pos);

    printf("Created statue entity %llu\n", (unsigned long long)statue);

    printf("\nTotal entities: %d\n", tecs_world_entity_count(world));

    /* Initial state */
    print_positions(world);
    print_player_health(world);

    /* Simulate frames */
    printf("\n--- Simulating 3 frames ---\n");

    for (int frame = 0; frame < 3; frame++) {
        printf("\n=== Frame %d (Tick %u) ===\n", frame + 1, tecs_world_tick(world));

        /* Run movement system */
        move_system(world, 0.1f);

        /* Increment world tick (simulates frame boundary) */
        tecs_world_update(world);

        /* Print results */
        print_positions(world);
        detect_changed_positions(world);
    }

    /* Test component removal */
    printf("\n--- Removing velocity from player ---\n");
    TECS_UNSET(world, player, Velocity);

    printf("Has velocity: %s\n", TECS_HAS(world, player, Velocity) ? "true" : "false");

    /* Run one more frame */
    printf("\n=== Frame 4 (after removal) ===\n");
    move_system(world, 0.1f);
    tecs_world_update(world);
    print_positions(world);

    /* Test entity deletion */
    printf("\n--- Deleting enemy entity ---\n");
    tecs_entity_delete(world, enemy);
    printf("Total entities: %d\n", tecs_world_entity_count(world));
    print_positions(world);

    /* Test change detection with manual marking */
    printf("\n--- Manual change detection ---\n");
    printf("Manually modifying player position without triggering change...\n");
    Position* pos = TECS_GET(world, player, Position);
    pos->x = 999.0f;
    pos->y = 888.0f;

    tecs_world_update(world);
    detect_changed_positions(world);  /* Should not detect change */

    printf("\nNow marking as changed...\n");
    TECS_MARK_CHANGED(world, player, Position);
    tecs_world_update(world);
    detect_changed_positions(world);  /* Should detect change */

    /* Cleanup */
    printf("\n--- Cleanup ---\n");
    tecs_world_free(world);
    printf("World freed successfully.\n");

    printf("\n=== Example completed successfully ===\n");
    return 0;
}
