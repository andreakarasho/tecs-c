/*
 * TinyEcs.Bevy Performance Benchmark
 *
 * Optimized benchmark using Bevy startup system:
 * - Spawns 1,048,576 entities (524,288 * 2) in startup system
 * - Reuses query across frames (passed as user_data)
 * - Uses library-cached iterator for zero allocations
 * - Processes all entities properly
 */

#define TINYECS_IMPLEMENTATION
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs.h"
#include "tinyecs_bevy.h"

#include <stdio.h>
#include <time.h>

#ifdef _WIN32
#include <windows.h>
static double get_time_ms(void) {
    LARGE_INTEGER frequency, counter;
    QueryPerformanceFrequency(&frequency);
    QueryPerformanceCounter(&counter);
    return (double)(counter.QuadPart * 1000.0) / frequency.QuadPart;
}
#else
#include <sys/time.h>
static double get_time_ms(void) {
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return tv.tv_sec * 1000.0 + tv.tv_usec / 1000.0;
}
#endif

/* Components - Using declaration macros */
TECS_DECLARE_COMPONENT(Position);
struct Position { float x, y; };

TECS_DECLARE_COMPONENT(Velocity);
struct Velocity { float x, y; };

#define ENTITIES_COUNT (524288 * 2)
#define FRAMES_PER_BATCH 3600

/* Startup system - spawns entities */
static void startup_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)user_data;

    tecs_world_t* world = ctx->world;

    printf("[Startup] Spawning %d entities...\n", ENTITIES_COUNT);
    double spawn_start = get_time_ms();

    for (int i = 0; i < ENTITIES_COUNT; i++) {
        tecs_entity_t entity = tecs_entity_new(world);
        Position pos = {1.0f, 1.0f};
        Velocity vel = {1.0001f, 1.0001f};
        TECS_SET(world, entity, Position, pos);
        TECS_SET(world, entity, Velocity, vel);

        if ((i + 1) % 100000 == 0) {
            printf("  Spawned %d entities...\n", i + 1);
        }
    }

    double spawn_elapsed = get_time_ms() - spawn_start;
    printf("[Startup] Spawned %d entities in %.2f ms (%.0f entities/sec)\n",
           ENTITIES_COUNT, spawn_elapsed, ENTITIES_COUNT / (spawn_elapsed / 1000.0));
    printf("[Startup] World entity count: %d\n", tecs_world_entity_count(world));
}

/* Update system - uses library-cached iterator for zero allocations */
static void update_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)ctx;
    tecs_query_t* query = (tecs_query_t*)user_data;

    /* Use library-cached iterator - no allocation! */
    tecs_query_iter_t* iter = tecs_query_iter_cached(query);

    while (tecs_query_next(iter)) {
        int count = tecs_iter_count(iter);
        Position* positions = (Position*)tecs_iter_column(iter, 0);
        Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            positions[i].x *= velocities[i].x;
            positions[i].y *= velocities[i].y;
        }
    }
    /* No free needed - managed by query! */
}

