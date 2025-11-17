using System;

namespace TinyEcsBindings;

/// <summary>
/// Example usage of the high-level TinyWorld wrapper API.
/// </summary>
public static class EcsWorldExample
{
    // Component definitions - must be structs
    public struct Position
    {
        public float X;
        public float Y;
    }

    public struct Velocity
    {
        public float X;
        public float Y;
    }

    // Managed component - struct containing references
    public struct Name
    {
        public string Value;

        public Name(string value)
        {
            Value = value;
        }
    }

    public static unsafe void Run()
    {
        Console.WriteLine("\n--- TinyWorld Wrapper Example ---");

        using var world = new TinyWorld();

        // Register components
        var posId = world.RegisterComponent<Position>();
        var velId = world.RegisterComponent<Velocity>();
        var nameId = world.RegisterComponent<Name>();

        Console.WriteLine($"Registered Position: {posId}");
        Console.WriteLine($"Registered Velocity: {velId}");
        Console.WriteLine($"Registered Name: {nameId}");

        // Create entities
        var player = world.Create();
        var enemy = world.Create();

        Console.WriteLine($"\nCreated player: {player}");
        Console.WriteLine($"Created enemy: {enemy}");

        // Set components
        world.Set(player, posId, new Position { X = 10, Y = 20 });
        world.Set(player, velId, new Velocity { X = 1, Y = 2 });
        world.Set(player, nameId, new Name("Hero"));

        world.Set(enemy, posId, new Position { X = 50, Y = 60 });
        world.Set(enemy, velId, new Velocity { X = -1, Y = -1 });
        world.Set(enemy, nameId, new Name("Goblin"));

        Console.WriteLine("\nSet components on entities");

        // Get components
        ref var playerPos = ref world.Get(player, posId);
        ref var playerName = ref world.Get(player, nameId);

        Console.WriteLine($"Player position: ({playerPos.X}, {playerPos.Y})");
        Console.WriteLine($"Player name: {playerName.Value}");

        // Modify component in-place
        playerPos.X += 5;
        Console.WriteLine($"Updated player position: ({playerPos.X}, {playerPos.Y})");

        // Query all entities with Position, Velocity, and Name
        Console.WriteLine("\n=== Query: Position + Velocity + Name ===");
        var query = world.Query()
            .With(posId)
            .With(velId)
            .With(nameId)
            .Iter();

        while (query.MoveNext())
        {
            var positions = query.Column(posId);
            var velocities = query.Column(velId);
            var names = query.Column(nameId);

            for (int i = 0; i < query.Count; i++)
            {
                Console.WriteLine($"  Entity: Pos({positions[i].X}, {positions[i].Y}), " +
                                $"Vel({velocities[i].X}, {velocities[i].Y}), " +
                                $"Name={names[i].Value}");

                // Update position based on velocity
                positions[i].X += velocities[i].X;
                positions[i].Y += velocities[i].Y;
            }
        }

        query.Dispose();

        // Query again to see updated values
        Console.WriteLine("\n=== After movement ===");
        query = world.Query()
            .With(posId)
            .With(nameId)
            .Iter();

        while (query.MoveNext())
        {
            var positions = query.Column(posId);
            var names = query.Column(nameId);

            for (int i = 0; i < query.Count; i++)
            {
                Console.WriteLine($"  {names[i].Value}: ({positions[i].X}, {positions[i].Y})");
            }
        }

        query.Dispose();

        // Test entity operations
        Console.WriteLine($"\nEntity count: {world.Count}");
        Console.WriteLine($"Player exists: {world.Exists(player)}");

        world.Delete(enemy);
        Console.WriteLine($"After deleting enemy, entity count: {world.Count}");

        // Query with Without
        Console.WriteLine("\n=== Query: Position WITHOUT Velocity ===");
        var noVelQuery = world.Query()
            .With(posId)
            .Without(velId)
            .Iter();

        int count = 0;
        while (noVelQuery.MoveNext())
        {
            count += noVelQuery.Count;
        }
        Console.WriteLine($"Found {count} entities with Position but no Velocity");

        noVelQuery.Dispose();

        Console.WriteLine("\nWorld tick: " + world.Tick);
    }
}
