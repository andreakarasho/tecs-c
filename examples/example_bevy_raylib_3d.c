/*
 * TinyEcs.Bevy + Raylib 3D Game Example
 *
 * A 3D space shooter demonstrating:
 * - 3D rendering with Raylib
 * - Entity hierarchy (ships with turrets and shields)
 * - ECS systems with 3D transforms
 * - Camera system
 * - Particle effects
 * - System ordering
 */

#define TINYECS_IMPLEMENTATION
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs.h"
#include "tinyecs_bevy.h"

#include <raylib.h>
#include <raymath.h>
#include <rlgl.h>
#include <stdio.h>
#include <stdlib.h>
#include <math.h>

/* ============================================================================
 * Components
 * ========================================================================= */

TECS_DECLARE_COMPONENT(Transform3D);
typedef struct Transform3D {
    Vector3 position;
    Vector3 rotation;  // Euler angles in radians
    Vector3 scale;
} Transform3D;

TECS_DECLARE_COMPONENT(Velocity3D);
typedef struct Velocity3D {
    Vector3 linear;
    Vector3 angular;
} Velocity3D;

TECS_DECLARE_COMPONENT(MeshRenderer);
typedef struct MeshRenderer {
    int mesh_type;  // 0=cube, 1=sphere, 2=cylinder
    Color color;
    float size;
} MeshRenderer;

TECS_DECLARE_COMPONENT(Health);
typedef struct Health {
    float current;
    float max;
} Health;

TECS_DECLARE_COMPONENT(Weapon);
typedef struct Weapon {
    float fire_rate;
    float last_fired;
    float bullet_speed;
} Weapon;

TECS_DECLARE_COMPONENT(NameTag);
typedef struct NameTag {
    char value[32];
} NameTag;

/* Tag components */
TECS_DECLARE_COMPONENT(Player);
typedef struct Player { int _unused; } Player;

TECS_DECLARE_COMPONENT(Enemy);
typedef struct Enemy {
    float spawn_time;
} Enemy;

TECS_DECLARE_COMPONENT(Bullet);
typedef struct Bullet {
    tecs_entity_t owner;
    float lifetime;
} Bullet;

TECS_DECLARE_COMPONENT(Turret);
typedef struct Turret {
    float rotation_speed;
    Vector3 offset;
} Turret;

TECS_DECLARE_COMPONENT(Shield);
typedef struct Shield {
    float rotation_speed;
    float radius;
    float alpha;
} Shield;

TECS_DECLARE_COMPONENT(Particle);
typedef struct Particle {
    float lifetime;
    float max_lifetime;
    Vector3 velocity;
} Particle;

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
    Camera3D camera;
    Vector3 target_position;
} CameraResource;
tecs_component_id_t CameraResource_id;

typedef struct {
    int score;
    int enemies_killed;
    int bullets_fired;
} GameStats;
tecs_component_id_t GameStats_id;

typedef struct {
    float spawn_timer;
    float spawn_interval;
    int wave_number;
} SpawnManager;
tecs_component_id_t SpawnManager_id;

/* ============================================================================
 * Helper Functions
 * ========================================================================= */

Matrix GetTransformMatrix(Transform3D transform) {
    Matrix mat = MatrixIdentity();
    mat = MatrixMultiply(mat, MatrixScale(transform.scale.x, transform.scale.y, transform.scale.z));
    mat = MatrixMultiply(mat, MatrixRotateXYZ(transform.rotation));
    mat = MatrixMultiply(mat, MatrixTranslate(transform.position.x, transform.position.y, transform.position.z));
    return mat;
}

Vector3 TransformPoint(Vector3 point, Transform3D transform) {
    Matrix mat = GetTransformMatrix(transform);
    return Vector3Transform(point, mat);
}

/* ============================================================================
 * Game Systems
 * ========================================================================= */