int main(void) {
    printf("=== TinyEcs.Bevy Performance Benchmark ===\n");
    printf("Entity count: %d\n", ENTITIES_COUNT);
    printf("Frames per batch: %d\n", FRAMES_PER_BATCH);
    printf("Component size: Position=%zu bytes, Velocity=%zu bytes\n\n",
           sizeof(Position), sizeof(Velocity));

    tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_SINGLE);
    tecs_world_t* world = tbevy_app_world(app);

    /* Register components using macro */
    TECS_COMPONENT_REGISTER(world, Position);
    TECS_COMPONENT_REGISTER(world, Velocity);

    /* Add startup system */
    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, startup_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_STARTUP)
        )
    );

    /* Create query ONCE (reused across all frames) */
    tecs_query_t* query = tecs_query_new(world);
    TECS_QUERY_WITH(query, Position);
    TECS_QUERY_WITH(query, Velocity);
    tecs_query_build(query);

    /* Add update system (pass query as user_data) */
    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, update_system, query),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    /* Run startup once */
    printf("[Main] Running startup...\n");
    tbevy_app_run_startup(app);

    /* Verify entities were spawned */
    int entity_count = tecs_world_entity_count(world);
    printf("[Main] Entity count after startup: %d\n", entity_count);

    if (entity_count != ENTITIES_COUNT) {
        printf("ERROR: Expected %d entities, found %d!\n", ENTITIES_COUNT, entity_count);
        return 1;
    }

    /* Verify query returns entities */
    printf("[Main] Verifying query...\n");
    int verify_count = 0;
    tecs_query_iter_t* verify_iter = tecs_query_iter_cached(query);
    while (tecs_query_next(verify_iter)) {
        verify_count += tecs_iter_count(verify_iter);
    }
    printf("[Main] Query returns %d entities\n\n", verify_count);

    if (verify_count != ENTITIES_COUNT) {
        printf("ERROR: Query returned %d entities, expected %d!\n", verify_count, ENTITIES_COUNT);
        return 1;
    }

    printf("[Main] Starting benchmark loop...\n");
    printf("Running %d frames per measurement batch...\n\n", FRAMES_PER_BATCH);

    /* Benchmark loop */
    double start_time = get_time_ms();
    double last_time = start_time;
    int batch_count = 0;

    while (batch_count < 50) {
        /* Run a batch of frames */
        for (int i = 0; i < FRAMES_PER_BATCH; i++) {
            tbevy_app_update(app);
        }

        /* Measure time */
        double current_time = get_time_ms();
        double batch_elapsed = current_time - last_time;
        double total_elapsed = current_time - start_time;
        last_time = current_time;

        batch_count++;

        /* Calculate statistics */
        double ms_per_frame = batch_elapsed / FRAMES_PER_BATCH;
        double fps = 1000.0 / ms_per_frame;
        double entities_per_second = ENTITIES_COUNT * fps;
        double total_frames = batch_count * FRAMES_PER_BATCH;

        printf("Batch %3d: %.2f ms (%.3f ms/frame, %.0f FPS, %.2fM entities/sec)\n",
               batch_count,
               batch_elapsed,
               ms_per_frame,
               fps,
               entities_per_second / 1000000.0);

        /* Print summary every 10 batches */
        if (batch_count % 10 == 0) {
            double avg_ms_per_frame = total_elapsed / total_frames;
            double avg_fps = 1000.0 / avg_ms_per_frame;
            double avg_entities_per_second = ENTITIES_COUNT * avg_fps;

            printf("\n--- Average over %d frames (%.2f seconds) ---\n",
                   (int)total_frames, total_elapsed / 1000.0);
            printf("  Time per frame: %.3f ms\n", avg_ms_per_frame);
            printf("  FPS: %.0f\n", avg_fps);
            printf("  Entities processed: %.2f M/sec\n", avg_entities_per_second / 1000000.0);
            printf("  Total frames: %d\n\n", (int)total_frames);
        }
    }

    /* Final statistics */
    double total_time = get_time_ms() - start_time;
    double total_frames = batch_count * FRAMES_PER_BATCH;
    double avg_ms_per_frame = total_time / total_frames;
    double avg_fps = 1000.0 / avg_ms_per_frame;
    double avg_entities_per_second = ENTITIES_COUNT * avg_fps;

    printf("\n=== Final Statistics ===\n");
    printf("Total time: %.2f seconds\n", total_time / 1000.0);
    printf("Total frames: %d\n", (int)total_frames);
    printf("Average time per frame: %.3f ms\n", avg_ms_per_frame);
    printf("Average FPS: %.0f\n", avg_fps);
    printf("Average entities/sec: %.2f M\n", avg_entities_per_second / 1000000.0);
    printf("Total entity updates: %.2f M\n", (ENTITIES_COUNT * total_frames) / 1000000.0);

    /* Memory usage estimate */
    size_t entity_size = sizeof(Position) + sizeof(Velocity);
    size_t total_memory = ENTITIES_COUNT * entity_size;
    printf("\nMemory usage (estimate):\n");
    printf("  Component data: %.2f MB\n", total_memory / (1024.0 * 1024.0));
    printf("  Per entity: %zu bytes\n", entity_size);

    /* Cleanup */
    printf("\n[Main] Cleaning up...\n");
    tecs_query_free(query);
    tbevy_app_free(app);

    printf("\n=== Benchmark completed successfully ===\n");
    return 0;
}
