using System;
using TinyEcsBindings.Bevy;

namespace TinyEcsBindings;

public static class BevyQueryExample
{
    public static void Run()
    {
        Console.WriteLine("=== Bevy-Style Query API Example ===\n");

        using var world = new TinyWorld();

        // Create some entities
        var player = world.Create();
        var enemy1 = world.Create();
        var enemy2 = world.Create();
        var obstacle = world.Create(); // Only has Position, no Velocity

        // Set components using auto-registration
        world.Set(player, new Position { X = 10.0f, Y = 20.0f });
        world.Set(player, new Velocity { X = 1.0f, Y = 0.5f });

        world.Set(enemy1, new Position { X = 50.0f, Y = 30.0f });
        world.Set(enemy1, new Velocity { X = -0.5f, Y = 0.3f });

        world.Set(enemy2, new Position { X = 80.0f, Y = 10.0f });
        world.Set(enemy2, new Velocity { X = -0.8f, Y = -0.2f });

        world.Set(obstacle, new Position { X = 40.0f, Y = 40.0f });
        // No velocity for obstacle!

        Console.WriteLine("Created 4 entities:");
        Console.WriteLine("- 1 player (Position + Velocity)");
        Console.WriteLine("- 2 enemies (Position + Velocity)");
        Console.WriteLine("- 1 obstacle (Position only)\n");

        // Query using Bevy-style API - manual iteration
        Console.WriteLine("=== Manual Query Iteration ===");
        Console.WriteLine("Entities with Position AND Velocity:");

        var query = world.Query()
            .With<Position>(world)
            .With<Velocity>(world)
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

        Console.WriteLine($"Found {count} moving entities\n");
        query.Dispose();

        // Demonstrate chunk-based processing (Bevy-style)
        Console.WriteLine("=== Bevy-Style Chunk Processing ===");
        Console.WriteLine("Applying velocity to positions:");

        query = world.Query()
            .With<Position>(world)
            .With<Velocity>(world)
            .Iter();

        while (query.MoveNext())
        {
            var positions = query.Column<Position>();
            var velocities = query.Column<Velocity>();

            // Process entire chunk at once (SIMD-friendly pattern)
            for (int i = 0; i < query.Count; i++)
            {
                positions[i].X += velocities[i].X;
                positions[i].Y += velocities[i].Y;
            }
        }

        query.Dispose();

        Console.WriteLine("Updated positions:");

        query = world.Query()
            .With<Position>(world)
            .With<Velocity>(world)
            .Iter();

        while (query.MoveNext())
        {
            var positions = query.Column<Position>();
            var entities = query.Entities;

            for (int i = 0; i < query.Count; i++)
            {
                Console.WriteLine($"  Entity {entities[i].Raw}: Pos({positions[i].X:F1}, {positions[i].Y:F1})");
            }
        }

        query.Dispose();

        // Demonstrate new Ref<T> API for per-entity access
        Console.WriteLine("\n=== Ref<T> Per-Entity Access ===");
        Console.WriteLine("Using Data<T> deconstruction for per-entity refs:");

        var dataQuery = world.Query()
            .With<Position>(world)
            .With<Velocity>(world)
            .Build<Data<Position, Velocity>>();

        foreach (var data in dataQuery)
        {
            // Deconstruct into Ref<T> for per-entity access
            var (pos, vel) = data;

            Console.WriteLine($"  Entity {data.Entity.Raw}: " +
                            $"Pos({pos.Value.X:F1}, {pos.Value.Y:F1}), " +
                            $"Vel({vel.Value.X:F2}, {vel.Value.Y:F2})");

            // Can modify through ref
            pos.Value.X += vel.Value.X * 2.0f;
            pos.Value.Y += vel.Value.Y * 2.0f;
        }

        Console.WriteLine("\n=== DeconstructSpans for Chunk Access ===");
        Console.WriteLine("Using DeconstructSpans for chunk-based processing:");

        dataQuery = world.Query()
            .With<Position>(world)
            .With<Velocity>(world)
            .Build<Data<Position, Velocity>>();

        foreach (var data in dataQuery)
        {
            // Deconstruct into Span<T> for chunk access
            data.DeconstructSpans(out var positions, out var velocities);

            Console.WriteLine($"  Processing chunk of {positions.Length} entities");

            // SIMD-friendly chunk processing
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i].X += velocities[i].X;
                positions[i].Y += velocities[i].Y;
            }
        }

        Console.WriteLine("\n✓ Bevy-style chunk-based iteration working!");
        Console.WriteLine("✓ Auto-registration of components");
        Console.WriteLine("✓ Type-safe, zero-allocation queries");
        Console.WriteLine("✓ SIMD-friendly data layout");
        Console.WriteLine("✓ Ref<T> per-entity access");
        Console.WriteLine("✓ DeconstructSpans chunk access");
    }
}