/* Movement system */
static void movement_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time) return;



    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Transform3D);
    TECS_QUERY_WITH(query, Velocity3D);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        Transform3D* transforms = (Transform3D*)tecs_iter_column(iter, 0);
        Velocity3D* velocities = (Velocity3D*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            transforms[i].position = Vector3Add(transforms[i].position,
                Vector3Scale(velocities[i].linear, time->delta_time));
            transforms[i].rotation = Vector3Add(transforms[i].rotation,
                Vector3Scale(velocities[i].angular, time->delta_time));
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Player input and control */
static void player_input_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time) return;



    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Transform3D);
    TECS_QUERY_WITH(query, Velocity3D);
    TECS_QUERY_WITH(query, Weapon);
    TECS_QUERY_WITH(query, Player);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform3D* transforms = (Transform3D*)tecs_iter_column(iter, 0);
        Velocity3D* velocities = (Velocity3D*)tecs_iter_column(iter, 1);
        Weapon* weapons = (Weapon*)tecs_iter_column(iter, 2);

        for (int i = 0; i < count; i++) {
            // Movement controls (WASD + QE for up/down)
            velocities[i].linear = (Vector3){0};
            float speed = 15.0f;

            if (IsKeyDown(KEY_W)) velocities[i].linear.z -= speed;
            if (IsKeyDown(KEY_S)) velocities[i].linear.z += speed;
            if (IsKeyDown(KEY_A)) velocities[i].linear.x -= speed;
            if (IsKeyDown(KEY_D)) velocities[i].linear.x += speed;
            if (IsKeyDown(KEY_Q)) velocities[i].linear.y -= speed;
            if (IsKeyDown(KEY_E)) velocities[i].linear.y += speed;

            // Rotation controls (Arrow keys)
            velocities[i].angular = (Vector3){0};
            float rot_speed = 2.0f;

            if (IsKeyDown(KEY_UP)) velocities[i].angular.x -= rot_speed;
            if (IsKeyDown(KEY_DOWN)) velocities[i].angular.x += rot_speed;
            if (IsKeyDown(KEY_LEFT)) velocities[i].angular.y += rot_speed;
            if (IsKeyDown(KEY_RIGHT)) velocities[i].angular.y -= rot_speed;

            // Fire weapon (SPACE)
            if (IsKeyDown(KEY_SPACE) &&
                time->time - weapons[i].last_fired > 1.0f / weapons[i].fire_rate) {

                weapons[i].last_fired = time->time;

                // Spawn bullet
                tbevy_entity_commands_t bullet_ec = tbevy_commands_spawn(ctx->commands);

                // Calculate forward direction from rotation
                Vector3 forward = {0, 0, -1};
                Matrix rot = MatrixRotateXYZ(transforms[i].rotation);
                forward = Vector3Transform(forward, rot);
                forward = Vector3Normalize(forward);

                Vector3 bullet_pos = Vector3Add(transforms[i].position,
                    Vector3Scale(forward, 2.0f));

                Transform3D bullet_transform = {
                    bullet_pos,
                    transforms[i].rotation,
                    {0.3f, 0.3f, 0.3f}
                };

                Velocity3D bullet_velocity = {
                    Vector3Scale(forward, weapons[i].bullet_speed),
                    {0, 0, 0}
                };

                MeshRenderer bullet_mesh = {1, YELLOW, 0.3f};
                Bullet bullet_tag = {entities[i], 3.0f};

                TBEVY_ENTITY_INSERT(&bullet_ec, bullet_transform, Transform3D);
                TBEVY_ENTITY_INSERT(&bullet_ec, bullet_velocity, Velocity3D);
                TBEVY_ENTITY_INSERT(&bullet_ec, bullet_mesh, MeshRenderer);
                TBEVY_ENTITY_INSERT(&bullet_ec, bullet_tag, Bullet);

                GameStats* stats = TBEVY_CTX_GET_RESOURCE_MUT(ctx, GameStats);
                if (stats) stats->bullets_fired++;
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Enemy AI - chase player */
static void enemy_ai_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time) return;



    // Find player
    Vector3 player_pos = {0};
    bool player_found = false;

    tecs_query_t* player_query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(player_query, Transform3D);
    TECS_QUERY_WITH(player_query, Player);
    tecs_query_build(player_query);

    tecs_query_iter_t* player_iter = tecs_query_iter(player_query);
    if (tecs_iter_next(player_iter)) {
        Transform3D* player_transforms = (Transform3D*)tecs_iter_column(player_iter, 0);
        player_pos = player_transforms[0].position;
        player_found = true;
    }
    tecs_query_iter_free(player_iter);
    tecs_query_free(player_query);

    if (!player_found) return;

    // Update enemies
    tecs_query_t* enemy_query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(enemy_query, Transform3D);
    TECS_QUERY_WITH(enemy_query, Velocity3D);
    TECS_QUERY_WITH(enemy_query, Enemy);
    tecs_query_build(enemy_query);

    tecs_query_iter_t* iter = tecs_query_iter(enemy_query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        Transform3D* transforms = (Transform3D*)tecs_iter_column(iter, 0);
        Velocity3D* velocities = (Velocity3D*)tecs_iter_column(iter, 1);
        Enemy* enemies = (Enemy*)tecs_iter_column(iter, 2);

        for (int i = 0; i < count; i++) {
            Vector3 to_player = Vector3Subtract(player_pos, transforms[i].position);
            float dist = Vector3Length(to_player);

            if (dist > 0.1f) {
                Vector3 dir = Vector3Normalize(to_player);
                float speed = 8.0f;
                velocities[i].linear = Vector3Scale(dir, speed);

                // Rotate towards player
                float age = time->time - enemies[i].spawn_time;
                velocities[i].angular.y = sinf(age * 2.0f) * 1.5f;
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(enemy_query);
}

/* Turret system - rotate turrets relative to parent */
static void turret_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time) return;



    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Transform3D);
    TECS_QUERY_WITH(query, Turret);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform3D* transforms = (Transform3D*)tecs_iter_column(iter, 0);
        Turret* turrets = (Turret*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            tecs_entity_t parent_id = tecs_get_parent(ctx->world, entities[i]);
            if (parent_id != TECS_ENTITY_NULL) {
                Transform3D* parent_transform = TECS_GET(ctx->world, parent_id, Transform3D);
                if (parent_transform) {
                    // Position relative to parent
                    Matrix parent_mat = GetTransformMatrix(*parent_transform);
                    transforms[i].position = Vector3Transform(turrets[i].offset, parent_mat);

                    // Rotate
                    transforms[i].rotation.y += turrets[i].rotation_speed * time->delta_time;
                }
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Shield system */
static void shield_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time) return;



    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Transform3D);
    TECS_QUERY_WITH(query, Shield);
    TECS_QUERY_WITH(query, MeshRenderer);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform3D* transforms = (Transform3D*)tecs_iter_column(iter, 0);
        Shield* shields = (Shield*)tecs_iter_column(iter, 1);
        MeshRenderer* renderers = (MeshRenderer*)tecs_iter_column(iter, 2);

        for (int i = 0; i < count; i++) {
            tecs_entity_t parent_id = tecs_get_parent(ctx->world, entities[i]);
            if (parent_id != TECS_ENTITY_NULL) {
                Transform3D* parent_transform = TECS_GET(ctx->world, parent_id, Transform3D);
                Health* parent_health = TECS_GET(ctx->world, parent_id, Health);

                if (parent_transform) {
                    transforms[i].position = parent_transform->position;
                    transforms[i].rotation.y += shields[i].rotation_speed * time->delta_time;
                    transforms[i].scale = (Vector3){
                        shields[i].radius, shields[i].radius, shields[i].radius
                    };

                    // Fade shield based on health
                    if (parent_health) {
                        float health_percent = parent_health->current / parent_health->max;
                        shields[i].alpha = health_percent * 150.0f;
                        renderers[i].color.a = (unsigned char)shields[i].alpha;
                    }
                }
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Bullet lifetime */
static void bullet_lifetime_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time) return;



    tecs_query_t* query = tecs_query_new(ctx->world);
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
                // Despawn via commands (deferred)
                tbevy_commands_entity_despawn(ctx->commands, entities[i]);
            }
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);

    tbevy_commands_apply(ctx->commands);
}

