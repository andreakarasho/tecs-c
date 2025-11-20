using System;
using TinyEcsBindings.Bevy;

namespace TinyEcsBindings;

/// <summary>
/// Comprehensive example demonstrating advanced Bevy-style ECS features:
/// - Observers with system parameters
/// - System piping
/// - Custom schedules
/// - State management
/// - Events
/// - Local state
/// </summary>
public static class BevyAdvancedExample
{
    // Components
    public struct Position { public float X, Y; }
    public struct Velocity { public float X, Y; }
    public struct Health { public int Current, Max; }
    public struct Player { }
    public struct Enemy { }
    public struct Damage { public int Amount; }

    // Resources
    public class GameConfig
    {
        public float MoveSpeed { get; set; } = 100.0f;
        public int StartingHealth { get; set; } = 100;
    }

    public class ScoreResource
    {
        public int Score { get; set; }
        public int EnemiesDefeated { get; set; }
    }

    // Events
    public struct EnemyDefeatedEvent
    {
        public Entity Enemy;
        public int ScoreValue;
    }

    public struct PlayerDamagedEvent
    {
        public Entity Player;
        public int DamageAmount;
    }

    // Game states
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    // System piping outputs
    public readonly struct EnemyCount : ISystemOutput
    {
        public int Count { get; init; }
    }

    public readonly struct HealthStatus : ISystemOutput
    {
        public bool IsLowHealth { get; init; }
        public int CurrentHealth { get; init; }
    }

