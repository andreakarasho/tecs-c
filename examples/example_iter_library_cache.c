/*
 * TinyEcs Library-Side Iterator Caching Example
 *
 * Demonstrates three iteration patterns:
 * 1. Allocating iterator (simple but slower)
 * 2. User-side cached iterator (zero allocation, manual)
 * 3. Library-side cached iterator (zero allocation, automatic)
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

/* Components - using macro style */
TECS_DECLARE_COMPONENT(Position);
struct Position { float x, y; };

TECS_DECLARE_COMPONENT(Velocity);
struct Velocity { float x, y; };

#define ENTITY_COUNT 100000
#define ITERATIONS 1000

int main(void) {
    printf("=== TinyEcs Library-Side Iterator Caching Example ===\n");
    printf("Entities: %d\n", ENTITY_COUNT);
    printf("Iterations: %d\n\n", ITERATIONS);

    tecs_world_t* world = tecs_world_new();

    /* Register components using macro */
    TECS_COMPONENT_REGISTER(world, Position);
    TECS_COMPONENT_REGISTER(world, Velocity);

    /* Spawn entities */
    printf("Spawning entities...\n");
    for (int i = 0; i < ENTITY_COUNT; i++) {
        tecs_entity_t e = tecs_entity_new(world);
        Position pos = {(float)i, (float)i};
        Velocity vel = {1.0f, 1.0f};
        TECS_SET(world, e, Position, pos);
        TECS_SET(world, e, Velocity, vel);
    }

    /* Create query */
    tecs_query_t* query = tecs_query_new(world);
    TECS_QUERY_WITH(query, Position);
    TECS_QUERY_WITH(query, Velocity);
    tecs_query_build(query);

    printf("Created query with %d entities\n\n", ENTITY_COUNT);

    /* ========================================================================
     * Method 1: Allocating iterator
     * ======================================================================= */

    printf("=== Method 1: Allocating iterator ===\n");
    double start = get_time_ms();

    for (int frame = 0; frame < ITERATIONS; frame++) {
        tecs_query_iter_t* iter = tecs_query_iter(query);

        while (tecs_iter_next(iter)) {
            int count = tecs_iter_count(iter);
            Position* positions = (Position*)tecs_iter_column(iter, 0);
            Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);

            for (int i = 0; i < count; i++) {
                positions[i].x += velocities[i].x;
                positions[i].y += velocities[i].y;
            }
        }

        tecs_query_iter_free(iter);
    }

    double elapsed1 = get_time_ms() - start;
    printf("Time: %.2f ms (%.4f ms/iter)\n\n", elapsed1, elapsed1 / ITERATIONS);

    /* ========================================================================
     * Method 2: User-side cached iterator
     * ======================================================================= */

    printf("=== Method 2: User-side cached iterator ===\n");
    start = get_time_ms();

    tecs_query_iter_t user_cached_iter;

    for (int frame = 0; frame < ITERATIONS; frame++) {
        tecs_query_iter_init(&user_cached_iter, query);

        while (tecs_iter_next(&user_cached_iter)) {
            int count = tecs_iter_count(&user_cached_iter);
            Position* positions = (Position*)tecs_iter_column(&user_cached_iter, 0);
            Velocity* velocities = (Velocity*)tecs_iter_column(&user_cached_iter, 1);

            for (int i = 0; i < count; i++) {
                positions[i].x += velocities[i].x;
                positions[i].y += velocities[i].y;
            }
        }
    }

    double elapsed2 = get_time_ms() - start;
    printf("Time: %.2f ms (%.4f ms/iter)\n\n", elapsed2, elapsed2 / ITERATIONS);

    /* ========================================================================
     * Method 3: Library-side cached iterator (NEW!)
     * ======================================================================= */

    printf("=== Method 3: Library-side cached iterator ===\n");
    start = get_time_ms();

    for (int frame = 0; frame < ITERATIONS; frame++) {
        /* Query manages its own cached iterator! */
        tecs_query_iter_t* iter = tecs_query_iter_cached(query);

        while (tecs_iter_next(iter)) {
            int count = tecs_iter_count(iter);
            Position* positions = (Position*)tecs_iter_column(iter, 0);
            Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);

            for (int i = 0; i < count; i++) {
                positions[i].x += velocities[i].x;
                positions[i].y += velocities[i].y;
            }
        }
        /* No free needed - managed by query! */
    }

    double elapsed3 = get_time_ms() - start;
    printf("Time: %.2f ms (%.4f ms/iter)\n\n", elapsed3, elapsed3 / ITERATIONS);

    /* ========================================================================
     * Performance Comparison
     * ======================================================================= */

    printf("=== Performance Comparison ===\n");
    printf("Method 1 (Allocating):        %.2f ms\n", elapsed1);
    printf("Method 2 (User-cached):       %.2f ms (%.2fx faster)\n",
           elapsed2, elapsed1 / elapsed2);
    printf("Method 3 (Library-cached):    %.2f ms (%.2fx faster)\n",
           elapsed3, elapsed1 / elapsed3);
    printf("\n");

    /* ========================================================================
     * Code Complexity Comparison
     * ======================================================================= */

    printf("=== Code Complexity ===\n");
    printf("Method 1 (Allocating):\n");
    printf("  - tecs_query_iter(query)\n");
    printf("  - tecs_query_iter_free(iter)\n");
    printf("  - 2 function calls, allocates memory\n\n");

    printf("Method 2 (User-cached):\n");
    printf("  - Declare: tecs_query_iter_t cached_iter;\n");
    printf("  - tecs_query_iter_init(&cached_iter, query)\n");
    printf("  - 1 function call, manual management\n\n");

    printf("Method 3 (Library-cached):\n");
    printf("  - tecs_query_iter_cached(query)\n");
    printf("  - 1 function call, fully automatic!\n");
    printf("  - Same performance as Method 2, simpler API\n\n");

    /* ========================================================================
     * Memory Layout
     * ======================================================================= */

    printf("=== Memory Layout ===\n");
    printf("Iterator size: %zu bytes\n", sizeof(tecs_query_iter_t));
    printf("\n");

    printf("Method 1 (Allocating):\n");
    printf("  - %d heap allocations\n", ITERATIONS);
    printf("  - %zu bytes allocated per iteration\n", sizeof(tecs_query_iter_t));
    printf("  - Total: %zu bytes allocated\n\n", ITERATIONS * sizeof(tecs_query_iter_t));

    printf("Method 2 (User-cached):\n");
    printf("  - 0 heap allocations\n");
    printf("  - %zu bytes on user's stack\n", sizeof(tecs_query_iter_t));
    printf("  - User manages lifetime\n\n");

    printf("Method 3 (Library-cached):\n");
    printf("  - 0 heap allocations\n");
    printf("  - %zu bytes embedded in query structure\n", sizeof(tecs_query_iter_t));
    printf("  - Library manages lifetime\n");
    printf("  - Query struct size: %zu bytes\n\n", sizeof(tecs_query_t));

    /* ========================================================================
     * Recommendations
     * ======================================================================= */

    printf("=== Recommendations ===\n");
    printf("Use Method 1 (Allocating) when:\n");
    printf("  - Prototyping or learning\n");
    printf("  - Infrequent iteration (< 1000/sec)\n");
    printf("  - Simplicity is more important than performance\n\n");

    printf("Use Method 2 (User-cached) when:\n");
    printf("  - Need control over iterator lifetime\n");
    printf("  - Passing iterators between functions\n");
    printf("  - Multiple iterators per query needed\n\n");

    printf("Use Method 3 (Library-cached) when:\n");
    printf("  - Hot loops (> 1000/sec)\n");
    printf("  - Want zero-allocation with simple API\n");
    printf("  - One iterator per query is sufficient\n");
    printf("  - Production code (best balance of simplicity + performance)\n\n");

    /* Cleanup */
    tecs_query_free(query);
    tecs_world_free(world);

    printf("=== Benchmark completed ===\n");
    return 0;
}