/* Simple collision detection */
static void collision_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;


    GameStats* stats = TBEVY_CTX_GET_RESOURCE_MUT(ctx, GameStats);

    // Get all bullets
    tecs_query_t* bullet_query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(bullet_query, Transform3D);
    TECS_QUERY_WITH(bullet_query, Bullet);
    tecs_query_build(bullet_query);

    // Get all enemies
    tecs_query_t* enemy_query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(enemy_query, Transform3D);
    TECS_QUERY_WITH(enemy_query, Health);
    TECS_QUERY_WITH(enemy_query, Enemy);
    tecs_query_build(enemy_query);

    tecs_query_iter_t* bullet_iter = tecs_query_iter(bullet_query);
    while (tecs_iter_next(bullet_iter)) {
        int bullet_count = tecs_iter_count(bullet_iter);
        tecs_entity_t* bullet_entities = tecs_iter_entities(bullet_iter);
        Transform3D* bullet_transforms = (Transform3D*)tecs_iter_column(bullet_iter, 0);

        tecs_query_iter_t* enemy_iter = tecs_query_iter(enemy_query);
        while (tecs_iter_next(enemy_iter)) {
            int enemy_count = tecs_iter_count(enemy_iter);
            tecs_entity_t* enemy_entities = tecs_iter_entities(enemy_iter);
            Transform3D* enemy_transforms = (Transform3D*)tecs_iter_column(enemy_iter, 0);
            Health* enemy_healths = (Health*)tecs_iter_column(enemy_iter, 1);

            for (int b = 0; b < bullet_count; b++) {
                for (int e = 0; e < enemy_count; e++) {
                    float dist = Vector3Distance(
                        bullet_transforms[b].position,
                        enemy_transforms[e].position
                    );

                    if (dist < 2.0f) {
                        // Hit!
                        enemy_healths[e].current -= 25.0f;

                        // Delete bullet via commands (deferred)
                        tbevy_commands_entity_despawn(ctx->commands, bullet_entities[b]);

                        // Check if enemy died
                        if (enemy_healths[e].current <= 0) {
                            if (stats) {
                                stats->enemies_killed++;
                                stats->score += 100;
                            }

                            // Spawn particles via commands (deferred)
                            for (int p = 0; p < 10; p++) {
                                Transform3D part_transform = {
                                    enemy_transforms[e].position,
                                    {0, 0, 0},
                                    {0.2f, 0.2f, 0.2f}
                                };

                                Vector3 random_vel = {
                                    ((float)rand() / RAND_MAX - 0.5f) * 20.0f,
                                    ((float)rand() / RAND_MAX - 0.5f) * 20.0f,
                                    ((float)rand() / RAND_MAX - 0.5f) * 20.0f
                                };

                                Particle part = {1.0f, 1.0f, random_vel};
                                MeshRenderer part_mesh = {0, ORANGE, 0.2f};

                                tbevy_entity_commands_t particle_ec = tbevy_commands_spawn(ctx->commands);
                                TBEVY_ENTITY_INSERT(&particle_ec, part_transform, Transform3D);
                                TBEVY_ENTITY_INSERT(&particle_ec, part, Particle);
                                TBEVY_ENTITY_INSERT(&particle_ec, part_mesh, MeshRenderer);
                            }

                            // Delete enemy and children via commands (deferred)
                            // Note: tecs_remove_all_children needs to be called before despawn
                            // This is a hierarchy operation that should happen immediately
                            tecs_remove_all_children(ctx->world, enemy_entities[e]);
                            tbevy_commands_entity_despawn(ctx->commands, enemy_entities[e]);
                        }

                        goto next_bullet;
                    }
                }
            }
            next_bullet:;
        }
        tecs_query_iter_free(enemy_iter);
    }

    tecs_query_iter_free(bullet_iter);
    tecs_query_free(bullet_query);
    tecs_query_free(enemy_query);
}

