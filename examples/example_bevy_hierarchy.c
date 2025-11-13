/*
 * TinyEcs.Bevy Hierarchy Example
 *
 * Demonstrates entity parent-child relationships with the Bevy layer:
 * - Spawning entities with hierarchy
 * - Parent-child transforms
 * - Querying parent/children components
 * - Destroying hierarchies
 * - System ordering
 */

#define TINYECS_IMPLEMENTATION
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs.h"
#include "tinyecs_bevy.h"

#include <stdio.h>
#include <math.h>
#include <string.h>

/* ============================================================================
 * Components
 * ========================================================================= */

TECS_DECLARE_COMPONENT(Transform);
typedef struct Transform {
    float x, y;
    float rotation;
} Transform;

TECS_DECLARE_COMPONENT(Name);
typedef struct Name {
    char value[64];
} Name;

TECS_DECLARE_COMPONENT(Turret);
typedef struct Turret {
    float rotation_speed;
} Turret;

TECS_DECLARE_COMPONENT(Shield);
typedef struct Shield {
    float rotation_speed;
    float radius;
} Shield;

/* ============================================================================
 * Resources
 * ========================================================================= */

typedef struct {
    float time;
    float delta_time;
} TimeResource;
tecs_component_id_t TimeResource_id;

/* ============================================================================
 * Systems
 * ========================================================================= */

