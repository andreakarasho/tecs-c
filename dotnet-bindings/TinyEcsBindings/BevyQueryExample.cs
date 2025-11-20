using System;
using TinyEcsBindings.Bevy;

namespace TinyEcsBindings;

/// <summary>
/// Example demonstrating the Bevy-style ECS system parameter API.
/// Shows Query, Commands, Res/ResMut, filters, and the App scheduler.
/// </summary>
public static class BevyQueryExample
{
    // Example components
    public struct Position { public float X, Y; }
    public struct Velocity { public float X, Y; }
    public struct Health { public int Current, Max; }
    public struct Enemy { }

    // Example resource
    public class Time
    {
        public float DeltaTime { get; set; } = 0.016f;
        public float TotalTime { get; set; }
    }

    public static void Run()
    {
        Console.WriteLine("=== Bevy-Style System Parameter API Example ===\n");

        var world = new TinyWorld();
        var app = new App(world);

        // Insert resources
        world.SetResource(new Time());

        // Add startup systems
        app.AddSystemToStage("Startup", SystemAdapters.Create<Commands>(SpawnEntities));

        // Add update systems with dependency injection
        var movementSystem = SystemAdapters.Create<Query<Data<Position, Velocity>>, Res<Time>>(MovementSystem);
        var printSystem = SystemAdapters.Create<Query<Data<Position>>>(PrintPositions);

        app.AddSystem(movementSystem)
            .Label("movement");

        app.AddSystem(printSystem)
            .After(movementSystem);

        // Run startup
        app.RunStartup();

        // Run a few frames
        Console.WriteLine("\nRunning 3 frames...");
        for (int frame = 0; frame < 3; frame++)
        {
            app.Update();
        }

        Console.WriteLine("\n=== Old-Style Direct Query API Example ===\n");

        using var world2 = new TinyWorld();

        // Create some entities
        var player = world2.Create();
        var enemy1 = world2.Create();
        var enemy2 = world2.Create();
        var obstacle = world2.Create(); // Only has Position, no Velocity

        // Set components using auto-registration
        world2.Set(player, new Position { X = 10.0f, Y = 20.0f });
        world2.Set(player, new Velocity { X = 1.0f, Y = 0.5f });

        world2.Set(enemy1, new Position { X = 50.0f, Y = 30.0f });
        world2.Set(enemy1, new Velocity { X = -0.5f, Y = 0.3f });

        world2.Set(enemy2, new Position { X = 80.0f, Y = 10.0f });
        world2.Set(enemy2, new Velocity { X = -0.8f, Y = -0.2f });

        world2.Set(obstacle, new Position { X = 40.0f, Y = 40.0f });
        // No velocity for obstacle!

        Console.WriteLine("Created 4 entities:");
        Console.WriteLine("- 1 player (Position + Velocity)");
        Console.WriteLine("- 2 enemies (Position + Velocity)");
        Console.WriteLine("- 1 obstacle (Position only)\n");

        // Query using direct API - manual iteration
        Console.WriteLine("=== Manual Query Iteration ===");
        Console.WriteLine("Entities with Position AND Velocity:");

        var query = world2.Query()
            .With<Position>()
            .With<Velocity>()
            .Iter();

        int count = 0;
        while (query.MoveNext())
        {
            var positions = query.Column<Position>();
            var velocities = query.Column<Velocity>();
            var entities = query.Entities;

            for (int i = 0; i < query.Count; i++)
            {
                count++;
                Console.WriteLine($"  Entity {entities[i].Raw}: Pos({positions[i].X:F1}, {positions[i].Y:F1}), " +
                                $"Vel({velocities[i].X:F2}, {velocities[i].Y:F2})");
            }
        }

        Console.WriteLine($"Found {count} moving entities");
        query.Dispose();
    }

    // System functions for the Bevy-style API
    private static void SpawnEntities(Commands commands)
    {
        Console.WriteLine("Spawning entities...");

        for (int i = 0; i < 3; i++)
        {
            commands.Spawn()
                .Insert(new Position { X = i * 10f, Y = 0f })
                .Insert(new Velocity { X = 1f, Y = 0.5f });
        }

        Console.WriteLine("Spawned 3 entities with Position and Velocity");
    }

    private static void MovementSystem(Query<Data<Position, Velocity>> query, Res<Time> time)
    {
        var deltaTime = time.Value.DeltaTime;

        foreach (var data in query.Iter())
        {
            var (pos, vel) = data;
            pos.Ref.X += vel.Ref.X * deltaTime;
            pos.Ref.Y += vel.Ref.Y * deltaTime;
        }
    }

    private static void PrintPositions(Query<Data<Position>> query)
    {
        int count = 0;
        foreach (var data in query.Iter())
        {
            data.Deconstruct(out var pos);
            Console.WriteLine($"  Position({pos.Ref.X:F2}, {pos.Ref.Y:F2})");
            count++;
        }

        if (count == 0)
        {
            Console.WriteLine("  (No entities)");
        }
    }
}
