/*
 * TinyEcs Core Performance Benchmark
 *
 * Direct port of C# MyBattleground/Program.cs example:
 * - Spawns 1,048,576 entities (524,288 * 2)
 * - Each entity has Position and Velocity components
 * - Runs a simple update system that multiplies position by velocity
 * - Measures throughput over 3600 frame batches
 *
 * This uses the core ECS API directly (no Bevy layer) for accurate comparison.
 */

#define TINYECS_IMPLEMENTATION
#include "tinyecs.h"

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

/* ============================================================================
 * Components
 * ========================================================================= */

typedef struct {
    float x, y;
} Position;

typedef struct {
    float x, y;
} Velocity;

/* Component IDs */
static tecs_component_id_t Position_id;
static tecs_component_id_t Velocity_id;

/* ============================================================================
 * Configuration
 * ========================================================================= */

/* Number of entities to spawn (matches C# example) */
#define ENTITIES_COUNT (524288 * 2 * 1)

/* Number of frames per measurement batch */
#define FRAMES_PER_BATCH 3600

/* ============================================================================
 * Main
 * ========================================================================= */

int main(void) {
    printf("=== TinyEcs Core Performance Benchmark ===\n");
    printf("Entity count: %d\n", ENTITIES_COUNT);
    printf("Frames per batch: %d\n", FRAMES_PER_BATCH);
    printf("Component size: Position=%zu bytes, Velocity=%zu bytes\n\n",
           sizeof(Position), sizeof(Velocity));

    /* Create world */
    tecs_world_t* world = tecs_world_new();

    /* Register component types */
    Position_id = tecs_register_component(world, "Position", sizeof(Position));
    Velocity_id = tecs_register_component(world, "Velocity", sizeof(Velocity));

    /* Spawn entities */
    printf("[Startup] Spawning %d entities...\n", ENTITIES_COUNT);
    double spawn_start = get_time_ms();

    for (int i = 0; i < ENTITIES_COUNT; i++) {
        tecs_entity_t entity = tecs_entity_new(world);

        Position pos = {0.0f, 0.0f};
        Velocity vel = {1.0f, 1.0f};

        tecs_set(world, entity, Position_id, &pos, sizeof(Position));
        tecs_set(world, entity, Velocity_id, &vel, sizeof(Velocity));

        /* Print progress every 100k entities */
        if ((i + 1) % 100000 == 0) {
            printf("  Spawned %d entities...\n", i + 1);
        }
    }

    double spawn_elapsed = get_time_ms() - spawn_start;
    printf("[Startup] Spawned %d entities in %.2f ms (%.0f entities/sec)\n",
           ENTITIES_COUNT, spawn_elapsed, ENTITIES_COUNT / (spawn_elapsed / 1000.0));

    /* Verify entity count */
    int entity_count = tecs_world_entity_count(world);
    printf("[Startup] World entity count: %d\n", entity_count);

    printf("\n[Main] Starting benchmark loop...\n");
    printf("Running %d frames per measurement batch...\n\n", FRAMES_PER_BATCH);

    /* Create query once (reuse it) */
    tecs_query_t* query = tecs_query_new(world);
    tecs_query_with(query, Position_id);
    tecs_query_with(query, Velocity_id);
    tecs_query_build(query);

    /* Benchmark loop */
    double start_time = get_time_ms();
    double last_time = start_time;
    int batch_count = 0;

    while (true) {
        /* Run a batch of frames */
        for (int frame = 0; frame < FRAMES_PER_BATCH; frame++) {
            /* Update system - multiply position by velocity */
            tecs_query_iter_t* iter = tecs_query_iter(query);
            while (tecs_query_next(iter)) {
                int count = tecs_iter_count(iter);
                Position* positions = (Position*)tecs_iter_column(iter, 0);
                Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);

                for (int i = 0; i < count; i++) {
                    positions[i].x *= velocities[i].x;
                    positions[i].y *= velocities[i].y;
                }
            }
            tecs_query_iter_free(iter);

            /* Increment world tick */
            tecs_world_update(world);
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

        /* Stop after 50 batches (180,000 frames) */
        if (batch_count >= 50) {
            printf("\n[Main] Benchmark complete!\n");
            break;
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
    tecs_world_free(world);

    printf("\n=== Benchmark completed successfully ===\n");
    return 0;
}