/* Particle system */
static void particle_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!time) return;



    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Transform3D);
    TECS_QUERY_WITH(query, Particle);
    TECS_QUERY_WITH(query, MeshRenderer);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        tecs_entity_t* entities = tecs_iter_entities(iter);
        Transform3D* transforms = (Transform3D*)tecs_iter_column(iter, 0);
        Particle* particles = (Particle*)tecs_iter_column(iter, 1);
        MeshRenderer* renderers = (MeshRenderer*)tecs_iter_column(iter, 2);

        for (int i = 0; i < count; i++) {
            particles[i].lifetime -= time->delta_time;

            if (particles[i].lifetime <= 0) {
                // Despawn via commands (deferred)
                tbevy_commands_entity_despawn(ctx->commands, entities[i]);
                continue;
            }

            // Move
            transforms[i].position = Vector3Add(transforms[i].position,
                Vector3Scale(particles[i].velocity, time->delta_time));

            // Fade out
            float alpha = particles[i].lifetime / particles[i].max_lifetime;
            renderers[i].color.a = (unsigned char)(alpha * 255);
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Enemy spawner */
static void enemy_spawn_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    SpawnManager* spawner = TBEVY_CTX_GET_RESOURCE_MUT(ctx, SpawnManager);
    if (!time || !spawner) return;

    spawner->spawn_timer += time->delta_time;

    if (spawner->spawn_timer >= spawner->spawn_interval) {
        spawner->spawn_timer = 0.0f;



        // Spawn enemy
        tbevy_entity_commands_t enemy_ec = tbevy_commands_spawn(ctx->commands);

        float angle = (float)rand() / RAND_MAX * PI * 2.0f;
        float radius = 50.0f;
        Vector3 spawn_pos = {
            cosf(angle) * radius,
            ((float)rand() / RAND_MAX - 0.5f) * 20.0f,
            sinf(angle) * radius
        };

        Transform3D enemy_transform = {
            spawn_pos,
            {0, 0, 0},
            {2.0f, 2.0f, 2.0f}
        };

        Velocity3D enemy_velocity = {{0, 0, 0}, {0, 0, 0}};
        Health enemy_health = {100.0f, 100.0f};
        MeshRenderer enemy_mesh = {0, RED, 2.0f};
        Enemy enemy_tag = {time->time};

        TBEVY_ENTITY_INSERT(&enemy_ec, enemy_transform, Transform3D);
        TBEVY_ENTITY_INSERT(&enemy_ec, enemy_velocity, Velocity3D);
        TBEVY_ENTITY_INSERT(&enemy_ec, enemy_health, Health);
        TBEVY_ENTITY_INSERT(&enemy_ec, enemy_mesh, MeshRenderer);
        TBEVY_ENTITY_INSERT(&enemy_ec, enemy_tag, Enemy);

        tecs_entity_t enemy_id = tbevy_entity_id(&enemy_ec);

        // Add turret
        tbevy_entity_commands_t turret_ec = tbevy_commands_spawn(ctx->commands);
        Transform3D turret_transform = {{0, 1.5f, 0}, {0, 0, 0}, {0.5f, 0.5f, 0.5f}};
        Turret turret = {3.0f, {0, 1.5f, 0}};
        MeshRenderer turret_mesh = {2, DARKGRAY, 0.5f};

        TBEVY_ENTITY_INSERT(&turret_ec, turret_transform, Transform3D);
        TBEVY_ENTITY_INSERT(&turret_ec, turret, Turret);
        TBEVY_ENTITY_INSERT(&turret_ec, turret_mesh, MeshRenderer);

        tecs_entity_t turret_id = tbevy_entity_id(&turret_ec);

        // Add shield
        tbevy_entity_commands_t shield_ec = tbevy_commands_spawn(ctx->commands);
        Transform3D shield_transform = {{0, 0, 0}, {0, 0, 0}, {3.0f, 3.0f, 3.0f}};
        Shield shield = {2.0f, 3.0f, 100.0f};
        MeshRenderer shield_mesh = {1, (Color){100, 100, 255, 100}, 3.0f};

        TBEVY_ENTITY_INSERT(&shield_ec, shield_transform, Transform3D);
        TBEVY_ENTITY_INSERT(&shield_ec, shield, Shield);
        TBEVY_ENTITY_INSERT(&shield_ec, shield_mesh, MeshRenderer);

        tecs_entity_t shield_id = tbevy_entity_id(&shield_ec);

        // Apply and establish hierarchy
        tbevy_commands_apply(ctx->commands);
        tecs_add_child(ctx->world, enemy_id, turret_id);
        tecs_add_child(ctx->world, enemy_id, shield_id);

        spawner->wave_number++;
    }
}

