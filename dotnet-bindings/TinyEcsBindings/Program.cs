using TinyEcsBindings;
using static TinyEcsBindings.TinyEcs;
using static TinyEcsBindings.TinyEcsBevy;
using SystemContext = TinyEcsBindings.TinyEcsBevy.SystemContext;

// Component types
struct Position
{
    public float X;
    public float Y;
}

struct Velocity
{
    public float X;
    public float Y;
}

struct GameState
{
    public int Score;
    public int Frame;
}

unsafe class Program
{
    // Store delegates to prevent garbage collection
    private static SystemFunction? s_startupSystem;
    private static SystemFunction? s_updateSystem;
    private static ulong s_gameStateId;

    static void Main()
    {
        Console.WriteLine("=== TinyECS C# Bindings Example ===\n");

        PerformanceTest();
        return;

        // Example 1: Basic ECS usage
        Console.WriteLine("--- Example 1: Basic ECS Operations ---");
        BasicEcsExample();

        Console.WriteLine("\n--- Example 2: Query System ---");
        QueryExample();

        Console.WriteLine("\n--- Example 3: Bevy-style Application ---");
        BevyStyleExample();
    }

    static void BasicEcsExample()
    {
        // Create a new world
        var world = tecs_world_new();
        if (world.Handle == IntPtr.Zero)
        {
            Console.WriteLine("Failed to create world!");
            return;
        }

        try
        {
            // Register components
            var positionId = RegisterComponent<Position>(world, "Position");
            var velocityId = RegisterComponent<Velocity>(world, "Velocity");

            Console.WriteLine($"Registered Position component: {positionId.Value}");
            Console.WriteLine($"Registered Velocity component: {velocityId.Value}");

            // Create entities
            var entity1 = tecs_entity_new(world);
            var entity2 = tecs_entity_new(world);

            Console.WriteLine($"\nCreated entity1: {entity1.Value} (index: {entity1.Index}, gen: {entity1.Generation})");
            Console.WriteLine($"Created entity2: {entity2.Value} (index: {entity2.Index}, gen: {entity2.Generation})");

            // Add components to entities
            var pos1 = new Position { X = 10.0f, Y = 20.0f };
            var vel1 = new Velocity { X = 1.0f, Y = 2.0f };

            Set(world, entity1, positionId, pos1);
            Set(world, entity1, velocityId, vel1);

            var pos2 = new Position { X = 30.0f, Y = 40.0f };
            Set(world, entity2, positionId, pos2);

            Console.WriteLine("\nAdded components to entities");

            // Query components
            var entity1Pos = Get<Position>(world, entity1, positionId);
            var entity1Vel = Get<Velocity>(world, entity1, velocityId);

            if (entity1Pos != null && entity1Vel != null)
            {
                Console.WriteLine($"Entity1 Position: ({entity1Pos->X}, {entity1Pos->Y})");
                Console.WriteLine($"Entity1 Velocity: ({entity1Vel->X}, {entity1Vel->Y})");
            }

            // Check component existence
            var hasVelocity = tecs_has(world, entity2, velocityId);
            Console.WriteLine($"\nEntity2 has velocity: {hasVelocity}");

            // Update component
            if (entity1Pos != null)
            {
                entity1Pos->X += 5.0f;
                entity1Pos->Y += 5.0f;
                Console.WriteLine($"Updated Entity1 Position: ({entity1Pos->X}, {entity1Pos->Y})");
            }

            // World stats
            var entityCount = tecs_world_entity_count(world);
            var tick = tecs_world_tick(world);
            Console.WriteLine($"\nWorld entity count: {entityCount}");
            Console.WriteLine($"World tick: {tick.Value}");

            // Delete an entity
            tecs_entity_delete(world, entity2);
            Console.WriteLine($"\nDeleted entity2");
            Console.WriteLine($"Entity2 exists: {tecs_entity_exists(world, entity2)}");
            Console.WriteLine($"Entity count after deletion: {tecs_world_entity_count(world)}");
        }
        finally
        {
            // Clean up
            tecs_world_free(world);
            Console.WriteLine("\nWorld cleaned up");
        }
    }

