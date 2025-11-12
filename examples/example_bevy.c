/*
 * TinyEcs.Bevy C API Example
 *
 * Demonstrates the Bevy-inspired scheduling layer:
 * - Application framework with stages
 * - System scheduling with dependencies
 * - Resources (Res/ResMut pattern)
 * - Commands for deferred entity operations
 * - Observers for component lifecycle events
 * - Events for decoupled communication
 * - State machines with OnEnter/OnExit
 */

#define TINYECS_IMPLEMENTATION
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs.h"
#include "tinyecs_bevy.h"

#include <stdio.h>
#include <stdlib.h>

/* ============================================================================
 * Components
 * ========================================================================= */

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

TECS_DECLARE_COMPONENT(Name);
struct Name {
    char name[32];
};

TECS_DECLARE_COMPONENT(Player);
struct Player {
    /* Empty tag */
};

/* ============================================================================
 * Resources
 * ========================================================================= */

typedef struct {
    float delta_time;
    uint32_t frame_count;
} TimeResource;

typedef struct {
    int player_score;
    int enemies_defeated;
} GameStats;

/* Resource IDs */
static uint64_t TimeResource_id;
static uint64_t GameStats_id;

/* ============================================================================
 * Events
 * ========================================================================= */

typedef struct {
    tecs_entity_t entity;
    float damage_amount;
} DamageEvent;

typedef struct {
    tecs_entity_t entity;
    int points;
} ScoreEvent;

/* Event IDs */
static uint64_t DamageEvent_id;
static uint64_t ScoreEvent_id;

/* ============================================================================
 * Game States
 * ========================================================================= */

typedef enum {
    GAME_STATE_MENU,
    GAME_STATE_PLAYING,
    GAME_STATE_PAUSED,
    GAME_STATE_GAME_OVER
} GameState;

static uint64_t GameState_id;

/* ============================================================================
 * Systems
 * ========================================================================= */

/* Startup system - runs once */
static void setup_world(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    printf("\n[Startup] Setting up world...\n");

    /* Spawn player */
    tbevy_entity_commands_t player = tbevy_commands_spawn(ctx->commands);

    Position player_pos = {100.0f, 100.0f};
    Velocity player_vel = {10.0f, 5.0f};
    Health player_health = {100.0f};
    Name player_name;
    strcpy(player_name.name, "Hero");
    Player player_tag = {};

    TBEVY_ENTITY_INSERT(&player, player_pos, Position);
    TBEVY_ENTITY_INSERT(&player, player_vel, Velocity);
    TBEVY_ENTITY_INSERT(&player, player_health, Health);
    TBEVY_ENTITY_INSERT(&player, player_name, Name);
    TBEVY_ENTITY_INSERT(&player, player_tag, Player);

    printf("  Spawned player entity %llu\n", (unsigned long long)tbevy_entity_id(&player));

    /* Spawn enemies */
    for (int i = 0; i < 3; i++) {
        tbevy_entity_commands_t enemy = tbevy_commands_spawn(ctx->commands);

        Position enemy_pos = {(float)(200 + i * 50), (float)(150 + i * 30)};
        Velocity enemy_vel = {-5.0f, 3.0f};
        Health enemy_health = {50.0f};
        Name enemy_name;
        snprintf(enemy_name.name, sizeof(enemy_name.name), "Enemy%d", i + 1);

        TBEVY_ENTITY_INSERT(&enemy, enemy_pos, Position);
        TBEVY_ENTITY_INSERT(&enemy, enemy_vel, Velocity);
        TBEVY_ENTITY_INSERT(&enemy, enemy_health, Health);
        TBEVY_ENTITY_INSERT(&enemy, enemy_name, Name);

        printf("  Spawned enemy entity %llu\n", (unsigned long long)tbevy_entity_id(&enemy));
    }

    printf("[Startup] World setup complete!\n");
}

/* Movement system */
static void movement_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    /* Get resources */
    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time) return;

    /* Query entities with Position and Velocity */
    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Position);
    TECS_QUERY_WITH(query, Velocity);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    int moved_count = 0;

    while (tecs_query_next(iter)) {
        int count = tecs_iter_count(iter);
        Position* positions = (Position*)tecs_iter_column(iter, 0);
        Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            positions[i].x += velocities[i].x * time->delta_time;
            positions[i].y += velocities[i].y * time->delta_time;
            moved_count++;
        }
    }

    if (moved_count > 0 && time->frame_count % 60 == 0) {
        printf("[Movement] Moved %d entities (frame %u)\n", moved_count, time->frame_count);
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Damage processing system */
static void handle_damage_events(tbevy_app_t* app, const void* event_data, void* user_data) {
    (void)user_data;
    const DamageEvent* dmg = (const DamageEvent*)event_data;

    /* Get health component */
    Health* health = TECS_GET(tbevy_app_world(app), dmg->entity, Health);
    if (!health) return;

    health->value -= dmg->damage_amount;

    printf("[Damage] Entity %llu took %.1f damage (health: %.1f)\n",
           (unsigned long long)dmg->entity, dmg->damage_amount, health->value);

    /* Send score event if entity died */
    if (health->value <= 0) {
        ScoreEvent score_event = {dmg->entity, 100};
        TBEVY_SEND_EVENT(app, ScoreEvent_id, score_event);
    }
}

static void damage_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;
    tbevy_app_read_events(ctx->_app, DamageEvent_id, handle_damage_events, NULL);
}