/* Camera follow player */
static void camera_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    CameraResource* cam_res = TBEVY_CTX_GET_RESOURCE_MUT(ctx, CameraResource);
    if (!cam_res) return;



    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Transform3D);
    TECS_QUERY_WITH(query, Player);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    if (tecs_iter_next(iter)) {
        Transform3D* player_transforms = (Transform3D*)tecs_iter_column(iter, 0);
        Vector3 player_pos = player_transforms[0].position;

        // Smooth camera follow
        Vector3 target = player_pos;
        cam_res->target_position = Vector3Lerp(cam_res->target_position, target, 0.1f);

        Vector3 offset = {0, 15.0f, 25.0f};
        cam_res->camera.position = Vector3Add(cam_res->target_position, offset);
        cam_res->camera.target = cam_res->target_position;
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

/* Render system */
static void render_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    const CameraResource* cam_res = TBEVY_CTX_GET_RESOURCE(ctx, CameraResource);
    const GameStats* stats = TBEVY_CTX_GET_RESOURCE(ctx, GameStats);
    const TimeResource* time = TBEVY_CTX_GET_RESOURCE(ctx, TimeResource);
    if (!cam_res) return;

    BeginDrawing();
    ClearBackground(BLACK);

    BeginMode3D(cam_res->camera);

    // Draw grid
    DrawGrid(100, 5.0f);

    // Render all meshes


    tecs_query_t* query = tecs_query_new(ctx->world);
    TECS_QUERY_WITH(query, Transform3D);
    TECS_QUERY_WITH(query, MeshRenderer);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_iter_next(iter)) {
        int count = tecs_iter_count(iter);
        Transform3D* transforms = (Transform3D*)tecs_iter_column(iter, 0);
        MeshRenderer* renderers = (MeshRenderer*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            Vector3 pos = transforms[i].position;
            Vector3 rot_deg = Vector3Scale(transforms[i].rotation, RAD2DEG);

            rlPushMatrix();
            rlTranslatef(pos.x, pos.y, pos.z);
            rlRotatef(rot_deg.y, 0, 1, 0);
            rlRotatef(rot_deg.x, 1, 0, 0);
            rlRotatef(rot_deg.z, 0, 0, 1);
            rlScalef(transforms[i].scale.x, transforms[i].scale.y, transforms[i].scale.z);

            switch (renderers[i].mesh_type) {
                case 0: // Cube
                    DrawCube((Vector3){0,0,0}, 1.0f, 1.0f, 1.0f, renderers[i].color);
                    DrawCubeWires((Vector3){0,0,0}, 1.0f, 1.0f, 1.0f, WHITE);
                    break;
                case 1: // Sphere
                    DrawSphere((Vector3){0,0,0}, 1.0f, renderers[i].color);
                    DrawSphereWires((Vector3){0,0,0}, 1.0f, 16, 16, WHITE);
                    break;
                case 2: // Cylinder
                    DrawCylinder((Vector3){0,0,0}, 0.5f, 0.5f, 1.0f, 8, renderers[i].color);
                    DrawCylinderWires((Vector3){0,0,0}, 0.5f, 0.5f, 1.0f, 8, WHITE);
                    break;
            }

            rlPopMatrix();
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);

    EndMode3D();

    // Draw UI
    if (stats && time) {
        DrawText(TextFormat("Score: %d", stats->score), 10, 10, 20, WHITE);
        DrawText(TextFormat("Kills: %d", stats->enemies_killed), 10, 35, 20, WHITE);
        DrawText(TextFormat("Shots: %d", stats->bullets_fired), 10, 60, 20, WHITE);
        DrawText(TextFormat("FPS: %d", GetFPS()), 10, 85, 20, LIME);
    }

    DrawText("WASD/QE: Move | Arrows: Rotate | SPACE: Fire", 10, GetScreenHeight() - 30, 20, GRAY);

    EndDrawing();
}