/* Update turrets relative to their parent */
static void turret_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time) return;

    

    /* Query all turrets */
    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Transform);
    TECS_QUERY_WITH(query, Turret);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform* transforms = (Transform*)tecs_iter_column(iter, 0);
        Turret* turrets = (Turret*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            /* Rotate turret */
            transforms[i].rotation += turrets[i].rotation_speed * time->delta_time;

            /* Get parent and update absolute position */
            tecs_entity_t parent_id = tecs_get_parent(ctx->world, entities[i]);
            if (parent_id != TECS_ENTITY_NULL) {
                Transform* parent_transform = TECS_GET(ctx->world, parent_id, Transform);
                if (parent_transform) {
                    /* Turret position relative to parent */
                    float offset_x = cosf(parent_transform->rotation) * 15.0f;
                    float offset_y = sinf(parent_transform->rotation) * 15.0f;

                    transforms[i].x = parent_transform->x + offset_x;
                    transforms[i].y = parent_transform->y + offset_y;
                }
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Update shields relative to their parent */
static void shield_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time) return;

    

    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Transform);
    TECS_QUERY_WITH(query, Shield);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform* transforms = (Transform*)tecs_iter_column(iter, 0);
        Shield* shields = (Shield*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            /* Rotate shield */
            transforms[i].rotation += shields[i].rotation_speed * time->delta_time;

            /* Position around parent */
            tecs_entity_t parent_id = tecs_get_parent(ctx->world, entities[i]);
            if (parent_id != TECS_ENTITY_NULL) {
                Transform* parent_transform = TECS_GET(ctx->world, parent_id, Transform);
                if (parent_transform) {
                    float angle = transforms[i].rotation;
                    transforms[i].x = parent_transform->x + cosf(angle) * shields[i].radius;
                    transforms[i].y = parent_transform->y + sinf(angle) * shields[i].radius;
                }
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Move parent entities */
static void movement_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time) return;

    

    /* Query all entities that have transforms but NO parent (root entities only) */
    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Transform);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform* transforms = (Transform*)tecs_iter_column(iter, 0);

        for (int i = 0; i < count; i++) {
            /* Only move root entities (no parent) */
            if (!tecs_has_parent(ctx->world, entities[i])) {
                /* Move in circle */
                float speed = 1.0f;
                transforms[i].x = cosf(time->time * speed) * 100.0f;
                transforms[i].y = sinf(time->time * speed) * 100.0f;
                transforms[i].rotation = time->time * 0.5f;
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Debug print system */
static void debug_print_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time) return;

    /* Only print every 30 frames */
    static int frame_counter = 0;
    frame_counter++;
    if (frame_counter % 30 != 0) return;

    printf("\n=== Frame %.0f (Time: %.2fs) ===\n", time->time / time->delta_time, time->time);

    

    /* Query all entities with transforms */
    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Transform);
    TECS_QUERY_WITH(query, Name);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform* transforms = (Transform*)tecs_iter_column(iter, 0);
        Name* names = (Name*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            tecs_entity_t parent_id = tecs_get_parent(ctx->world, entities[i]);
            int child_count = tecs_child_count(ctx->world, entities[i]);

            if (parent_id == TECS_ENTITY_NULL) {
                printf("%-20s [Root] Pos:(%.1f, %.1f) Rot:%.2f  Children:%d\n",
                       names[i].value,
                       transforms[i].x,
                       transforms[i].y,
                       transforms[i].rotation,
                       child_count);
            } else {
                Name* parent_name = TECS_GET(ctx->world, parent_id, Name);
                printf("  %-18s [Child of %s] Pos:(%.1f, %.1f) Rot:%.2f\n",
                       names[i].value,
                       parent_name ? parent_name->value : "Unknown",
                       transforms[i].x,
                       transforms[i].y,
                       transforms[i].rotation);
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);

    /* Print hierarchy tree */
    printf("\nHierarchy Tree:\n");
    query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Transform);
    TECS_QUERY_WITH(query, Name);
    tecs_query_build(query);

    iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Name* names = (Name*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            if (!tecs_has_parent(ctx->world, entities[i])) {
                printf("%s\n", names[i].value);

                /* Print children */
                const tecs_children_t* children = tecs_get_children(ctx->world, entities[i]);
                if (children) {
                    for (int j = 0; j < children->count; j++) {
                        Name* child_name = TECS_GET(ctx->world, children->entities[j], Name);
                        printf("  └─ %s\n", child_name ? child_name->value : "Unknown");
                    }
                }
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Time update system */
static void time_update_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    TimeResource* time = TBEVY_CTX_GET_RESOURCE_MUT(ctx, TimeResource);
    if (time) {
        time->delta_time = 0.016f; /* 60 FPS */
        time->time += time->delta_time;
    }
}

/* Startup system - spawn initial entities with hierarchy */
static void startup_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    printf("\n=== Spawning Entities with Hierarchy ===\n");

    /* Spawn Ship 1 with turrets and shield */
    tbevy_entity_commands_t ship1_ec = tbevy_commands_spawn(ctx->commands);
    Transform ship1_transform = { 0.0f, 0.0f, 0.0f };
    Name ship1_name;
    snprintf(ship1_name.value, sizeof(ship1_name.value), "Ship-1");

    TBEVY_ENTITY_INSERT(&ship1_ec, ship1_transform, Transform);
    TBEVY_ENTITY_INSERT(&ship1_ec, ship1_name, Name);

    tecs_entity_t ship1_id = tbevy_entity_id(&ship1_ec);
    printf("Spawned %s (entity %llu)\n", ship1_name.value, (unsigned long long)ship1_id);

    /* Spawn turrets as children */
    for (int i = 0; i < 3; i++) {
        tbevy_entity_commands_t turret_ec = tbevy_commands_spawn(ctx->commands);
        Transform turret_transform = { 0.0f, 0.0f, (float)i * 2.0f };
        Turret turret = { 2.0f + (float)i * 0.5f };
        Name turret_name;
        snprintf(turret_name.value, sizeof(turret_name.value), "Ship-1-Turret-%d", i + 1);

        TBEVY_ENTITY_INSERT(&turret_ec, turret_transform, Transform);
        TBEVY_ENTITY_INSERT(&turret_ec, turret, Turret);
        TBEVY_ENTITY_INSERT(&turret_ec, turret_name, Name);

        tecs_entity_t turret_id = tbevy_entity_id(&turret_ec);

        /* Add as child - must apply commands first */
        tbevy_commands_apply(ctx->commands);
        tecs_add_child(ctx->world, ship1_id, turret_id);

        printf("  Added %s (entity %llu) as child\n", turret_name.value, (unsigned long long)turret_id);
    }

    /* Spawn shield as child */
    tbevy_entity_commands_t shield_ec = tbevy_commands_spawn(ctx->commands);
    Transform shield_transform = { 0.0f, 0.0f, 0.0f };
    Shield shield = { 3.0f, 30.0f };
    Name shield_name;
    snprintf(shield_name.value, sizeof(shield_name.value), "Ship-1-Shield");

    TBEVY_ENTITY_INSERT(&shield_ec, shield_transform, Transform);
    TBEVY_ENTITY_INSERT(&shield_ec, shield, Shield);
    TBEVY_ENTITY_INSERT(&shield_ec, shield_name, Name);

    tecs_entity_t shield_id = tbevy_entity_id(&shield_ec);

    tbevy_commands_apply(ctx->commands);
    tecs_add_child(ctx->world, ship1_id, shield_id);

    printf("  Added %s (entity %llu) as child\n", shield_name.value, (unsigned long long)shield_id);

    /* Spawn Ship 2 with different configuration */
    tbevy_entity_commands_t ship2_ec = tbevy_commands_spawn(ctx->commands);
    Transform ship2_transform = { 200.0f, 0.0f, 0.0f };
    Name ship2_name;
    snprintf(ship2_name.value, sizeof(ship2_name.value), "Ship-2");

    TBEVY_ENTITY_INSERT(&ship2_ec, ship2_transform, Transform);
    TBEVY_ENTITY_INSERT(&ship2_ec, ship2_name, Name);

    tecs_entity_t ship2_id = tbevy_entity_id(&ship2_ec);
    printf("\nSpawned %s (entity %llu)\n", ship2_name.value, (unsigned long long)ship2_id);

    /* Spawn dual turrets */
    for (int i = 0; i < 2; i++) {
        tbevy_entity_commands_t turret_ec = tbevy_commands_spawn(ctx->commands);
        Transform turret_transform = { 0.0f, 0.0f, (float)i * 3.0f };
        Turret turret = { -1.5f - (float)i * 0.3f }; /* Negative = counter-rotation */
        Name turret_name;
        snprintf(turret_name.value, sizeof(turret_name.value), "Ship-2-Turret-%d", i + 1);

        TBEVY_ENTITY_INSERT(&turret_ec, turret_transform, Transform);
        TBEVY_ENTITY_INSERT(&turret_ec, turret, Turret);
        TBEVY_ENTITY_INSERT(&turret_ec, turret_name, Name);

        tecs_entity_t turret_id = tbevy_entity_id(&turret_ec);

        tbevy_commands_apply(ctx->commands);
        tecs_add_child(ctx->world, ship2_id, turret_id);

        printf("  Added %s (entity %llu) as child\n", turret_name.value, (unsigned long long)turret_id);
    }

    printf("\n=== Entity Spawning Complete ===\n");
}

