using System.Runtime.CompilerServices;
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

            // Add managed components using tecs_set directly
            var playerName = new PlayerName { Value = "Hero" };
            tecs_set(world, player, nameId, Unsafe.AsPointer(ref playerName), IntPtr.Size);

            var playerDesc = new Description
            {
                Text = "The main character",
                Tags = new List<string> { "player", "hero" }
            };
            tecs_set(world, player, descId, Unsafe.AsPointer(ref playerDesc), IntPtr.Size);

            var enemyName = new PlayerName { Value = "Goblin" };
            tecs_set(world, enemy, nameId, Unsafe.AsPointer(ref enemyName), IntPtr.Size);

            var enemyDesc = new Description
            {
                Text = "A dangerous foe",
                Tags = new List<string> { "enemy", "hostile" }
            };
            tecs_set(world, enemy, descId, Unsafe.AsPointer(ref enemyDesc), IntPtr.Size);

            Console.WriteLine("Added managed components to entities\n");

            // Get and display components using tecs_get directly
            var playerNamePtr = tecs_get(world, player, nameId);
            ref var playerNameRef = ref Unsafe.AsRef<PlayerName?>(playerNamePtr);
            var playerDescPtr = tecs_get(world, player, descId);
            ref var playerDescRef = ref Unsafe.AsRef<Description?>(playerDescPtr);

            Console.WriteLine($"Player:");
            Console.WriteLine($"  Name: {playerNameRef?.Value}");
            Console.WriteLine($"  Description: {playerDescRef?.Text}");
            Console.WriteLine($"  Tags: {string.Join(", ", playerDescRef?.Tags ?? new List<string>())}\n");

            var enemyNamePtr = tecs_get(world, enemy, nameId);
            ref var enemyNameRef = ref Unsafe.AsRef<PlayerName?>(enemyNamePtr);
            var enemyDescPtr = tecs_get(world, enemy, descId);
            ref var enemyDescRef = ref Unsafe.AsRef<Description?>(enemyDescPtr);

            Console.WriteLine($"Enemy:");
            Console.WriteLine($"  Name: {enemyNameRef?.Value}");
            Console.WriteLine($"  Description: {enemyDescRef?.Text}");
            Console.WriteLine($"  Tags: {string.Join(", ", enemyDescRef?.Tags ?? new List<string>())}\n");

            // Modify in place - the object is stored directly in the pinned array
            if (playerNameRef != null)
            {
                playerNameRef.Value = "Legendary Hero";
                var modifiedPtr = tecs_get(world, player, nameId);
                ref var modifiedName = ref Unsafe.AsRef<PlayerName?>(modifiedPtr);
                Console.WriteLine($"Modified player name: {modifiedName?.Value}\n");
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
