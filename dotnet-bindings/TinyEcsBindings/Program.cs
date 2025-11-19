using TinyEcsBindings;
using static TinyEcsBindings.TinyEcs;
using static TinyEcsBindings.TinyEcsBevy;
using SystemContext = TinyEcsBindings.TinyEcsBevy.SystemContext;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

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

// Managed component types (for demonstration)
struct Name
{
    public string Value { get; set; }

    public override string ToString() => Value;
}

struct Description
{
    public string Text { get; set; }
    public List<string> Tags { get; set; }

    public Description(string text, List<string> tags)
    {
        Text = text;
        Tags = tags;
    }

    public override string ToString() => $"{Text} [{string.Join(", ", Tags)}]";
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

        // Example 1: Basic ECS usage
        Console.WriteLine("--- Example 1: Basic ECS Operations ---");
        BasicEcsExample();

        Console.WriteLine("\n--- Example 2: Query System ---");
        QueryExample();

        Console.WriteLine("\n--- Example 3: Bevy-style Application ---");
        BevyStyleExample();

        Console.WriteLine("\n--- Example 4: Pluggable Storage System ---");
        PluggableStorageExample();

        Console.WriteLine("\n--- Example 5: Managed Components ---");
        ManagedComponentExample();

        Console.WriteLine("\n--- Example 6: TinyWorld Wrapper API ---");
        EcsWorldExample.Run();

        Console.WriteLine("\n--- Example 7: TinyApp Bevy-Style Wrapper API ---");
        TinyAppExample.Run();

        Console.WriteLine("\n--- Example 8: Auto-Registration (No Manual Registration) ---");
        AutoRegistrationExample.Run();

        Console.WriteLine("\n--- Example 9: Bevy-Style Query API ---");
        BevyQueryExample.Run();

