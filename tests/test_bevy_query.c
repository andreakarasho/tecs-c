#define TINYECS_IMPLEMENTATION
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs.h"
#include "tinyecs_bevy.h"
#include <stdio.h>

typedef struct { float x, y; } Position;
typedef struct { float x, y; } Velocity;

int main(void) {
    tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_SINGLE);
    tecs_world_t* world = tbevy_app_world(app);
    
    tecs_component_id_t Position_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t Velocity_id = tecs_register_component(world, "Velocity", sizeof(Velocity));
    
    // Spawn 100 entities directly
    printf("Spawning 100 entities...\n");
    for (int i = 0; i < 100; i++) {
        tecs_entity_t entity = tecs_entity_new(world);
        Position pos = {(float)i, (float)i};
        Velocity vel = {1.0f, 1.0f};
        tecs_set(world, entity, Position_id, &pos, sizeof(Position));
        tecs_set(world, entity, Velocity_id, &vel, sizeof(Velocity));
    }
    
    printf("World entity count: %d\n", tecs_world_entity_count(world));
    
    // Query entities
    printf("Querying entities...\n");
    tecs_query_t* query = tecs_query_new(world);
    tecs_query_with(query, Position_id);
    tecs_query_with(query, Velocity_id);
    tecs_query_build(query);
    
    int total_entities = 0;
    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_query_next(iter)) {
        int count = tecs_iter_count(iter);
        printf("  Chunk: %d entities\n", count);
        total_entities += count;
        
        Position* positions = (Position*)tecs_iter_column(iter, 0);
        Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);
        
        // Print first entity
        if (count > 0) {
            printf("    First entity: pos=(%.1f, %.1f), vel=(%.1f, %.1f)\n",
                   positions[0].x, positions[0].y, velocities[0].x, velocities[0].y);
        }
    }
    tecs_query_iter_free(iter);
    tecs_query_free(query);
    
    printf("Total entities found by query: %d\n", total_entities);
    
    tbevy_app_free(app);
    return 0;
}
