using System;
using System.Runtime.InteropServices;

namespace TinyEcsBindings;

/// <summary>
/// Example usage of the high-level TinyApp wrapper API for Bevy-style applications.
/// </summary>
public static class TinyAppExample
{
    // Store delegates to prevent GC
    private static TinyEcsBevy.SystemFunction? s_startupSystem;
    private static TinyEcsBevy.SystemFunction? s_updateSystem;

    // Resource definitions - must be structs
    public struct Time
    {
        public float DeltaTime;
        public int Frame;
    }

    public struct GameConfig
    {
        public int MaxEnemies;
        public float SpawnRate;
    }

    // Managed resource with references
    public struct PlayerStats
    {
        public string PlayerName;
        public int Score;

        public PlayerStats(string name, int score)
        {
            PlayerName = name;
            Score = score;
        }
    }

    // Components
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

    public static unsafe void Run()
    {
        Console.WriteLine("\n--- TinyApp Bevy-Style Wrapper Example ---");

        using var app = new TinyApp();

        // Insert resources
        var timeId = app.InsertResource(new Time { DeltaTime = 0.016f, Frame = 0 });
        var configId = app.InsertResource(new GameConfig { MaxEnemies = 10, SpawnRate = 2.0f });
        var statsId = app.InsertResource(new PlayerStats("Player1", 0));

        Console.WriteLine("Inserted resources: Time, GameConfig, PlayerStats");

        // Register components via the World
        var posId = app.World.RegisterComponent<Position>();
        var velId = app.World.RegisterComponent<Velocity>();

        Console.WriteLine($"Registered components: Position={posId}, Velocity={velId}");

        // Create some entities
        var player = app.World.Create();
        app.World.Set(player, posId, new Position { X = 0, Y = 0 });
        app.World.Set(player, velId, new Velocity { X = 1, Y = 1 });

        var enemy = app.World.Create();
        app.World.Set(enemy, posId, new Position { X = 100, Y = 100 });
        app.World.Set(enemy, velId, new Velocity { X = -1, Y = -1 });

        Console.WriteLine($"Created entities: player={player}, enemy={enemy}");

        // Add a startup system
        s_startupSystem = StartupSystem;
        app.AddSystem(s_startupSystem, null)
            .InStage(Stages.Startup)
            .Build();

        // Add an update system
        s_updateSystem = UpdateSystem;
        app.AddSystem(s_updateSystem, null)
            .InStage(Stages.Update)
            .Build();

        Console.WriteLine("\nAdded systems to Startup and Update stages");

        // Run startup systems once
        Console.WriteLine("\n=== Running Startup ===");
        app.RunStartup();

        // Run a few update frames
        Console.WriteLine("\n=== Running Update Frames ===");
        for (int i = 0; i < 3; i++)
        {
            Console.WriteLine($"\nFrame {i + 1}:");
            app.Update();
        }

        Console.WriteLine($"\n=== Final State ===");
        Console.WriteLine("Resources successfully inserted and systems executed!");
        Console.WriteLine($"Note: Resource access via GetResource/GetResourceMut requires");
        Console.WriteLine($"proper integration with the Bevy resource system in the C library.");

        Console.WriteLine("\nApp will be cleaned up");
    }

    private static unsafe void StartupSystem(TinyEcsBevy.SystemContext* ctx, void* userData)
    {
        Console.WriteLine("  [Startup System] Initializing game...");
        Console.WriteLine("  [Startup System] Loading assets...");
        Console.WriteLine("  [Startup System] Ready!");
    }

    private static unsafe void UpdateSystem(TinyEcsBevy.SystemContext* ctx, void* userData)
    {
        Console.WriteLine("  [Update System] Processing game logic...");

        // In a real system, you'd:
        // - Query entities with tbevy_query_*
        // - Read/write resources via context
        // - Spawn/despawn entities via commands
    }
}