    public static void Run()
    {
        Console.WriteLine("=== Bevy Advanced Features Example ===\n");

        var world = new TinyWorld();
        var app = new App(world);

        // Add resources
        world.SetResource(new GameConfig());
        world.SetResource(new ScoreResource());

        // Add state management
        app.AddState(GameState.MainMenu);

        // === STARTUP SYSTEMS ===
        app.AddSystemToStage("Startup", (Commands commands, Res<GameConfig> config) =>
        {
            Console.WriteLine("[Startup] Initializing game...");

            // Spawn player
            commands.Spawn()
                .Insert(new Player())
                .Insert(new Position { X = 0, Y = 0 })
                .Insert(new Velocity { X = 0, Y = 0 })
                .Insert(new Health { Current = config.Value.StartingHealth, Max = config.Value.StartingHealth });

            // Spawn enemies
            for (int i = 0; i < 3; i++)
            {
                commands.Spawn()
                    .Insert(new Enemy())
                    .Insert(new Position { X = 100 + i * 50, Y = 100 })
                    .Insert(new Health { Current = 50, Max = 50 });
            }

            Console.WriteLine("  - Spawned 1 player and 3 enemies\n");
        });

        // === OBSERVERS ===
        // Observer: React when health changes
        app.AddObserver((On<Health, Changed> trigger, Commands commands) =>
        {
            var health = trigger.Component;
            Console.WriteLine($"[Observer] Health changed on entity {trigger.Entity.Raw}: {health.Current}/{health.Max}");

            if (health.Current <= 0)
            {
                Console.WriteLine($"[Observer] Entity {trigger.Entity.Raw} died! Despawning...");
                commands.Entity(trigger.Entity).Despawn();
            }
        });

        // Observer: React when damage is added (simpler version)
        app.AddObserver((On<Damage, Added> trigger, EventWriter<PlayerDamagedEvent> events) =>
        {
            var damage = trigger.Component;
            var entity = trigger.Entity;

            Console.WriteLine($"[Observer] Damage component added to entity {entity.Raw}: {damage.Amount} damage");

            // Send event
            events.Send(new PlayerDamagedEvent { Player = entity, DamageAmount = damage.Amount });
        });

        // Observer: React when enemy is removed (defeated)
        app.AddObserver((On<Enemy, Removed> trigger, EventWriter<EnemyDefeatedEvent> events, ResMut<ScoreResource> score) =>
        {
            Console.WriteLine($"[Observer] Enemy {trigger.Entity.Raw} was removed!");
            score.Value.EnemiesDefeated++;
            score.Value.Score += 100;

            events.Send(new EnemyDefeatedEvent { Enemy = trigger.Entity, ScoreValue = 100 });
        });

        // === SYSTEM PIPING ===
        // Simple piping example
        app.AddPipedSystem(
            // Source: Return enemy count
            () => new EnemyCount { Count = 2 },
            // Target: Process count
            (In<EnemyCount> input) =>
            {
                Console.WriteLine($"[Piped System] Enemy count from previous system: {input.Value.Count}");
            }
        );

        // Health status piping
        app.AddPipedSystem(
            // Source: Check health
            () => new HealthStatus { IsLowHealth = true, CurrentHealth = 25 },
            // Target: React to status
            (In<HealthStatus> input) =>
            {
                if (input.Value.IsLowHealth)
                {
                    Console.WriteLine($"[Piped System] ⚠️  Low health warning! ({input.Value.CurrentHealth}HP)");
                }
            }
        );

        // === STATE-BASED SYSTEMS ===
        // Movement system - only runs during Playing state
        app.AddSystem((Query<Data<Position, Velocity>> query) =>
        {
            Console.WriteLine("[Playing State] Movement system running...");

            // Direct foreach on query - no need for .Iter()!
            foreach (var (pos, vel) in query)
            {
                pos.Ref.X += vel.Ref.X * 0.016f;
                pos.Ref.Y += vel.Ref.Y * 0.016f;
            }
        })
        .Label("movement")
        .RunIf(StateConditions.InState(GameState.Playing));

        // Menu system - only runs in MainMenu state
        app.AddSystem(() =>
        {
            Console.WriteLine("[MainMenu State] Waiting for player input...");
        })
        .RunIf(StateConditions.InState(GameState.MainMenu));

        // === EVENT SYSTEMS ===
        app.AddSystem((EventReader<EnemyDefeatedEvent> reader, Res<ScoreResource> score) =>
        {
            foreach (var evt in reader.Iter())
            {
                Console.WriteLine($"[Event System] Enemy defeated event received! Score: {score.Value.Score}");
            }
        });

        app.AddSystem((EventReader<PlayerDamagedEvent> reader) =>
        {
            foreach (var evt in reader.Iter())
            {
                Console.WriteLine($"[Event System] Player took {evt.DamageAmount} damage!");
            }
        });

        // === LOCAL STATE SYSTEM ===
        app.AddSystem((Local<int> frameCounter) =>
        {
            frameCounter.Value++;
            Console.WriteLine($"[Local State] Frame counter: {frameCounter.Value}");
        })
        .After("movement");

        // === CUSTOM SCHEDULE ===
        var fixedUpdateSchedule = new Schedule("FixedUpdate")
            .AddStage(new Stage("FixedUpdate"));

        app.AddSchedule(fixedUpdateSchedule);

        app.AddSystemToSchedule("FixedUpdate", "FixedUpdate", () =>
        {
            Console.WriteLine("[Fixed Update] Physics tick at fixed timestep");
        });

        Console.WriteLine("=== Running Simulation ===\n");

        // Run startup
        app.RunStartup();

        // Simulate state transitions and frames
        Console.WriteLine("\n--- Frame 1: Main Menu ---");
        app.Update();

        Console.WriteLine("\n--- Transitioning to Playing state ---");
        world.GetResource<State<GameState>>().Set(GameState.Playing);

        Console.WriteLine("\n--- Frame 2: Playing ---");
        app.Update();

        Console.WriteLine("\n--- Running Fixed Update Schedule ---");
        app.RunSchedule("FixedUpdate");

        Console.WriteLine("\n--- Frame 3: Triggering observer by adding Damage ---");
        var testEntity = world.Create();
        world.Set(testEntity, new Health { Current = 30, Max = 100 });
        world.Set(testEntity, new Damage { Amount = 35 }); // This triggers the Added observer

        app.Update();

        Console.WriteLine("\n--- Final Score ---");
        var finalScore = world.GetResource<ScoreResource>();
        Console.WriteLine($"Score: {finalScore.Score}");
        Console.WriteLine($"Enemies Defeated: {finalScore.EnemiesDefeated}");

        Console.WriteLine("\n=== Example Complete ===");
    }
}
