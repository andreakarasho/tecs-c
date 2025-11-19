using System;

namespace TinyEcsBindings;

// Tag component (zero-sized struct)
struct Player { }

// Tag component (zero-sized struct)
struct Enemy { }

public static class AutoRegistrationExample
{
    public static void Run()
    {
        using var world = new TinyWorld();

        Console.WriteLine("=== Auto-Registration Example ===\n");
        Console.WriteLine("No manual RegisterComponent<T>() calls needed!\n");

        // Demonstrate zero-sized struct detection
        Console.WriteLine($"Position size: {ComponentSize<Position>.Size} bytes");
        Console.WriteLine($"Velocity size: {ComponentSize<Velocity>.Size} bytes");
        Console.WriteLine($"Player tag size: {ComponentSize<Player>.Size} bytes (zero-sized!)");
        Console.WriteLine($"Enemy tag size: {ComponentSize<Enemy>.Size} bytes (zero-sized!)\n");

        // Create entities - components are auto-registered on first use
        var playerEntity = world.Create();
        var enemyEntity = world.Create();

        // Set components - auto-registers Position, Velocity, and tag components
        world.Set(playerEntity, new Position { X = 10.0f, Y = 20.0f });
        world.Set(playerEntity, new Velocity { X = 1.0f, Y = 2.0f });
        world.Set(playerEntity, new Player());  // Tag component - no data allocated!

        world.Set(enemyEntity, new Position { X = 50.0f, Y = 60.0f });
        world.Set(enemyEntity, new Enemy());  // Tag component - no data allocated!

        Console.WriteLine("Created entities with auto-registered components and tags");

        // Get components - auto-registers if needed (already registered in this case)
        ref var playerPos = ref world.Get<Position>(playerEntity);
        ref var playerVel = ref world.Get<Velocity>(playerEntity);

        Console.WriteLine($"Player Position: ({playerPos.X}, {playerPos.Y})");
        Console.WriteLine($"Player Velocity: ({playerVel.X}, {playerVel.Y})");

        // Check tag components
        Console.WriteLine($"Player has Player tag: {world.Has<Player>(playerEntity)}");
        Console.WriteLine($"Enemy has Enemy tag: {world.Has<Enemy>(enemyEntity)}");

        // Query with auto-registration
        Console.WriteLine("\nQuerying entities with Position AND Player tag:");
        var query = world.Query()
            .With<Position>(world)  // Auto-registers Position
            .With<Player>(world)    // Auto-registers Player tag
            .Iter();

        int count = 0;
        while (query.MoveNext())
        {
            var positions = query.Column<Position>();

            for (int i = 0; i < query.Count; i++)
            {
                Console.WriteLine($"  Player entity: Pos({positions[i].X}, {positions[i].Y})");
                count++;
            }
        }
        Console.WriteLine($"Found {count} player entities");

        query.Dispose();

        // Query enemies
        Console.WriteLine("\nQuerying entities with Position AND Enemy tag:");
        query = world.Query()
            .With<Position>(world)
            .With<Enemy>(world)
            .Iter();

        count = 0;
        while (query.MoveNext())
        {
            var positions = query.Column<Position>();

            for (int i = 0; i < query.Count; i++)
            {
                Console.WriteLine($"  Enemy entity: Pos({positions[i].X}, {positions[i].Y})");
                count++;
            }
        }
        Console.WriteLine($"Found {count} enemy entities");

        query.Dispose();

        // Remove component
        world.Remove<Velocity>(playerEntity);
        Console.WriteLine("\nRemoved Velocity from player");
        Console.WriteLine($"Player has Velocity: {world.Has<Velocity>(playerEntity)}");

        Console.WriteLine("\n✓ All components were auto-registered!");
        Console.WriteLine("✓ No reflection used!");
        Console.WriteLine("✓ Type-safe and fast!");
    }
}