    static void QueryExample()
    {
        // Create a new world
        var world = tecs_world_new();
        if (world.Handle == IntPtr.Zero)
        {
            Console.WriteLine("Failed to create world!");
            return;
        }

        try
        {
            // Register components
            var positionId = RegisterComponent<Position>(world, "Position");
            var velocityId = RegisterComponent<Velocity>(world, "Velocity");

            Console.WriteLine("Creating entities with components...");

            // Create some entities with both Position and Velocity
            for (int i = 0; i < 5; i++)
            {
                var entity = tecs_entity_new(world);
                var pos = new Position { X = i * 10.0f, Y = i * 20.0f };
                var vel = new Velocity { X = 1.0f, Y = 2.0f };
                Set(world, entity, positionId, pos);
                Set(world, entity, velocityId, vel);
            }

            // Create some entities with only Position
            for (int i = 0; i < 3; i++)
            {
                var entity = tecs_entity_new(world);
                var pos = new Position { X = 100.0f + i, Y = 200.0f + i };
                Set(world, entity, positionId, pos);
            }

            Console.WriteLine($"Created {tecs_world_entity_count(world)} entities");

            // Build a query for entities with both Position and Velocity
            Console.WriteLine("\nQuerying entities with Position AND Velocity:");
            var query = tecs_query_new(world);
            tecs_query_with(query, positionId);
            tecs_query_with(query, velocityId);
            tecs_query_build(query);

            // Iterate through matching entities
            QueryIter iter;
            tecs_query_iter_init(&iter, query);
            int matchCount = 0;
            while (tecs_iter_next(&iter))
            {
                var count = tecs_iter_count(&iter);
                var entities = tecs_iter_entities(&iter);
                var positions = IterColumn<Position>(&iter, 0);
                var velocities = IterColumn<Velocity>(&iter, 1);

                for (int i = 0; i < count; i++)
                {
                    Console.WriteLine($"  Entity {entities[i].Value}: Pos({positions[i].X}, {positions[i].Y}), Vel({velocities[i].X}, {velocities[i].Y})");
                    matchCount++;
                }
            }
            Console.WriteLine($"Found {matchCount} entities with Position and Velocity");

            // Free the query
            tecs_query_free(query);

            // Build another query for entities with Position but WITHOUT Velocity
            Console.WriteLine("\nQuerying entities with Position but WITHOUT Velocity:");
            var query2 = tecs_query_new(world);
            tecs_query_with(query2, positionId);
            tecs_query_without(query2, velocityId);
            tecs_query_build(query2);

            QueryIter iter2;
            tecs_query_iter_init(&iter2, query2);
            matchCount = 0;
            while (tecs_iter_next(&iter2))
            {
                var count = tecs_iter_count(&iter2);
                var entities = tecs_iter_entities(&iter2);
                var positions = IterColumn<Position>(&iter2, 0);

                for (int i = 0; i < count; i++)
                {
                    Console.WriteLine($"  Entity {entities[i].Value}: Pos({positions[i].X}, {positions[i].Y})");
                    matchCount++;
                }
            }
            Console.WriteLine($"Found {matchCount} entities with Position but no Velocity");

            tecs_query_free(query2);
        }
        finally
        {
            // Clean up
            tecs_world_free(world);
            Console.WriteLine("\nWorld cleaned up");
        }
    }

    static void StartupSystem(SystemContext* ctx, void* userData)
    {
        Console.WriteLine("Startup system executed!");

        var state = GetResourceMut<GameState>(ctx->app, s_gameStateId);
        if (state != null)
        {
            state->Score = 100;
            state->Frame = 0;
            Console.WriteLine($"  Initialized: Score = {state->Score}, Frame = {state->Frame}");
        }
    }

    static void UpdateSystem(SystemContext* ctx, void* userData)
    {
        var state = GetResourceMut<GameState>(ctx->app, s_gameStateId);
        if (state != null)
        {
            state->Frame++;
            state->Score += 10;
            Console.WriteLine($"  Frame {state->Frame}: Score = {state->Score}");
        }
    }

    static void BevyStyleExample()
    {
        // Create a Bevy-style application
        var app = tbevy_app_new(ThreadingMode.SingleThreaded);
        if (app.Handle == IntPtr.Zero)
        {
            Console.WriteLine("Failed to create app!");
            return;
        }

        try
        {
            var world = tbevy_app_world(app);

            // Register and insert resource
            s_gameStateId = RegisterResourceType<GameState>("GameState");
            var initialState = new GameState { Score = 0, Frame = 0 };
            InsertResource(app, s_gameStateId, initialState);

            Console.WriteLine("Created Bevy-style app with GameState resource");

            // Add a startup system
            var startupStage = tbevy_stage_default(StageId.Startup);

            // Store delegate in static field to prevent GC
            s_startupSystem = StartupSystem;
            var startupBuilder = tbevy_app_add_system(app, s_startupSystem, null);
            tbevy_system_in_stage(startupBuilder, startupStage);
            tbevy_system_build(startupBuilder);

            // Add an update system
            var updateStage = tbevy_stage_default(StageId.Update);

            // Store delegate in static field to prevent GC
            s_updateSystem = UpdateSystem;
            var updateBuilder = tbevy_app_add_system(app, s_updateSystem, null);
            tbevy_system_in_stage(updateBuilder, updateStage);
            tbevy_system_build(updateBuilder);

            // Run startup
            Console.WriteLine("\nRunning startup systems:");
            tbevy_app_run_startup(app);

            // Run a few updates
            Console.WriteLine("\nRunning update systems:");
            for (int i = 0; i < 3; i++)
            {
                tbevy_app_update(app);
            }

            // Check final state
            var finalState = GetResource<GameState>(app, s_gameStateId);
            if (finalState != null)
            {
                Console.WriteLine($"\nFinal state - Frame: {finalState->Frame}, Score: {finalState->Score}");
            }
            else
            {
                Console.WriteLine("\nWARNING: Could not retrieve final GameState");
            }
        }
        finally
        {
            // Clean up
            tbevy_app_free(app);
            Console.WriteLine("\nApp cleaned up");
        }
    }

