#define TINYECS_IMPLEMENTATION
#include "tinyecs.h"
#include <stdio.h>

int main(void) {
    tecs_world_t* world = tecs_world_new();
    
    tecs_entity_t parent = tecs_entity_new(world);
    tecs_entity_t child = tecs_entity_new(world);
    
    printf("Before add_child:\n");
    printf("  Parent ID: %llu\n", (unsigned long long)parent);
    printf("  Child ID: %llu\n", (unsigned long long)child);
    
    tecs_add_child(world, parent, child);
    
    printf("\nAfter add_child:\n");
    printf("  Child has parent: %d\n", tecs_has_parent(world, child));
    printf("  Child's parent: %llu\n", (unsigned long long)tecs_get_parent(world, child));
    printf("  Parent has children component: %d\n", tecs_has(world, parent, tecs_get_children_component_id(world)));
    printf("  Parent child count: %d\n", tecs_child_count(world, parent));
    
    const tecs_children_t* children = tecs_get_children(world, parent);
    if (children) {
        printf("  Children component found: count=%d, capacity=%d, entities=%p\n", 
               children->count, children->capacity, (void*)children->entities);
        for (int i = 0; i < children->count; i++) {
            printf("    Child[%d] = %llu\n", i, (unsigned long long)children->entities[i]);
        }
    } else {
        printf("  Children component is NULL!\n");
    }
    
    tecs_world_free(world);
    return 0;
}
