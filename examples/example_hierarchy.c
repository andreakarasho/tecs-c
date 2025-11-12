/*
 * TinyEcs Hierarchy Example
 *
 * Demonstrates parent-child relationships:
 * - Adding/removing children
 * - Reparenting
 * - Cycle detection
 * - Hierarchy traversal
 * - Query integration
 */

#define TINYECS_IMPLEMENTATION
#include "tinyecs.h"

#include <stdio.h>

/* Components */
TECS_DECLARE_COMPONENT(Name);
struct Name {
    char name[32];
};

/* Visitor function for traversal */
static void print_entity(tecs_world_t* world, tecs_entity_t entity, void* user_data) {
    int* indent = (int*)user_data;

    /* Print indentation */
    for (int i = 0; i < *indent; i++) {
        printf("  ");
    }

    /* Get name if exists */
    Name* name = TECS_GET(world, entity, Name);
    if (name) {
        printf("Entity %llu: %s\n", (unsigned long long)entity, name->name);
    } else {
        printf("Entity %llu\n", (unsigned long long)entity);
    }
}

int main(void) {
    printf("=== TinyEcs Hierarchy Example ===\n\n");

    /* Create world */
    tecs_world_t* world = tecs_world_new();

    /* Register components */
    TECS_COMPONENT_REGISTER(world, Name);

    printf("Hierarchy components auto-registered:\n");
    printf("  tecs_parent_t\n");
    printf("  tecs_children_t\n\n");

    /* ========================================================================
     * Test 1: Basic Parent-Child Relationship
     * ======================================================================== */

    printf("--- Test 1: Basic Parent-Child ---\n");

    tecs_entity_t root = tecs_entity_new(world);
    Name root_name = {0};
    snprintf(root_name.name, sizeof(root_name.name), "Root");
    TECS_SET(world, root, Name, root_name);

    tecs_entity_t child1 = tecs_entity_new(world);
    Name child1_name = {0};
    snprintf(child1_name.name, sizeof(child1_name.name), "Child1");
    TECS_SET(world, child1, Name, child1_name);

    tecs_entity_t child2 = tecs_entity_new(world);
    Name child2_name = {0};
    snprintf(child2_name.name, sizeof(child2_name.name), "Child2");
    TECS_SET(world, child2, Name, child2_name);

    /* Add children */
    TECS_ADD_CHILD(world, root, child1);
    TECS_ADD_CHILD(world, root, child2);

    printf("Root has %d children\n", TECS_CHILD_COUNT(world, root));
    printf("Child1 has parent: %s\n", TECS_HAS_PARENT(world, child1) ? "YES" : "NO");
    printf("Parent of Child1: %llu\n\n", (unsigned long long)TECS_GET_PARENT(world, child1));

    /* ========================================================================
     * Test 2: Hierarchy Traversal
     * ======================================================================== */

    printf("--- Test 2: Hierarchy Traversal ---\n");

    tecs_entity_t grandchild1 = tecs_entity_new(world);
    Name gc1_name = {0};
    snprintf(gc1_name.name, sizeof(gc1_name.name), "Grandchild1");
    TECS_SET(world, grandchild1, Name, gc1_name);

    tecs_entity_t grandchild2 = tecs_entity_new(world);
    Name gc2_name = {0};
    snprintf(gc2_name.name, sizeof(gc2_name.name), "Grandchild2");
    TECS_SET(world, grandchild2, Name, gc2_name);

    TECS_ADD_CHILD(world, child1, grandchild1);
    TECS_ADD_CHILD(world, child1, grandchild2);

    printf("Hierarchy (recursive):\n");
    print_entity(world, root, &(int){0});
    int indent = 1;
    tecs_traverse_children(world, root, print_entity, &indent, true);
    printf("\n");

    /* ========================================================================
     * Test 3: Reparenting
     * ======================================================================== */

    printf("--- Test 3: Reparenting ---\n");

    printf("Before reparenting:\n");
    printf("  Root children: %d\n", TECS_CHILD_COUNT(world, root));
    printf("  Child1 children: %d\n", TECS_CHILD_COUNT(world, child1));

    /* Move child2 to be child of child1 */
    TECS_ADD_CHILD(world, child1, child2);

    printf("After reparenting Child2 to Child1:\n");
    printf("  Root children: %d\n", TECS_CHILD_COUNT(world, root));
    printf("  Child1 children: %d\n", TECS_CHILD_COUNT(world, child1));
    printf("  Child2 parent: %llu\n\n", (unsigned long long)TECS_GET_PARENT(world, child2));

    /* ========================================================================
     * Test 4: Cycle Detection
     * ======================================================================== */

    printf("--- Test 4: Cycle Detection ---\n");

    /* Try to create cycle: root -> child1 -> grandchild1, then grandchild1 -> root */
    printf("Attempting to add Root as child of Grandchild1 (would create cycle)...\n");
    TECS_ADD_CHILD(world, grandchild1, root);

    /* Check if cycle was prevented */
    tecs_entity_t root_parent = TECS_GET_PARENT(world, root);
    if (root_parent == 0) {
        printf("SUCCESS: Cycle prevented! Root has no parent.\n\n");
    } else {
        printf("FAILED: Cycle not detected!\n\n");
    }

    /* ========================================================================
     * Test 5: Hierarchy Depth & Ancestor Queries
     * ======================================================================== */

    printf("--- Test 5: Hierarchy Depth & Queries ---\n");

    printf("Hierarchy depth of Root: %d\n", tecs_get_hierarchy_depth(world, root));
    printf("Hierarchy depth of Child1: %d\n", tecs_get_hierarchy_depth(world, child1));
    printf("Hierarchy depth of Grandchild1: %d\n", tecs_get_hierarchy_depth(world, grandchild1));

    printf("Is Root ancestor of Grandchild1? %s\n",
           tecs_is_ancestor_of(world, root, grandchild1) ? "YES" : "NO");
    printf("Is Grandchild1 descendant of Root? %s\n",
           tecs_is_descendant_of(world, grandchild1, root) ? "YES" : "NO");
    printf("Is Child2 ancestor of Root? %s\n\n",
           tecs_is_ancestor_of(world, child2, root) ? "YES" : "NO");

    /* ========================================================================
     * Test 6: Remove Child
     * ======================================================================== */

    printf("--- Test 6: Remove Child ---\n");

    printf("Before removal: Child1 has %d children\n", TECS_CHILD_COUNT(world, child1));

    TECS_REMOVE_CHILD(world, child1, child2);

    printf("After removing Child2: Child1 has %d children\n", TECS_CHILD_COUNT(world, child1));
    printf("Child2 has parent: %s\n\n", TECS_HAS_PARENT(world, child2) ? "YES" : "NO");

    /* ========================================================================
     * Test 7: Remove All Children
     * ======================================================================== */

    printf("--- Test 7: Remove All Children ---\n");

    printf("Before removal: Root has %d children\n", TECS_CHILD_COUNT(world, root));

    TECS_REMOVE_ALL_CHILDREN(world, root);

    printf("After removal: Root has %d children\n", TECS_CHILD_COUNT(world, root));
    printf("Child1 has parent: %s\n\n", TECS_HAS_PARENT(world, child1) ? "YES" : "NO");

    /* ========================================================================
     * Test 8: Traverse Ancestors
     * ======================================================================== */

    printf("--- Test 8: Traverse Ancestors ---\n");

    /* Rebuild hierarchy for this test */
    TECS_ADD_CHILD(world, root, child1);
    TECS_ADD_CHILD(world, child1, grandchild1);

    printf("Ancestors of Grandchild1:\n");
    tecs_traverse_ancestors(world, grandchild1, print_entity, &(int){1});
    printf("\n");

    /* Cleanup */
    printf("--- Cleanup ---\n");
    tecs_world_free(world);
    printf("World freed successfully.\n");

    printf("\n=== All tests completed successfully ===\n");
    return 0;
}