/* Score processing system */
static void handle_score_events(tbevy_app_t* app, const void* event_data, void* user_data) {
    (void)user_data;
    const ScoreEvent* score = (const ScoreEvent*)event_data;

    GameStats* stats = TBEVY_GET_RESOURCE_MUT(app, GameStats);
    if (!stats) return;

    stats->player_score += score->points;
    stats->enemies_defeated++;

    printf("[Score] +%d points! Total score: %d (enemies defeated: %d)\n",
           score->points, stats->player_score, stats->enemies_defeated);
}

static void score_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;
    tbevy_app_read_events(ctx->_app, ScoreEvent_id, handle_score_events, NULL);
}

/* Debug print system */
static void debug_print_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time || time->frame_count % 120 != 0) return;

    printf("\n[Debug] === Frame %u ===\n", time->frame_count);

    /* Print player status */
    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Player);
    TECS_QUERY_WITH(query, Position);
    TECS_QUERY_WITH(query, Health);
    TECS_QUERY_WITH(query, Name);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_query_next(iter)) {
        int count = tecs_iter_count(iter);
        Position* positions = (Position*)tecs_iter_column(iter, 0);
        Health* healths = (Health*)tecs_iter_column(iter, 1);
        Name* names = (Name*)tecs_iter_column(iter, 2);

        for (int i = 0; i < count; i++) {
            printf("  Player '%s': pos(%.1f, %.1f) health(%.1f)\n",
                   names[i].name, positions[i].x, positions[i].y, healths[i].value);
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);

    /* Print stats */
    const GameStats* stats = TBEVY_CTX_GET_RESOURCE(ctx, GameStats);
    if (stats) {
        printf("  Score: %d | Enemies defeated: %d\n",
               stats->player_score, stats->enemies_defeated);
    }

    printf("\n");
}

/* Update time resource */
static void update_time_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    TimeResource* time = TBEVY_CTX_GET_RESOURCE_MUT(ctx, TimeResource);
    if (!time) return;

    time->frame_count++;
    time->delta_time = 0.016f;  /* 60 FPS */
}

/* State transition systems */
static void on_enter_playing(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)ctx;
    (void)user_data;
    printf("\n>>> Entered PLAYING state <<<\n\n");
}

static void on_exit_playing(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)ctx;
    (void)user_data;
    printf("\n>>> Exited PLAYING state <<<\n\n");
}

static void on_enter_paused(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)ctx;
    (void)user_data;
    printf("\n>>> Game PAUSED <<<\n\n");
}

static void on_enter_game_over(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const GameStats* stats = TBEVY_CTX_GET_RESOURCE(ctx, GameStats);
    printf("\n>>> GAME OVER <<<\n");
    if (stats) {
        printf("Final Score: %d\n", stats->player_score);
        printf("Enemies Defeated: %d\n", stats->enemies_defeated);
    }
    printf("\n");
}

/* ============================================================================
 * Observers
 * ========================================================================= */

static void on_health_changed(tbevy_app_t* app, tecs_entity_t entity,
                               tecs_component_id_t component_id,
                               const void* component_data, void* user_data) {
    (void)app;
    (void)component_id;
    (void)user_data;

    const Health* health = (const Health*)component_data;
    if (health && health->value < 20.0f) {
        printf("[Observer] WARNING: Entity %llu low health (%.1f)!\n",
               (unsigned long long)entity, health->value);
    }
}

/* ============================================================================
 * Main
 * ========================================================================= */

