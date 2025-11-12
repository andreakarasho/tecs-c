/*
 * TinyEcs Iterator Caching Example
 *
 * Demonstrates two iteration patterns:
 * 1. Allocating iterator (simple but slower)
 * 2. Cached iterator (zero allocation, faster)
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

typedef struct { float x, y; } Position;
typedef struct { float x, y; } Velocity;

#define ENTITY_COUNT 100000
#define ITERATIONS 1000

int main(void) {
    printf("=== TinyEcs Iterator Caching Example ===\n");
    printf("Entities: %d\n", ENTITY_COUNT);
    printf("Iterations: %d\n\n", ITERATIONS);

    tecs_world_t* world = tecs_world_new();

    tecs_component_id_t pos_id = tecs_register_component(world, "Position", sizeof(Position));
    tecs_component_id_t vel_id = tecs_register_component(world, "Velocity", sizeof(Velocity));

    /* Spawn entities */
    printf("Spawning entities...\n");
    for (int i = 0; i < ENTITY_COUNT; i++) {
        tecs_entity_t e = tecs_entity_new(world);
        Position pos = {(float)i, (float)i};
        Velocity vel = {1.0f, 1.0f};
        tecs_set(world, e, pos_id, &pos, sizeof(Position));
        tecs_set(world, e, vel_id, &vel, sizeof(Velocity));
    }

    /* Create query */
    tecs_query_t* query = tecs_query_new(world);
    tecs_query_with(query, pos_id);
    tecs_query_with(query, vel_id);
    tecs_query_build(query);

    printf("Created query with %d entities\n\n", ENTITY_COUNT);

    /* ========================================================================
     * Method 1: Allocating iterator (old style)
     * Allocates new iterator on every frame - slower
     * ======================================================================= */

    printf("=== Method 1: Allocating iterator ===\n");
    double start = get_time_ms();

    for (int frame = 0; frame < ITERATIONS; frame++) {
        tecs_query_iter_t* iter = tecs_query_iter(query);  /* Allocates! */

        while (tecs_query_next(iter)) {
            int count = tecs_iter_count(iter);
            Position* positions = (Position*)tecs_iter_column(iter, 0);
            Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);

            for (int i = 0; i < count; i++) {
                positions[i].x += velocities[i].x;
                positions[i].y += velocities[i].y;
            }
        }

        tecs_query_iter_free(iter);  /* Frees! */
    }

    double elapsed1 = get_time_ms() - start;
    printf("Time: %.2f ms\n", elapsed1);
    printf("Per iteration: %.4f ms\n", elapsed1 / ITERATIONS);
    printf("Total updates: %d\n\n", ENTITY_COUNT * ITERATIONS);

    /* ========================================================================
     * Method 2: Cached iterator (new style)
     * Reuses same iterator - zero allocation overhead
     * ======================================================================= */

    printf("=== Method 2: Cached iterator (zero allocation) ===\n");
    start = get_time_ms();

    /* Allocate iterator ONCE */
    tecs_query_iter_t cached_iter;

    for (int frame = 0; frame < ITERATIONS; frame++) {
        tecs_query_iter_init(&cached_iter, query);  /* Reset, no allocation! */

        while (tecs_query_next(&cached_iter)) {
            int count = tecs_iter_count(&cached_iter);
            Position* positions = (Position*)tecs_iter_column(&cached_iter, 0);
            Velocity* velocities = (Velocity*)tecs_iter_column(&cached_iter, 1);

            for (int i = 0; i < count; i++) {
                positions[i].x += velocities[i].x;
                positions[i].y += velocities[i].y;
            }
        }
        /* No free needed - iterator is on stack! */
    }

    double elapsed2 = get_time_ms() - start;
    printf("Time: %.2f ms\n", elapsed2);
    printf("Per iteration: %.4f ms\n", elapsed2 / ITERATIONS);
    printf("Total updates: %d\n\n", ENTITY_COUNT * ITERATIONS);

    /* ========================================================================
     * Performance Comparison
     * ======================================================================= */

    printf("=== Performance Comparison ===\n");
    printf("Allocating iterator: %.2f ms\n", elapsed1);
    printf("Cached iterator:     %.2f ms\n", elapsed2);
    printf("Speedup:             %.2fx faster\n", elapsed1 / elapsed2);
    printf("Overhead saved:      %.2f ms (%.1f%%)\n\n",
           elapsed1 - elapsed2,
           ((elapsed1 - elapsed2) / elapsed1) * 100.0);

    /* ========================================================================
     * Memory Comparison
     * ======================================================================= */

    printf("=== Memory Comparison ===\n");
    printf("Allocating iterator:\n");
    printf("  Allocations per frame: 1\n");
    printf("  Total allocations:     %d\n", ITERATIONS);
    printf("  Bytes allocated:       %zu\n\n", ITERATIONS * sizeof(tecs_query_iter_t));

    printf("Cached iterator:\n");
    printf("  Allocations per frame: 0\n");
    printf("  Total allocations:     0\n");
    printf("  Stack space used:      %zu bytes\n\n", sizeof(tecs_query_iter_t));

    /* Cleanup */
    tecs_query_free(query);
    tecs_world_free(world);

    printf("=== Benchmark completed ===\n");
    return 0;
}