/* Time update */
static void time_update_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    TimeResource* time = TBEVY_CTX_GET_RESOURCE_MUT(ctx, TimeResource);
    if (time) {
        time->delta_time = GetFrameTime();
        time->time += time->delta_time;
        time->frame++;
    }
}

/* Startup system */
static void startup_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    // Spawn player
    tbevy_entity_commands_t player_ec = tbevy_commands_spawn(ctx->commands);

    Transform3D player_transform = {{0, 0, 0}, {0, 0, 0}, {1.5f, 1.5f, 1.5f}};
    Velocity3D player_velocity = {{0, 0, 0}, {0, 0, 0}};
    Health player_health = {100.0f, 100.0f};
    Weapon player_weapon = {5.0f, 0.0f, 50.0f};
    MeshRenderer player_mesh = {0, BLUE, 1.5f};
    Player player_tag = {0};

    TBEVY_ENTITY_INSERT(&player_ec, player_transform, Transform3D);
    TBEVY_ENTITY_INSERT(&player_ec, player_velocity, Velocity3D);
    TBEVY_ENTITY_INSERT(&player_ec, player_health, Health);
    TBEVY_ENTITY_INSERT(&player_ec, player_weapon, Weapon);
    TBEVY_ENTITY_INSERT(&player_ec, player_mesh, MeshRenderer);
    TBEVY_ENTITY_INSERT(&player_ec, player_tag, Player);

    printf("Player spawned!\n");
}

