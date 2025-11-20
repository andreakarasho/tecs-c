using System;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Interface for component bundles.
/// Bundles are groups of components that are commonly added together.
/// </summary>
public interface IBundle
{
    /// <summary>
    /// Insert all components in this bundle into the entity.
    /// </summary>
    void Insert(Entity entity, TinyWorld world);
}

/// <summary>
/// Extension methods for spawning entities with bundles.
/// </summary>
public static class BundleExtensions
{
    /// <summary>
    /// Spawn an entity with a bundle of components.
    /// </summary>
    public static Entity Spawn<TBundle>(this TinyWorld world, TBundle bundle)
        where TBundle : IBundle
    {
        var entity = world.Create();
        bundle.Insert(entity, world);
        return entity;
    }

    /// <summary>
    /// Insert a bundle of components into an existing entity.
    /// </summary>
    public static void InsertBundle<TBundle>(this Entity entity, TinyWorld world, TBundle bundle)
        where TBundle : IBundle
    {
        bundle.Insert(entity, world);
    }
}

/// <summary>
/// Example: Transform bundle with position, rotation, and scale.
/// </summary>
public struct TransformBundle : IBundle
{
    public struct Transform
    {
        public float X, Y, Z;
        public float RotationX, RotationY, RotationZ;
        public float ScaleX, ScaleY, ScaleZ;

        public static Transform Identity => new()
        {
            X = 0, Y = 0, Z = 0,
            RotationX = 0, RotationY = 0, RotationZ = 0,
            ScaleX = 1, ScaleY = 1, ScaleZ = 1
        };
    }

    public Transform LocalTransform;
    public Transform GlobalTransform;

    public TransformBundle()
    {
        LocalTransform = Transform.Identity;
        GlobalTransform = Transform.Identity;
    }

    public void Insert(Entity entity, TinyWorld world)
    {
        world.Set(entity, LocalTransform);
        world.Set(entity, GlobalTransform);
    }
}