    static void Setup(SystemContext* ctx, void* userData)
    {
        const int COUNT = 1_000;

        var commands = ctx->commands;
        // tbevy_commands_init(&commands, ctx->app);
        Console.WriteLine("commands {0}", (IntPtr)commands);

        var posId = ComponentStorage.GetComponentId<Position>(ctx->world);
        var velId = ComponentStorage.GetComponentId<Velocity>(ctx->world);

        Console.WriteLine("posId {0}", posId.Value);
        Console.WriteLine("velId {0}", velId.Value);

        for (int i = 0; i < COUNT; i++)
        {
            var entity = tbevy_commands_spawn(commands);
            EntityInsert(&entity, posId, new Position());
            EntityInsert(&entity, velId, new Velocity());
        }

        tbevy_commands_apply(commands);

        Console.WriteLine("Setup completed");
    }

    static void Performance(SystemContext* ctx, void* userData)
    {
        var posId = ComponentStorage.GetComponentId<Position>(ctx->world);
        var velId = ComponentStorage.GetComponentId<Velocity>(ctx->world);

        var query = tecs_query_new(ctx->world);
        tecs_query_with(query, posId);
        tecs_query_with(query, velId);
        tecs_query_changed(query, velId);
        tecs_query_build(query);

        QueryIter iter;
        tecs_query_iter_init(&iter, query);

        // var worldTick = tecs_world_tick(ctx->world);
        while (tecs_iter_next(&iter))
        {
            var count = tecs_iter_count(&iter);
            var pos = IterColumn<Position>(&iter, 0);
            var vel = IterColumn<Velocity>(&iter, 1);
            // var velChangedTicks = tecs_iter_changed_ticks(&iter, 1);

            for (var i = 0; i < count; ++i)
            {
                // if (velChangedTicks[i].Value < worldTick.Value)
                // {
                //     continue;
                // }
                pos[i].X *= vel[i].X;
                pos[i].Y *= vel[i].Y;
            }

        }

        tecs_query_free(query);
    }

    static void PerformanceTest()
    {
        var app = tbevy_app_new(ThreadingMode.SingleThreaded);
        var world = tbevy_app_world(app);

        ComponentStorage.Register<Position>(world);
        ComponentStorage.Register<Velocity>(world);

        var stageStartup = tbevy_stage_default(StageId.Startup);
        var stageUpdate = tbevy_stage_default(StageId.Update);

        var builder = tbevy_app_add_system(app, Setup, null);
        builder = tbevy_system_in_stage(builder, stageStartup);
        tbevy_system_build(builder);

        builder = tbevy_app_add_system(app, Performance, null);
        builder = tbevy_system_in_stage(builder, stageUpdate);
        tbevy_system_build(builder);


        var sw = System.Diagnostics.Stopwatch.StartNew();
        var current = 0L;
        var last = 0L;

        for (var i = 0; i < 50; ++i)
        {
            for (var j = 0; j < 3600; ++j)
                tbevy_app_update(app);

            var elapsed = sw.ElapsedMilliseconds;
            last = current;
            current = elapsed;
            Console.WriteLine($"Iteration {i}: {current - last} ms");
        }

        tbevy_app_free(app);
    }
}

static class ComponentStorage
{
    private static Dictionary<IntPtr, Dictionary<Type, ComponentId>> _components = new ();

    public static void Register<T>(World world) where T : unmanaged
    {
        if (!_components.ContainsKey(world.Handle))
            _components[world.Handle] = new ();

        var name = typeof(T).ToString();
        var id = RegisterComponent<T>(world, name);
        _components[world.Handle][typeof(T)] = id;
    }

    public static ComponentId GetComponentId<T>(World world)
    {
        var type = typeof(T);
        if (_components.TryGetValue(world.Handle, out var componentIds))
            return componentIds[type];
        throw new ArgumentException($"Component {type} is not registered.");
    }
}