/* ============================================================================
 * Main
 * ========================================================================= */

static bool should_quit(tbevy_app_t* app) {
    (void)app;
    return WindowShouldClose();
}

int main(void) {
    printf("╔═══════════════════════════════════════════╗\n");
    printf("║  TinyECS + Raylib 3D Space Shooter       ║\n");
    printf("║                                           ║\n");
    printf("║  WASD/QE: Move    Arrows: Rotate         ║\n");
    printf("║  SPACE: Fire      ESC: Quit              ║\n");
    printf("╚═══════════════════════════════════════════╝\n\n");

    // Initialize Raylib
    InitWindow(1280, 720, "TinyECS + Raylib 3D - Space Shooter");
    SetTargetFPS(-1);

    // Create app
    tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_SINGLE);
    tecs_world_t* world = tbevy_app_world(app);

    // Register components
    TECS_COMPONENT_REGISTER(world, Transform3D);
    TECS_COMPONENT_REGISTER(world, Velocity3D);
    TECS_COMPONENT_REGISTER(world, MeshRenderer);
    TECS_COMPONENT_REGISTER(world, Health);
    TECS_COMPONENT_REGISTER(world, Weapon);
    TECS_COMPONENT_REGISTER(world, NameTag);
    TECS_COMPONENT_REGISTER(world, Player);
    TECS_COMPONENT_REGISTER(world, Enemy);
    TECS_COMPONENT_REGISTER(world, Bullet);
    TECS_COMPONENT_REGISTER(world, Turret);
    TECS_COMPONENT_REGISTER(world, Shield);
    TECS_COMPONENT_REGISTER(world, Particle);

    // Register resources
    TimeResource_id = TBEVY_REGISTER_RESOURCE(TimeResource);
    CameraResource_id = TBEVY_REGISTER_RESOURCE(CameraResource);
    GameStats_id = TBEVY_REGISTER_RESOURCE(GameStats);
    SpawnManager_id = TBEVY_REGISTER_RESOURCE(SpawnManager);

    // Initialize resources
    TimeResource time_init = {0.0f, 0.016f, 0};
    CameraResource cam_init = {
        .camera = {
            {0, 15.0f, 25.0f},
            {0, 0, 0},
            {0, 1, 0},
            45.0f,
            CAMERA_PERSPECTIVE
        },
        .target_position = {0, 0, 0}
    };
    GameStats stats_init = {0, 0, 0};
    SpawnManager spawn_init = {0.0f, 3.0f, 0};

    TBEVY_APP_INSERT_RESOURCE(app, time_init, TimeResource);
    TBEVY_APP_INSERT_RESOURCE(app, cam_init, CameraResource);
    TBEVY_APP_INSERT_RESOURCE(app, stats_init, GameStats);
    TBEVY_APP_INSERT_RESOURCE(app, spawn_init, SpawnManager);

    // Add systems
    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, startup_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_STARTUP)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, time_update_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_FIRST)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, player_input_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, enemy_ai_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, enemy_spawn_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, turret_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, shield_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, movement_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_POST_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, collision_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_POST_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, bullet_lifetime_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_POST_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, particle_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_POST_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, camera_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_POST_UPDATE)
        )
    );

    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, render_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_LAST)
        )
    );

    // Run
    printf("Starting game...\n");
    tbevy_app_run_startup(app);
    tbevy_app_run(app, should_quit);

    // Cleanup
    printf("\nShutting down...\n");
    tbevy_app_free(app);
    CloseWindow();

    printf("Game completed successfully!\n");
    return 0;
}