        Console.WriteLine("\n--- Example 10: Performance Test ---");
        PerformanceTest();
    }

    static void BasicEcsExample()
    {
        using var world = new TinyWorld();

        // Register components
        var positionId = world.RegisterComponent<Position>();
        var velocityId = world.RegisterComponent<Velocity>();

        Console.WriteLine($"Registered Position component: {positionId.Id.Value}");
        Console.WriteLine($"Registered Velocity component: {velocityId.Id.Value}");

        // Create entities
        var entity1 = world.Create();
        var entity2 = world.Create();

        Console.WriteLine($"\nCreated entity1: {entity1}");
        Console.WriteLine($"Created entity2: {entity2}");

        // Add components to entities
        world.Set(entity1, positionId, new Position { X = 10.0f, Y = 20.0f });
        world.Set(entity1, velocityId, new Velocity { X = 1.0f, Y = 2.0f });
        world.Set(entity2, positionId, new Position { X = 30.0f, Y = 40.0f });

        Console.WriteLine("\nAdded components to entities");

        // Query components
        ref var entity1Pos = ref world.Get(entity1, positionId);
        ref var entity1Vel = ref world.Get(entity1, velocityId);

        Console.WriteLine($"Entity1 Position: ({entity1Pos.X}, {entity1Pos.Y})");
        Console.WriteLine($"Entity1 Velocity: ({entity1Vel.X}, {entity1Vel.Y})");

        // Check component existence
        var hasVelocity = world.Has(entity2, velocityId);
        Console.WriteLine($"\nEntity2 has velocity: {hasVelocity}");

        // Update component
        entity1Pos.X += 5.0f;
        entity1Pos.Y += 5.0f;
        Console.WriteLine($"Updated Entity1 Position: ({entity1Pos.X}, {entity1Pos.Y})");

        // World stats
        Console.WriteLine($"\nWorld entity count: {world.Count}");
        Console.WriteLine($"World tick: {world.Tick}");

        // Delete an entity
        world.Delete(entity2);
        Console.WriteLine($"\nDeleted entity2");
        Console.WriteLine($"Entity2 exists: {world.Exists(entity2)}");
        Console.WriteLine($"Entity count after deletion: {world.Count}");

        Console.WriteLine("\nWorld cleaned up");
    }

    static void QueryExample()
    {
        using var world = new TinyWorld();

        // Register components
        var positionId = world.RegisterComponent<Position>();
        var velocityId = world.RegisterComponent<Velocity>();

        Console.WriteLine("Creating entities with components...");

        // Create some entities with both Position and Velocity
        for (int i = 0; i < 5; i++)
        {
            var entity = world.Create();
            world.Set(entity, positionId, new Position { X = i * 10.0f, Y = i * 20.0f });
            world.Set(entity, velocityId, new Velocity { X = 1.0f, Y = 2.0f });
        }

        // Create some entities with only Position
        for (int i = 0; i < 3; i++)
        {
            var entity = world.Create();
            world.Set(entity, positionId, new Position { X = 100.0f + i, Y = 200.0f + i });
        }

        Console.WriteLine($"Created {world.Count} entities");

        // Query entities with both Position and Velocity
        Console.WriteLine("\nQuerying entities with Position AND Velocity:");
        var query = world.Query()
            .With(positionId)
            .With(velocityId)
            .Iter();

        int matchCount = 0;
        while (query.MoveNext())
        {
            var positions = query.Column(positionId);
            var velocities = query.Column(velocityId);

            for (int i = 0; i < query.Count; i++)
            {
                Console.WriteLine($"  Entity {i}: Pos({positions[i].X}, {positions[i].Y}), Vel({velocities[i].X}, {velocities[i].Y})");
                matchCount++;
            }
        }
        Console.WriteLine($"Found {matchCount} entities with Position and Velocity");

        query.Dispose();

        // Query entities with Position but WITHOUT Velocity
        Console.WriteLine("\nQuerying entities with Position but WITHOUT Velocity:");
        query = world.Query()
            .With(positionId)
            .Without(velocityId)
            .Iter();

        matchCount = 0;
        while (query.MoveNext())
        {
            var positions = query.Column(positionId);

            for (int i = 0; i < query.Count; i++)
            {
                Console.WriteLine($"  Entity {i}: Pos({positions[i].X}, {positions[i].Y})");
                matchCount++;
            }
        }
        Console.WriteLine($"Found {matchCount} entities with Position but no Velocity");

        query.Dispose();

        Console.WriteLine("\nWorld cleaned up");
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

    static void ManagedComponentExample()
    {
        using var world = new TinyWorld();

        // Register managed components
        var nameId = world.RegisterComponent<Name>();
        var descId = world.RegisterComponent<Description>();

        Console.WriteLine($"Registered managed components: Name={nameId.Id.Value}, Description={descId.Id.Value}\n");

        // Create entities
        var player = world.Create();
        var enemy = world.Create();

        // Add managed components
        world.Set(player, nameId, new Name { Value = "Hero" });
        world.Set(player, descId, new Description
        {
            Text = "The main character",
            Tags = new List<string> { "player", "hero" }
        });
        world.Set(enemy, nameId, new Name { Value = "Goblin" });

        Console.WriteLine("✓ Added managed components\n");

        // Get component reference
        ref var retrievedName = ref world.Get(player, nameId);
        Console.WriteLine($"✓ Retrieved via Get: {retrievedName.Value ?? "(null)"}");

        Console.WriteLine("=== Managed Components Benefits ===");
        Console.WriteLine("✓ Single GCHandle per column");
        Console.WriteLine("✓ No boxing/unboxing");
        Console.WriteLine("✓ Direct object references");
        Console.WriteLine("✓ Modify in place");
    }

    static void PluggableStorageExample()
    {
        Console.WriteLine("Demonstrating pluggable storage system...\n");

        var world = tecs_world_new();
        if (world.Handle == IntPtr.Zero)
        {
            Console.WriteLine("Failed to create world!");
            return;
        }

        try
        {
            // Register a component with custom managed storage
            ManagedStorage.ManagedStorageProvider<Name>? nameProvider = null;
            var nameId = ManagedStorage.RegisterManagedComponent(world, "Name", out nameProvider);

            using (nameProvider)
            {
                Console.WriteLine($"✓ Registered managed component 'Name' with ID: {nameId.Value}");
                Console.WriteLine($"✓ Storage provider uses pinned C# arrays");
                Console.WriteLine($"✓ Can store reference types without boxing");

                // Also register regular unmanaged component for comparison
                var posId = RegisterComponent<Position>(world, "Position");
                Console.WriteLine($"✓ Registered native component 'Position' with ID: {posId.Value}");

                Console.WriteLine("\n=== Pluggable Storage Benefits ===");
                Console.WriteLine("✓ VTable-based storage provider interface");
                Console.WriteLine("✓ Custom allocators for component data");
                Console.WriteLine("✓ Zero overhead for native storage (fast path)");
                Console.WriteLine("✓ Enables managed C# types in ECS");
                Console.WriteLine("✓ Storage-agnostic iteration API");
                Console.WriteLine("✓ Full backward compatibility");

                Console.WriteLine("\nNOTE: Full managed component example requires additional");
                Console.WriteLine("helper methods for setting/getting managed components via");
                Console.WriteLine("the standard tecs_set/tecs_get API. The storage provider");
                Console.WriteLine("infrastructure is complete and functional!");
            }
        }
        finally
        {
            tecs_world_free(world);
            Console.WriteLine("\nWorld cleaned up");
        }
    }

    static void PerformanceSetupSystem(SystemContext* ctx, void* userData)
    {
        const int COUNT = 524288 * 2;

        var commands = ctx->commands;
        Console.WriteLine("Setting up {0} entities...", COUNT);

        var posId = PerformanceTestState.PositionId.Id;
        var velId = PerformanceTestState.VelocityId.Id;
        var nameId = PerformanceTestState.NameId.Id;

        for (int i = 0; i < COUNT; i++)
        {
            var entity = tbevy_commands_spawn(commands);
            EntityInsert(&entity, posId, new Position() { X = 1.0f, Y = 1.0f });
            EntityInsert(&entity, velId, new Velocity() { X = 1.0001f, Y = 1.0001f });
            EntityInsertManaged(&entity, nameId, new Name() { Value = $"Entity_{i}" });
        }

        tbevy_commands_apply(commands);
        Console.WriteLine("Setup completed - {0} entities created", COUNT);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Span<T> Column<T>(QueryIter* iter, int columnIndex) where T : notnull
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            return ManagedStorage.GetManagedSpan<T>(iter, columnIndex);
        }

        var count = tecs_iter_count(iter);
        var ptr = tecs_iter_column(iter, columnIndex);
        return new Span<T>(ptr, count);
    }

    static void PerformanceUpdateSystem(SystemContext* ctx, void* userData)
    {
        var posId = PerformanceTestState.PositionId.Id;
        var velId = PerformanceTestState.VelocityId.Id;

        var query = tecs_query_new(ctx->world);
        tecs_query_with(query, posId);
        tecs_query_with(query, velId);
        tecs_query_build(query);

        var iter = tecs_query_iter_cached(query);

        while (tecs_iter_next(iter))
        {
            var count = tecs_iter_count(iter);
            var pos = Column<Position>(iter, 0);
            var vel = Column<Velocity>(iter, 1);

            for (var i = 0; i < count; ++i)
            {
                pos[i].X *= vel[i].X;
                pos[i].Y *= vel[i].Y;
            }
        }

        tecs_query_free(query);
    }

    static void PerformanceTest()
    {
        using var app = new TinyApp(ThreadingMode.SingleThreaded);

        // Register components
        var posId = app.World.RegisterComponent<Position>();
        var velId = app.World.RegisterComponent<Velocity>();
        var nameId = app.World.RegisterComponent<Name>();

        // Store component IDs for systems to use
        PerformanceTestState.PositionId = posId;
        PerformanceTestState.VelocityId = velId;
        PerformanceTestState.NameId = nameId;

        // Add setup system to startup stage
        var setupDelegate = new SystemFunction(PerformanceSetupSystem);
        app.AddSystem(setupDelegate, null)
            .InStage(Stages.Startup)
            .Build();

        // Add performance system to update stage
        var perfDelegate = new SystemFunction(PerformanceUpdateSystem);
        app.AddSystem(perfDelegate, null)
            .InStage(Stages.Update)
            .Build();

        // Run startup once
        app.RunStartup();

        // Benchmark update loop
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var current = 0L;
        var last = 0L;

        for (var i = 0; i < 50; ++i)
        {
            for (var j = 0; j < 3600; ++j)
                app.Update();

            var elapsed = sw.ElapsedMilliseconds;
            last = current;
            current = elapsed;
            Console.WriteLine($"Iteration {i}: {current - last} ms");
        }
    }
}

static class PerformanceTestState
{
    public static ComponentId<Position> PositionId;
    public static ComponentId<Velocity> VelocityId;
    public static ComponentId<Name> NameId;
}
