#define TINYECS_IMPLEMENTATION
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs.h"
#include "tinyecs_bevy.h"
#include <stdio.h>

typedef struct { float x, y; } Position;
typedef struct { float x, y; } Velocity;

static int system_called = 0;

static void update_system(tbevy_system_ctx_t* ctx, void* user_data) {
    (void)ctx;
    (void)user_data;
    system_called++;
    if (system_called <= 3) {
        printf("update_system called (call #%d)\n", system_called);
    }
}

int main(void) {
    tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_SINGLE);
    
    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, update_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );
    
    printf("Running 10 updates...\n");
    for (int i = 0; i < 10; i++) {
        tbevy_app_update(app);
    }
    printf("System was called %d times\n", system_called);
    
    tbevy_app_free(app);
    return 0;
}
