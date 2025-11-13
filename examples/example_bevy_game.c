/*
 * TinyEcs.Bevy Comprehensive Game Example
 *
 * A feature-complete space shooter demonstrating:
 * - Entity hierarchy (ships with turrets/shields)
 * - State management (Menu, Playing, Paused, GameOver)
 * - Event system (collision, damage, score)
 * - Observers (damage reactions, death handlers)
 * - System ordering and stages
 * - Component bundles
 * - Change detection
 * - Multiple queries with filters
 * - Resources and time management
 */

#define TINYECS_IMPLEMENTATION
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs.h"
#include "tinyecs_bevy.h"

#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <string.h>

/* ============================================================================
 * Components
 * ========================================================================= */

TECS_DECLARE_COMPONENT(Transform);
typedef struct Transform {
    float x, y;
    float rotation;
    float scale;
} Transform;

TECS_DECLARE_COMPONENT(Velocity);
typedef struct Velocity {
    float x, y;
    float angular;
} Velocity;

TECS_DECLARE_COMPONENT(Health);
typedef struct Health {
    float current;
    float max;
    float shield;
} Health;

TECS_DECLARE_COMPONENT(Weapon);
typedef struct Weapon {
    float fire_rate;
    float damage;
    float last_fired;
} Weapon;

TECS_DECLARE_COMPONENT(Sprite);
typedef struct Sprite {
    char symbol;
    int layer;
} Sprite;

TECS_DECLARE_COMPONENT(Name);
typedef struct Name {
    char value[32];
} Name;

/* Tag components */
TECS_DECLARE_COMPONENT(Player);
typedef struct Player { int _unused; } Player;

TECS_DECLARE_COMPONENT(Enemy);
typedef struct Enemy { int _unused; } Enemy;

TECS_DECLARE_COMPONENT(Bullet);
typedef struct Bullet {
    tecs_entity_t owner;
    float lifetime;
} Bullet;

TECS_DECLARE_COMPONENT(Turret);
typedef struct Turret {
    float rotation_offset;
    float rotation_speed;
} Turret;

TECS_DECLARE_COMPONENT(Shield);
typedef struct Shield {
    float radius;
    float rotation;
} Shield;

TECS_DECLARE_COMPONENT(Collider);
typedef struct Collider {
    float radius;
} Collider;

/* ============================================================================
 * Game States
 * ========================================================================= */

typedef enum {
    GAME_STATE_MENU,
    GAME_STATE_PLAYING,
    GAME_STATE_PAUSED,
    GAME_STATE_GAME_OVER
} GameState;

/* ============================================================================
 * Resources
 * ========================================================================= */

typedef struct {
    float time;
    float delta_time;
    uint32_t frame;
} TimeResource;
tecs_component_id_t TimeResource_id;

typedef struct {
    int score;
    int enemies_killed;
    int bullets_fired;
    int hits_taken;
} GameStats;
tecs_component_id_t GameStats_id;

typedef struct {
    float spawn_timer;
    float spawn_interval;
    int wave_number;
    int enemies_this_wave;
} SpawnManager;
tecs_component_id_t SpawnManager_id;

typedef struct {
    float bounds_x;
    float bounds_y;
} WorldBounds;
tecs_component_id_t WorldBounds_id;

/* ============================================================================
 * Events
 * ========================================================================= */

typedef struct {
    tecs_entity_t entity;
    tecs_entity_t attacker;
    float damage;
} DamageEvent;
tecs_component_id_t DamageEvent_id;

typedef struct {
    tecs_entity_t entity1;
    tecs_entity_t entity2;
} CollisionEvent;
tecs_component_id_t CollisionEvent_id;

typedef struct {
    tecs_entity_t entity;
    int points;
} ScoreEvent;
tecs_component_id_t ScoreEvent_id;

typedef struct {
    tecs_entity_t entity;
} DeathEvent;
tecs_component_id_t DeathEvent_id;

/* ============================================================================
 * Component Bundles
 * ========================================================================= */

typedef struct {
    Transform transform;
    Velocity velocity;
    Sprite sprite;
    Collider collider;
} SpriteBundle;

typedef struct {
    Transform transform;
    Velocity velocity;
    Health health;
    Weapon weapon;
    Sprite sprite;
    Collider collider;
    Player player;
    Name name;
} PlayerBundle;

typedef struct {
    Transform transform;
    Velocity velocity;
    Health health;
    Sprite sprite;
    Collider collider;
    Enemy enemy;
    Name name;
} EnemyBundle;

/* ============================================================================
 * System Parameter Helpers
 * ========================================================================= */