static bool should_quit(tbevy_app_t* app) {
    const TimeResource* time = TBEVY_GET_RESOURCE(app, TimeResource);
    if (!time) return false;

    /* Run for 300 frames (~5 seconds at 60 FPS) */
    if (time->frame_count >= 300) {
        printf("\n[Main] Simulation complete (300 frames)\n");
        return true;
    }

    /* Simulate state transitions */
    if (time->frame_count == 60) {
        printf("\n[Main] Transitioning to PLAYING state...\n");
        tbevy_app_set_state(app, GameState_id, GAME_STATE_PLAYING);
    } else if (time->frame_count == 120) {
        printf("\n[Main] Sending damage events...\n");
        /* Send some damage events */
        DamageEvent dmg1 = {1, 25.0f};
        DamageEvent dmg2 = {2, 60.0f};
        TBEVY_SEND_EVENT(app, DamageEvent_id, dmg1);
        TBEVY_SEND_EVENT(app, DamageEvent_id, dmg2);
    } else if (time->frame_count == 180) {
        printf("\n[Main] Pausing game...\n");
        tbevy_app_set_state(app, GameState_id, GAME_STATE_PAUSED);
    } else if (time->frame_count == 240) {
        printf("\n[Main] Resuming game...\n");
        tbevy_app_set_state(app, GameState_id, GAME_STATE_PLAYING);
    } else if (time->frame_count == 270) {
        printf("\n[Main] Game over!\n");
        tbevy_app_set_state(app, GameState_id, GAME_STATE_GAME_OVER);
    }

    return false;
}

int main(void) {
    printf("=== TinyEcs.Bevy C API Example ===\n");

    /* Create app with auto threading mode */
    tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_AUTO);

    /* Register component types */
    TECS_COMPONENT_REGISTER(tbevy_app_world(app), Position);
    TECS_COMPONENT_REGISTER(tbevy_app_world(app), Velocity);
    TECS_COMPONENT_REGISTER(tbevy_app_world(app), Health);
    TECS_COMPONENT_REGISTER(tbevy_app_world(app), Name);
    TECS_COMPONENT_REGISTER(tbevy_app_world(app), Player);

    /* Register resource types */
    TimeResource_id = TBEVY_REGISTER_RESOURCE(TimeResource);
    GameStats_id = TBEVY_REGISTER_RESOURCE(GameStats);

    /* Register event types */
    DamageEvent_id = TBEVY_REGISTER_EVENT(DamageEvent);
    ScoreEvent_id = TBEVY_REGISTER_EVENT(ScoreEvent);

    /* Register state type */
    GameState_id = TBEVY_REGISTER_RESOURCE(GameState);

    /* Insert initial resources */
    TimeResource time = {0.016f, 0};
    GameStats stats = {0, 0};
    TBEVY_INSERT_RESOURCE(app, TimeResource_id, time);
    TBEVY_INSERT_RESOURCE(app, GameStats_id, stats);

    /* Add state machine */
    tbevy_app_add_state(app, GameState_id, GAME_STATE_MENU);

    /* Add startup system */
    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, setup_world, NULL),
            tbevy_stage_default(TBEVY_STAGE_STARTUP)
        )
    );

    /* Add state transition systems */
    tbevy_system_build(
        tbevy_app_add_system_on_enter(app, GameState_id, GAME_STATE_PLAYING,
                                       on_enter_playing, NULL)
    );

    tbevy_system_build(
        tbevy_app_add_system_on_exit(app, GameState_id, GAME_STATE_PLAYING,
                                      on_exit_playing, NULL)
    );

    tbevy_system_build(
        tbevy_app_add_system_on_enter(app, GameState_id, GAME_STATE_PAUSED,
                                       on_enter_paused, NULL)
    );

    tbevy_system_build(
        tbevy_app_add_system_on_enter(app, GameState_id, GAME_STATE_GAME_OVER,
                                       on_enter_game_over, NULL)
    );

    /* Add regular systems with labels and dependencies */
    tbevy_system_build(
        tbevy_system_label(
            tbevy_system_in_stage(
                tbevy_app_add_system(app, update_time_system, NULL),
                tbevy_stage_default(TBEVY_STAGE_FIRST)
            ),
            "update_time"
        )
    );

    tbevy_system_build(
        tbevy_system_after(
            tbevy_system_label(
                tbevy_system_in_stage(
                    tbevy_app_add_system(app, movement_system, NULL),
                    tbevy_stage_default(TBEVY_STAGE_UPDATE)
                ),
                "movement"
            ),
            "update_time"
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, damage_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_after(
            tbevy_system_in_stage(
                tbevy_app_add_system(app, score_system, NULL),
                tbevy_stage_default(TBEVY_STAGE_POST_UPDATE)
            ),
            "movement"
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, debug_print_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_LAST)
        )
    );

    /* Add observer for health changes */
    TBEVY_ADD_OBSERVER_INSERT(app, Health, on_health_changed, NULL);

    /* Run the application */
    printf("\n[Main] Starting game loop...\n");
    tbevy_app_run(app, should_quit);

    /* Cleanup */
    printf("\n[Main] Shutting down...\n");
    tbevy_app_free(app);

    printf("\n=== Example completed successfully ===\n");
    return 0;
}
