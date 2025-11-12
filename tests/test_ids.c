#define TINYECS_IMPLEMENTATION
#include "tinyecs.h"
#include <stdio.h>

int main(void) {
    printf("Creating world...\n");
    tecs_world_t* world = tecs_world_new();

    printf("\nAfter world_new:\n");
    printf("  parent component id: %llu\n", (unsigned long long)tecs_get_parent_component_id(world));
    printf("  children component id: %llu\n", (unsigned long long)tecs_get_children_component_id(world));

    printf("\nTest passed - component IDs retrieved successfully\n");

    tecs_world_free(world);
    return 0;
}