typedef struct {
    tbevy_commands_t* commands;
    tbevy_app_t* app;
} SystemContext;

/* ============================================================================
 * Game Logic Systems
 * ========================================================================= */

/* Movement system - applies velocity to transform */
static void movement_system(tbevy_app_t* app, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_GET_RESOURCE(app, TimeResource);
    if (!time) return;

    tecs_query_t* query = tecs_query_new(tbevy_app_world(app));
    TECS_QUERY_WITH(query, Transform);
    TECS_QUERY_WITH(query, Velocity);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        Transform* transforms = (Transform*)tecs_iter_column(iter, 0);
        Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            transforms[i].x += velocities[i].x * time->delta_time;
            transforms[i].y += velocities[i].y * time->delta_time;
            transforms[i].rotation += velocities[i].angular * time->delta_time;
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Player input system */
static void player_input_system(tbevy_app_t* app, void* user_data) {
    tbevy_commands_t* commands = (tbevy_commands_t*)user_data;

    const TimeResource* time = TBEVY_GET_RESOURCE(app, TimeResource);
    if (!time) return;

    tecs_query_t* query = tecs_query_new(tbevy_app_world(app));
    TECS_QUERY_WITH(query, Transform);
    TECS_QUERY_WITH(query, Velocity);
    TECS_QUERY_WITH(query, Weapon);
    TECS_QUERY_WITH(query, Player);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform* transforms = (Transform*)tecs_iter_column(iter, 0);
        Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);
        Weapon* weapons = (Weapon*)tecs_iter_column(iter, 2);

        for (int i = 0; i < count; i++) {
            /* Simulate input (in real game, read from keyboard) */
            float speed = 100.0f;
            velocities[i].x = (time->frame % 120 < 60) ? speed : -speed;
            velocities[i].y = sinf(time->time * 2.0f) * 50.0f;

            /* Fire weapon */
            if (time->time - weapons[i].last_fired > 1.0f / weapons[i].fire_rate) {
                weapons[i].last_fired = time->time;

                /* Spawn bullet */
                tbevy_entity_commands_t bullet_ec = tbevy_commands_spawn(commands);

                Transform bullet_transform = {
                    transforms[i].x + 10.0f,
                    transforms[i].y,
                    0.0f,
                    1.0f
                };
                Velocity bullet_velocity = { 300.0f, 0.0f, 0.0f };
                Sprite bullet_sprite = { '-', 3 };
                Collider bullet_collider = { 2.0f };
                Bullet bullet_tag = { entities[i], 2.0f };
                Name bullet_name;
                snprintf(bullet_name.value, sizeof(bullet_name.value), "PlayerBullet");

                TBEVY_ENTITY_INSERT(&bullet_ec, bullet_transform, Transform);
                TBEVY_ENTITY_INSERT(&bullet_ec, bullet_velocity, Velocity);
                TBEVY_ENTITY_INSERT(&bullet_ec, bullet_sprite, Sprite);
                TBEVY_ENTITY_INSERT(&bullet_ec, bullet_collider, Collider);
                TBEVY_ENTITY_INSERT(&bullet_ec, bullet_tag, Bullet);
                TBEVY_ENTITY_INSERT(&bullet_ec, bullet_name, Name);

                /* Update stats */
                GameStats* stats = TBEVY_GET_RESOURCE_MUT(app, GameStats);
                if (stats) stats->bullets_fired++;
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Enemy AI system */
static void enemy_ai_system(tbevy_app_t* app, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_GET_RESOURCE(app, TimeResource);
    if (!time) return;

    /* Find player position */
    tecs_query_t* player_query = tecs_query_new(tbevy_app_world(app));
    TECS_QUERY_WITH(player_query, Transform);
    TECS_QUERY_WITH(player_query, Player);
    tecs_query_build(player_query);

    float player_x = 0.0f, player_y = 0.0f;
    bool player_found = false;

    tecs_query_iter_t* player_iter = tecs_query_iter(player_query);
    if (tecs_iter_next(player_iter)) {
        Transform* player_transforms = (Transform*)tecs_iter_column(player_iter, 0);
        player_x = player_transforms[0].x;
        player_y = player_transforms[0].y;
        player_found = true;
    }
    tecs_query_iter_free(player_iter);
    tecs_query_free(player_query);

    if (!player_found) return;

    /* Update enemy behavior */
    tecs_query_t* enemy_query = tecs_query_new(tbevy_app_world(app));
    TECS_QUERY_WITH(enemy_query, Transform);
    TECS_QUERY_WITH(enemy_query, Velocity);
    TECS_QUERY_WITH(enemy_query, Enemy);
    tecs_query_build(enemy_query);

    tecs_query_iter_t* iter = tecs_query_iter(enemy_query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        Transform* transforms = (Transform*)tecs_iter_column(iter, 0);
        Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            /* Move towards player */
            float dx = player_x - transforms[i].x;
            float dy = player_y - transforms[i].y;
            float dist = sqrtf(dx * dx + dy * dy);

            if (dist > 0.001f) {
                float speed = 50.0f;
                velocities[i].x = (dx / dist) * speed;
                velocities[i].y = (dy / dist) * speed;
            }

            /* Rotate to face player */
            velocities[i].angular = sinf(time->time * 3.0f) * 2.0f;
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(enemy_query);
}

/* Collision detection system */
static void collision_system(tbevy_app_t* app, void* user_data) {
    tbevy_commands_t* commands = (tbevy_commands_t*)user_data;

    tecs_query_t* query = tecs_query_new(tbevy_app_world(app));
    TECS_QUERY_WITH(query, Transform);
    TECS_QUERY_WITH(query, Collider);
    tecs_query_build(query);

    /* Collect all colliders */
    typedef struct { tecs_entity_t entity; Transform transform; Collider collider; } ColliderData;
    ColliderData colliders[256];
    int collider_count = 0;

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter) && collider_count < 256) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform* transforms = (Transform*)tecs_iter_column(iter, 0);
        Collider* colliders_data = (Collider*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count && collider_count < 256; i++) {
            colliders[collider_count++] = (ColliderData){
                entities[i], transforms[i], colliders_data[i]
            };
        }
    }
    tecs_query_iter_free(iter);
    tecs_query_free(query);

    /* Check all pairs */
    for (int i = 0; i < collider_count; i++) {
        for (int j = i + 1; j < collider_count; j++) {
            float dx = colliders[i].transform.x - colliders[j].transform.x;
            float dy = colliders[i].transform.y - colliders[j].transform.y;
            float dist_sq = dx * dx + dy * dy;
            float radius_sum = colliders[i].collider.radius + colliders[j].collider.radius;

            if (dist_sq < radius_sum * radius_sum) {
                /* Collision detected - emit event */
                CollisionEvent event = { colliders[i].entity, colliders[j].entity };
                tbevy_commands_emit_event(commands, CollisionEvent_id, &event, sizeof(event));
            }
        }
    }
}

/* Handle collision events */
static void handle_collision_events(tbevy_app_t* app, const void* event_data, void* user_data) {
    tbevy_commands_t* commands = (tbevy_commands_t*)user_data;
    const CollisionEvent* collision = (const CollisionEvent*)event_data;

    tecs_world_t* world = tbevy_app_world(app);

    /* Check if bullet hit enemy */
    Bullet* bullet1 = TECS_GET(world, collision->entity1, Bullet);
    Enemy* enemy2 = TECS_GET(world, collision->entity2, Enemy);

    Bullet* bullet2 = TECS_GET(world, collision->entity2, Bullet);
    Enemy* enemy1 = TECS_GET(world, collision->entity1, Enemy);

    if (bullet1 && enemy2) {
        /* Bullet 1 hit enemy 2 */
        DamageEvent dmg = { collision->entity2, bullet1->owner, 25.0f };
        tbevy_commands_emit_event(commands, DamageEvent_id, &dmg, sizeof(dmg));

        /* Destroy bullet */
        tbevy_commands_despawn(commands, collision->entity1);
    } else if (bullet2 && enemy1) {
        /* Bullet 2 hit enemy 1 */
        DamageEvent dmg = { collision->entity1, bullet2->owner, 25.0f };
        tbevy_commands_emit_event(commands, DamageEvent_id, &dmg, sizeof(dmg));

        /* Destroy bullet */
        tbevy_commands_despawn(commands, collision->entity2);
    }

    /* Check if enemy hit player */
    Player* player1 = TECS_GET(world, collision->entity1, Player);
    Player* player2 = TECS_GET(world, collision->entity2, Player);

    if ((enemy1 && player2) || (enemy2 && player1)) {
        tecs_entity_t player_entity = player1 ? collision->entity1 : collision->entity2;
        tecs_entity_t enemy_entity = enemy1 ? collision->entity1 : collision->entity2;

        DamageEvent dmg = { player_entity, enemy_entity, 10.0f };
        tbevy_commands_emit_event(commands, DamageEvent_id, &dmg, sizeof(dmg));
    }
}

/* Handle damage events */
static void handle_damage_events(tbevy_app_t* app, const void* event_data, void* user_data) {
    tbevy_commands_t* commands = (tbevy_commands_t*)user_data;
    const DamageEvent* dmg = (const DamageEvent*)event_data;

    Health* health = TECS_GET(tbevy_app_world(app), dmg->entity, Health);
    if (!health) return;

    /* Apply damage to shield first, then health */
    if (health->shield > 0.0f) {
        health->shield -= dmg->damage;
        if (health->shield < 0.0f) {
            health->current += health->shield;
            health->shield = 0.0f;
        }
    } else {
        health->current -= dmg->damage;
    }

    Name* name = TECS_GET(tbevy_app_world(app), dmg->entity, Name);
    printf("[Damage] %s took %.1f damage (HP: %.1f/%.1f, Shield: %.1f)\n",
           name ? name->value : "Entity",
           dmg->damage,
           health->current,
           health->max,
           health->shield);

    /* Check if dead */
    if (health->current <= 0.0f) {
        DeathEvent death = { dmg->entity };
        tbevy_commands_emit_event(commands, DeathEvent_id, &death, sizeof(death));
    }

    /* Update stats if player was hit */
    Player* player = TECS_GET(tbevy_app_world(app), dmg->entity, Player);
    if (player) {
        GameStats* stats = TBEVY_GET_RESOURCE_MUT(app, GameStats);
        if (stats) stats->hits_taken++;
    }
}

/* Handle death events */
static void handle_death_events(tbevy_app_t* app, const void* event_data, void* user_data) {
    tbevy_commands_t* commands = (tbevy_commands_t*)user_data;
    const DeathEvent* death = (const DeathEvent*)event_data;

    tecs_world_t* world = tbevy_app_world(app);
    Name* name = TECS_GET(world, death->entity, Name);
    printf("[Death] %s destroyed!\n", name ? name->value : "Entity");

    /* Award points if enemy died */
    Enemy* enemy = TECS_GET(world, death->entity, Enemy);
    if (enemy) {
        ScoreEvent score = { death->entity, 100 };
        tbevy_commands_emit_event(commands, ScoreEvent_id, &score, sizeof(score));

        GameStats* stats = TBEVY_GET_RESOURCE_MUT(app, GameStats);
        if (stats) stats->enemies_killed++;
    }

    /* Check if player died -> game over */
    Player* player = TECS_GET(world, death->entity, Player);
    if (player) {
        printf("\n=== GAME OVER ===\n");
        tbevy_commands_set_state(commands, (int)GAME_STATE_GAME_OVER);
    }

    /* Destroy entity and all children */
    tecs_remove_all_children(world, death->entity);
    tbevy_commands_despawn(commands, death->entity);
}

/* Handle score events */
static void handle_score_events(tbevy_app_t* app, const void* event_data, void* user_data) {
    (void)user_data;
    const ScoreEvent* score = (const ScoreEvent*)event_data;

    GameStats* stats = TBEVY_GET_RESOURCE_MUT(app, GameStats);
    if (stats) {
        stats->score += score->points;
        printf("[Score] +%d points! Total: %d\n", score->points, stats->score);
    }
}

/* Enemy spawning system */
static void enemy_spawn_system(tbevy_app_t* app, void* user_data) {
    tbevy_commands_t* commands = (tbevy_commands_t*)user_data;

    const TimeResource* time = TBEVY_GET_RESOURCE(app, TimeResource);
    SpawnManager* spawner = TBEVY_GET_RESOURCE_MUT(app, SpawnManager);
    if (!time || !spawner) return;

    spawner->spawn_timer += time->delta_time;

    if (spawner->spawn_timer >= spawner->spawn_interval) {
        spawner->spawn_timer = 0.0f;

        /* Spawn enemy with turrets and shield (hierarchy example) */
        tbevy_entity_commands_t enemy_ec = tbevy_commands_spawn(commands);

        float enemy_x = 600.0f;
        float enemy_y = ((float)(rand() % 400) - 200.0f);

        Transform enemy_transform = { enemy_x, enemy_y, 0.0f, 1.0f };
        Velocity enemy_velocity = { -30.0f, 0.0f, 1.0f };
        Health enemy_health = { 100.0f, 100.0f, 50.0f };
        Sprite enemy_sprite = { 'E', 2 };
        Collider enemy_collider = { 15.0f };
        Enemy enemy_tag = { 0 };
        Name enemy_name;
        snprintf(enemy_name.value, sizeof(enemy_name.value), "Enemy%d", spawner->wave_number);

        TBEVY_ENTITY_INSERT(&enemy_ec, enemy_transform, Transform);
        TBEVY_ENTITY_INSERT(&enemy_ec, enemy_velocity, Velocity);
        TBEVY_ENTITY_INSERT(&enemy_ec, enemy_health, Health);
        TBEVY_ENTITY_INSERT(&enemy_ec, enemy_sprite, Sprite);
        TBEVY_ENTITY_INSERT(&enemy_ec, enemy_collider, Collider);
        TBEVY_ENTITY_INSERT(&enemy_ec, enemy_tag, Enemy);
        TBEVY_ENTITY_INSERT(&enemy_ec, enemy_name, Name);

        tecs_entity_t enemy_id = tbevy_entity_id(&enemy_ec);

        /* Add turret as child */
        tbevy_entity_commands_t turret_ec = tbevy_commands_spawn(commands);
        Transform turret_transform = { 0.0f, 10.0f, 0.0f, 0.8f };
        Sprite turret_sprite = { 't', 3 };
        Turret turret_tag = { 0.0f, 3.0f };
        Name turret_name;
        snprintf(turret_name.value, sizeof(turret_name.value), "Turret");

        TBEVY_ENTITY_INSERT(&turret_ec, turret_transform, Transform);
        TBEVY_ENTITY_INSERT(&turret_ec, turret_sprite, Sprite);
        TBEVY_ENTITY_INSERT(&turret_ec, turret_tag, Turret);
        TBEVY_ENTITY_INSERT(&turret_ec, turret_name, Name);

        tecs_entity_t turret_id = tbevy_entity_id(&turret_ec);

        /* Add shield as child */
        tbevy_entity_commands_t shield_ec = tbevy_commands_spawn(commands);
        Transform shield_transform = { 0.0f, 0.0f, 0.0f, 1.2f };
        Sprite shield_sprite = { 'o', 1 };
        Shield shield_tag = { 20.0f, 0.0f };
        Name shield_name;
        snprintf(shield_name.value, sizeof(shield_name.value), "Shield");

        TBEVY_ENTITY_INSERT(&shield_ec, shield_transform, Transform);
        TBEVY_ENTITY_INSERT(&shield_ec, shield_sprite, Sprite);
        TBEVY_ENTITY_INSERT(&shield_ec, shield_tag, Shield);
        TBEVY_ENTITY_INSERT(&shield_ec, shield_name, Name);

        tecs_entity_t shield_id = tbevy_entity_id(&shield_ec);

        /* Establish hierarchy after all entities are spawned */
        tbevy_commands_add_child(commands, enemy_id, turret_id);
        tbevy_commands_add_child(commands, enemy_id, shield_id);

        spawner->enemies_this_wave++;

        /* Increase difficulty */
        if (spawner->enemies_this_wave >= 5) {
            spawner->wave_number++;
            spawner->enemies_this_wave = 0;
            spawner->spawn_interval *= 0.9f;
            printf("\n=== Wave %d ===\n", spawner->wave_number);
        }
    }
}

/* Hierarchy update system - update child transforms relative to parent */
static void hierarchy_update_system(tbevy_app_t* app, void* user_data) {
    (void)user_data;

    tecs_world_t* world = tbevy_app_world(app);
    const TimeResource* time = TBEVY_GET_RESOURCE(app, TimeResource);
    if (!time) return;

    /* Update turrets relative to parent */
    tecs_query_t* turret_query = tecs_query_new(world);
    TECS_QUERY_WITH(turret_query, Transform);
    TECS_QUERY_WITH(turret_query, Turret);
    tecs_query_build(turret_query);

    tecs_query_iter_t* iter = tecs_query_iter(turret_query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform* transforms = (Transform*)tecs_iter_column(iter, 0);
        Turret* turrets = (Turret*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            tecs_entity_t parent = tecs_get_parent(world, entities[i]);
            if (parent != TECS_ENTITY_NULL) {
                Transform* parent_transform = TECS_GET(world, parent, Transform);
                if (parent_transform) {
                    turrets[i].rotation_offset += turrets[i].rotation_speed * time->delta_time;
                    transforms[i].rotation = parent_transform->rotation + turrets[i].rotation_offset;
                }
            }
        }
    }
    tecs_query_iter_free(iter);
    tecs_query_free(turret_query);

    /* Update shields */
    tecs_query_t* shield_query = tecs_query_new(world);
    TECS_QUERY_WITH(shield_query, Transform);
    TECS_QUERY_WITH(shield_query, Shield);
    tecs_query_build(shield_query);

    iter = tecs_query_iter(shield_query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform* transforms = (Transform*)tecs_iter_column(iter, 0);
        Shield* shields = (Shield*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            shields[i].rotation += 2.0f * time->delta_time;
            transforms[i].rotation = shields[i].rotation;

            /* Check if parent's shield is depleted */
            tecs_entity_t parent = tecs_get_parent(world, entities[i]);
            if (parent != TECS_ENTITY_NULL) {
                Health* parent_health = TECS_GET(world, parent, Health);
                if (parent_health && parent_health->shield <= 0.0f) {
                    /* Hide shield sprite when depleted */
                    Sprite* shield_sprite = TECS_GET(world, entities[i], Sprite);
                    if (shield_sprite) shield_sprite->symbol = ' ';
                }
            }
        }
    }
    tecs_query_iter_free(iter);
    tecs_query_free(shield_query);
}

/* Bullet lifetime system */
static void bullet_lifetime_system(tbevy_app_t* app, void* user_data) {
    tbevy_commands_t* commands = (tbevy_commands_t*)user_data;

    const TimeResource* time = TBEVY_GET_RESOURCE(app, TimeResource);
    if (!time) return;

    tecs_query_t* query = tecs_query_new(tbevy_app_world(app));
    TECS_QUERY_WITH(query, Bullet);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Bullet* bullets = (Bullet*)tecs_iter_column(iter, 0);

        for (int i = 0; i < count; i++) {
            bullets[i].lifetime -= time->delta_time;
            if (bullets[i].lifetime <= 0.0f) {
                tbevy_commands_despawn(commands, entities[i]);
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Bounds checking system */
static void bounds_check_system(tbevy_app_t* app, void* user_data) {
    tbevy_commands_t* commands = (tbevy_commands_t*)user_data;

    const WorldBounds* bounds = TBEVY_GET_RESOURCE(app, WorldBounds);
    if (!bounds) return;

    tecs_query_t* query = tecs_query_new(tbevy_app_world(app));
    TECS_QUERY_WITH(query, Transform);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform* transforms = (Transform*)tecs_iter_column(iter, 0);

        for (int i = 0; i < count; i++) {
            if (fabsf(transforms[i].x) > bounds->bounds_x ||
                fabsf(transforms[i].y) > bounds->bounds_y) {
                /* Despawn entities that leave bounds */
                tbevy_commands_despawn(commands, entities[i]);
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Debug rendering system (text-based visualization) */
static void debug_render_system(tbevy_app_t* app, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_GET_RESOURCE(app, TimeResource);
    const GameStats* stats = TBEVY_GET_RESOURCE(app, GameStats);

    /* Only render every 10 frames for readability */
    if (time && time->frame % 10 != 0) return;

    printf("\n=== Frame %u (%.2fs) ===\n", time ? time->frame : 0, time ? time->time : 0.0f);

    if (stats) {
        printf("Score: %d | Kills: %d | Shots: %d | Hits Taken: %d\n",
               stats->score, stats->enemies_killed, stats->bullets_fired, stats->hits_taken);
    }

    /* Query all visible entities */
    tecs_query_t* query = tecs_query_new(tbevy_app_world(app));
    TECS_QUERY_WITH(query, Transform);
    TECS_QUERY_WITH(query, Sprite);
    tecs_query_build(query);

    printf("Entities:\n");

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        Transform* transforms = (Transform*)tecs_iter_column(iter, 0);
        Sprite* sprites = (Sprite*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            printf("  [%c] at (%.1f, %.1f) rot=%.1f scale=%.1f\n",
                   sprites[i].symbol,
                   transforms[i].x,
                   transforms[i].y,
                   transforms[i].rotation,
                   transforms[i].scale);
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Time update system */
static void time_update_system(tbevy_app_t* app, void* user_data) {
    (void)user_data;

    TimeResource* time = TBEVY_GET_RESOURCE_MUT(app, TimeResource);
    if (time) {
        time->frame++;
        time->delta_time = 0.016f; /* 60 FPS */
        time->time += time->delta_time;
    }
}

/* ============================================================================
 * State Transition Systems
 * ========================================================================= */

static void on_enter_menu(tbevy_app_t* app, void* user_data) {
    (void)user_data;
    printf("\n╔════════════════════════════╗\n");
    printf("║   SPACE SHOOTER - MENU     ║\n");
    printf("║                            ║\n");
    printf("║  Starting game...          ║\n");
    printf("╚════════════════════════════╝\n\n");

    /* Auto-start after 1 frame */
    TimeResource* time = TBEVY_GET_RESOURCE_MUT(app, TimeResource);
    if (time && time->frame > 0) {
        tbevy_commands_t commands;
        tbevy_commands_init(&commands, app);
        tbevy_commands_set_state(&commands, (int)GAME_STATE_PLAYING);
        tbevy_commands_apply(&commands);
        tbevy_commands_free(&commands);
    }
}

static void on_enter_playing(tbevy_app_t* app, void* user_data) {
    tbevy_commands_t* commands = (tbevy_commands_t*)user_data;

    printf("\n=== GAME START ===\n\n");

    /* Spawn player */
    tbevy_entity_commands_t player_ec = tbevy_commands_spawn(commands);

    Transform player_transform = { -200.0f, 0.0f, 0.0f, 1.5f };
    Velocity player_velocity = { 0.0f, 0.0f, 0.0f };
    Health player_health = { 100.0f, 100.0f, 50.0f };
    Weapon player_weapon = { 3.0f, 25.0f, 0.0f };
    Sprite player_sprite = { 'P', 5 };
    Collider player_collider = { 10.0f };
    Player player_tag = { 0 };
    Name player_name;
    snprintf(player_name.value, sizeof(player_name.value), "Player");

    TBEVY_ENTITY_INSERT(&player_ec, player_transform, Transform);
    TBEVY_ENTITY_INSERT(&player_ec, player_velocity, Velocity);
    TBEVY_ENTITY_INSERT(&player_ec, player_health, Health);
    TBEVY_ENTITY_INSERT(&player_ec, player_weapon, Weapon);
    TBEVY_ENTITY_INSERT(&player_ec, player_sprite, Sprite);
    TBEVY_ENTITY_INSERT(&player_ec, player_collider, Collider);
    TBEVY_ENTITY_INSERT(&player_ec, player_tag, Player);
    TBEVY_ENTITY_INSERT(&player_ec, player_name, Name);

    printf("Player spawned!\n");
}

static void on_exit_playing(tbevy_app_t* app, void* user_data) {
    (void)app;
    (void)user_data;
    printf("\n=== GAME STOPPED ===\n");
}

static void on_enter_game_over(tbevy_app_t* app, void* user_data) {
    (void)user_data;

    const GameStats* stats = TBEVY_GET_RESOURCE(app, GameStats);

    printf("\n╔════════════════════════════╗\n");
    printf("║       GAME OVER!           ║\n");
    printf("║                            ║\n");
    if (stats) {
        printf("║  Final Score: %-12d ║\n", stats->score);
        printf("║  Enemies Killed: %-8d ║\n", stats->enemies_killed);
        printf("║  Shots Fired: %-11d ║\n", stats->bullets_fired);
    }
    printf("╚════════════════════════════╝\n\n");
}

/* ============================================================================
 * Main
 * ========================================================================= */

static bool should_quit_callback(void* user_data) {
    tbevy_app_t* app = (tbevy_app_t*)user_data;
    const TimeResource* time = TBEVY_GET_RESOURCE(app, TimeResource);

    /* Run for 600 frames (10 seconds at 60fps) */
    return time && time->frame >= 600;
}

int main(void) {
    printf("=== TinyEcs.Bevy Complete Game Example ===\n");
    printf("Demonstrating: Hierarchy, States, Events, Observers, Bundles, Systems\n\n");

    srand(42); /* Deterministic randomness */

    /* Create app */
    tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_SINGLE);
    tecs_world_t* world = tbevy_app_world(app);

    /* Register components */
    TECS_COMPONENT_REGISTER(world, Transform);
    TECS_COMPONENT_REGISTER(world, Velocity);
    TECS_COMPONENT_REGISTER(world, Health);
    TECS_COMPONENT_REGISTER(world, Weapon);
    TECS_COMPONENT_REGISTER(world, Sprite);
    TECS_COMPONENT_REGISTER(world, Name);
    TECS_COMPONENT_REGISTER(world, Player);
    TECS_COMPONENT_REGISTER(world, Enemy);
    TECS_COMPONENT_REGISTER(world, Bullet);
    TECS_COMPONENT_REGISTER(world, Turret);
    TECS_COMPONENT_REGISTER(world, Shield);
    TECS_COMPONENT_REGISTER(world, Collider);

    /* Register resources */
    TimeResource_id = TBEVY_REGISTER_RESOURCE(TimeResource);
    GameStats_id = TBEVY_REGISTER_RESOURCE(GameStats);
    SpawnManager_id = TBEVY_REGISTER_RESOURCE(SpawnManager);
    WorldBounds_id = TBEVY_REGISTER_RESOURCE(WorldBounds);

    /* Initialize resources */
    TimeResource time_init = { 0.0f, 0.016f, 0 };
    GameStats stats_init = { 0, 0, 0, 0 };
    SpawnManager spawn_init = { 0.0f, 2.0f, 1, 0 };
    WorldBounds bounds_init = { 800.0f, 400.0f };

    tbevy_app_insert_resource(app, TimeResource_id, &time_init, sizeof(time_init));
    tbevy_app_insert_resource(app, GameStats_id, &stats_init, sizeof(stats_init));
    tbevy_app_insert_resource(app, SpawnManager_id, &spawn_init, sizeof(spawn_init));
    tbevy_app_insert_resource(app, WorldBounds_id, &bounds_init, sizeof(bounds_init));

    /* Register events */
    DamageEvent_id = TBEVY_REGISTER_EVENT(DamageEvent);
    CollisionEvent_id = TBEVY_REGISTER_EVENT(CollisionEvent);
    ScoreEvent_id = TBEVY_REGISTER_EVENT(ScoreEvent);
    DeathEvent_id = TBEVY_REGISTER_EVENT(DeathEvent);

    /* Setup commands for state transitions and systems */
    tbevy_commands_t commands;
    tbevy_commands_init(&commands, app);

    /* Add game states */
    tbevy_app_add_state(app, (int)GAME_STATE_MENU);

    /* State transition systems */
    tbevy_system_build(
        tbevy_system_on_enter(
            tbevy_app_add_system(app, on_enter_menu, NULL),
            (int)GAME_STATE_MENU
        )
    );

    tbevy_system_build(
        tbevy_system_on_enter(
            tbevy_app_add_system(app, on_enter_playing, &commands),
            (int)GAME_STATE_PLAYING
        )
    );

    tbevy_system_build(
        tbevy_system_on_exit(
            tbevy_app_add_system(app, on_exit_playing, NULL),
            (int)GAME_STATE_PLAYING
        )
    );

    tbevy_system_build(
        tbevy_system_on_enter(
            tbevy_app_add_system(app, on_enter_game_over, NULL),
            (int)GAME_STATE_GAME_OVER
        )
    );

    /* Add systems - carefully ordered */
    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, time_update_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_FIRST)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_system_run_if_state(
                tbevy_app_add_system(app, player_input_system, &commands),
                (int)GAME_STATE_PLAYING
            ),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_system_run_if_state(
                tbevy_app_add_system(app, enemy_ai_system, NULL),
                (int)GAME_STATE_PLAYING
            ),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_system_run_if_state(
                tbevy_app_add_system(app, enemy_spawn_system, &commands),
                (int)GAME_STATE_PLAYING
            ),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_system_run_if_state(
                tbevy_app_add_system(app, hierarchy_update_system, NULL),
                (int)GAME_STATE_PLAYING
            ),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_system_run_if_state(
                tbevy_app_add_system(app, movement_system, NULL),
                (int)GAME_STATE_PLAYING
            ),
            tbevy_stage_default(TBEVY_STAGE_POST_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_system_run_if_state(
                tbevy_app_add_system(app, collision_system, &commands),
                (int)GAME_STATE_PLAYING
            ),
            tbevy_stage_default(TBEVY_STAGE_POST_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_system_run_if_state(
                tbevy_app_add_system(app, bullet_lifetime_system, &commands),
                (int)GAME_STATE_PLAYING
            ),
            tbevy_stage_default(TBEVY_STAGE_POST_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_system_run_if_state(
                tbevy_app_add_system(app, bounds_check_system, &commands),
                (int)GAME_STATE_PLAYING
            ),
            tbevy_stage_default(TBEVY_STAGE_POST_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, debug_render_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_LAST)
        )
    );

    /* Register event handlers */
    tbevy_app_add_event_handler(app, CollisionEvent_id, handle_collision_events, &commands);
    tbevy_app_add_event_handler(app, DamageEvent_id, handle_damage_events, &commands);
    tbevy_app_add_event_handler(app, DeathEvent_id, handle_death_events, &commands);
    tbevy_app_add_event_handler(app, ScoreEvent_id, handle_score_events, NULL);

    /* Run game loop */
    printf("\n=== Starting Game Loop ===\n");
    tbevy_app_run_startup(app);
    tbevy_app_run(app, should_quit_callback, app);

    /* Cleanup */
    printf("\n=== Shutting Down ===\n");
    tbevy_commands_free(&commands);
    tbevy_app_free(app);

    printf("\n=== Example Completed Successfully ===\n");
    return 0;
}