/* ============================================================================
 * Main
 * ========================================================================= */

static bool should_quit(tbevy_app_t* app) {
    const TimeResource* time = TBEVY_GET_RESOURCE(app, TimeResource);
    return time && time->time >= 5.0f; /* Run for 5 seconds */
}

int main(void) {
    printf("╔════════════════════════════════════════════╗\n");
    printf("║  TinyEcs.Bevy Hierarchy Example           ║\n");
    printf("║                                            ║\n");
    printf("║  Demonstrates:                             ║\n");
    printf("║  - Parent-child entity relationships      ║\n");
    printf("║  - Hierarchy queries                       ║\n");
    printf("║  - Relative transform updates              ║\n");
    printf("║  - System ordering                         ║\n");
    printf("╚════════════════════════════════════════════╝\n");

    /* Create app */
    tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_SINGLE);
    tecs_world_t* world = tbevy_app_world(app);

    /* Register components */
    TECS_COMPONENT_REGISTER(world, Transform);
    TECS_COMPONENT_REGISTER(world, Name);
    TECS_COMPONENT_REGISTER(world, Turret);
    TECS_COMPONENT_REGISTER(world, Shield);

    /* Register resources */
    TimeResource_id = TBEVY_REGISTER_RESOURCE(TimeResource);

    TimeResource time_init = { 0.0f, 0.016f };
    tbevy_app_insert_resource(app, TimeResource_id, &time_init, sizeof(time_init));

    /* Add startup system */
    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, startup_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_STARTUP)
        )
    );

    /* Add update systems with proper ordering */
    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_system_label(
                tbevy_app_add_system(app, time_update_system, NULL),
                "time_update"
            ),
            tbevy_stage_default(TBEVY_STAGE_FIRST)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_system_after(
                tbevy_system_label(
                    tbevy_app_add_system(app, movement_system, NULL),
                    "movement"
                ),
                "time_update"
            ),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_system_after(
                tbevy_app_add_system(app, turret_system, NULL),
                "movement"
            ),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_system_after(
                tbevy_app_add_system(app, shield_system, NULL),
                "movement"
            ),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, debug_print_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_LAST)
        )
    );

    /* Run */
    printf("\n=== Starting Game Loop ===\n");
    tbevy_app_run_startup(app);
    tbevy_app_run(app, should_quit);

    /* Cleanup */
    printf("\n\n=== Shutting Down ===\n");
    tbevy_app_free(app);

    printf("\n=== Example Completed Successfully ===\n");
    return 0;
}
