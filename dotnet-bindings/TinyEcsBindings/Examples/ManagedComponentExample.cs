using TinyEcsBindings;
using static TinyEcsBindings.TinyEcs;
using static TinyEcsBindings.ManagedStorage;

namespace TinyEcsBindings.Examples;

// Managed component types
public class PlayerName
{
    public string Value { get; set; } = "";
}

public class Description
{
    public string Text { get; set; } = "";
    public List<string> Tags { get; set; } = new();
}

public static unsafe class ManagedComponentExample
{
    public static void Run()
    {
        Console.WriteLine("=== Managed Component Example ===\n");

        var world = tecs_world_new();

        // Register managed components with custom storage
        var nameStorage = new ManagedStorageProvider<PlayerName>();
        var descStorage = new ManagedStorageProvider<Description>();

        try
        {
            var nameId = RegisterManagedComponent(world, "PlayerName", out nameStorage);
            var descId = RegisterManagedComponent(world, "Description", out descStorage);

            Console.WriteLine($"Registered PlayerName component: {nameId.Value}");
            Console.WriteLine($"Registered Description component: {descId.Value}\n");

            // Create entities with managed components
            var player = tecs_entity_new(world);
            var enemy = tecs_entity_new(world);

            // Add managed components using the storage provider
            nameStorage.AddComponent(world, player, nameId, new PlayerName { Value = "Hero" });
            descStorage.AddComponent(world, player, descId, new Description
            {
                Text = "The main character",
                Tags = new List<string> { "player", "hero" }
            });

            nameStorage.AddComponent(world, enemy, nameId, new PlayerName { Value = "Goblin" });
            descStorage.AddComponent(world, enemy, descId, new Description
            {
                Text = "A dangerous foe",
                Tags = new List<string> { "enemy", "hostile" }
            });

            Console.WriteLine("Added managed components to entities\n");

            // Get and display components
            var playerName = nameStorage.GetComponent(world, player, nameId);
            var playerDesc = descStorage.GetComponent(world, player, descId);

            Console.WriteLine($"Player:");
            Console.WriteLine($"  Name: {playerName?.Value}");
            Console.WriteLine($"  Description: {playerDesc?.Text}");
            Console.WriteLine($"  Tags: {string.Join(", ", playerDesc?.Tags ?? new List<string>())}\n");

            var enemyName = nameStorage.GetComponent(world, enemy, nameId);
            var enemyDesc = descStorage.GetComponent(world, enemy, descId);

            Console.WriteLine($"Enemy:");
            Console.WriteLine($"  Name: {enemyName?.Value}");
            Console.WriteLine($"  Description: {enemyDesc?.Text}");
            Console.WriteLine($"  Tags: {string.Join(", ", enemyDesc?.Tags ?? new List<string>())}\n");

            // Modify in place - the object is stored directly in the pinned array
            if (playerName != null)
            {
                playerName.Value = "Legendary Hero";
                Console.WriteLine($"Modified player name: {nameStorage.GetComponent(world, player, nameId)?.Value}\n");
            }

            Console.WriteLine("=== Benefits ===");
            Console.WriteLine("✓ Single GCHandle per column (not per component)");
            Console.WriteLine("✓ Object references stored directly in pinned array");
            Console.WriteLine("✓ No boxing/unboxing overhead");
            Console.WriteLine("✓ Can modify objects in place");
            Console.WriteLine("✓ Full debugger support");
        }
        finally
        {
            nameStorage?.Dispose();
            descStorage?.Dispose();
            tecs_world_free(world);
        }
    }
}
