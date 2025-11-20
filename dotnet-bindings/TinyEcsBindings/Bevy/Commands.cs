using System;
using System.Collections.Generic;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// System parameter for deferred world mutations.
/// Commands are queued during system execution and applied after all systems finish.
/// This allows safe mutation of the world while iterating over queries.
/// </summary>
public sealed class Commands : ISystemParam
{
    private TinyWorld? _world;
    private readonly List<IDeferredCommand> _commands = new();
    private readonly List<Entity> _spawnedEntities = new();

    public void Initialize(TinyWorld world)
    {
        _world = world;
    }

    public void Fetch(TinyWorld world)
    {
        _world = world;
    }

    public SystemParamAccess GetAccess()
    {
        var access = new SystemParamAccess();
        // Commands have exclusive world access to prevent conflicts
        access.WriteResources.Add(typeof(Commands));
        return access;
    }

    /// <summary>
    /// Spawns a new entity and returns a builder for adding components.
    /// </summary>
    public EntityCommands Spawn()
    {
        var entity = _world!.Create();
        var spawnIndex = _spawnedEntities.Count;
        _spawnedEntities.Add(entity);
        return new EntityCommands(this, spawnIndex, entity);
    }

    /// <summary>
    /// Spawns a new entity with a bundle of components.
    /// </summary>
    public EntityCommands SpawnBundle<TBundle>(TBundle bundle) where TBundle : IBundle
    {
        var entity = _world!.Create();
        var spawnIndex = _spawnedEntities.Count;
        _spawnedEntities.Add(entity);

        // Queue the bundle insertion
        _commands.Add(new InsertBundleCommand<TBundle>(this, spawnIndex, bundle));

        return new EntityCommands(this, spawnIndex, entity);
    }

    /// <summary>
    /// Gets a builder for an existing entity.
    /// </summary>
    public EntityCommands Entity(Entity entity)
    {
        return new EntityCommands(this, -1, entity);
    }

    /// <summary>
    /// Tries to get a builder for an entity if it exists.
    /// </summary>
    public bool TryEntity(Entity entity, out EntityCommands entityCommands)
    {
        if (_world!.Exists(entity))
        {
            entityCommands = new EntityCommands(this, -1, entity);
            return true;
        }
        entityCommands = default;
        return false;
    }

    /// <summary>
    /// Inserts a global resource.
    /// </summary>
    public void InsertResource<T>(T resource) where T : notnull
    {
        _commands.Add(new InsertResourceCommand<T>(resource));
    }

    /// <summary>
    /// Removes a global resource.
    /// </summary>
    public void RemoveResource<T>() where T : notnull
    {
        _commands.Add(new RemoveResourceCommand(typeof(T)));
    }

    /// <summary>
    /// Checks if a resource exists (immediate check, not deferred).
    /// </summary>
    public bool HasResource<T>() where T : notnull
    {
        return _world!.HasResource<T>();
    }

    internal void QueueCommand(IDeferredCommand command)
    {
        _commands.Add(command);
    }

    internal Entity GetSpawnedEntity(int spawnIndex)
    {
        return _spawnedEntities[spawnIndex];
    }

    internal Entity ResolveEntity(in DeferredEntityRef entityRef)
    {
        return entityRef.SpawnIndex >= 0
            ? GetSpawnedEntity(entityRef.SpawnIndex)
            : entityRef.Entity;
    }

    /// <summary>
    /// Applies all queued commands. Called automatically by the scheduler after systems run.
    /// </summary>
    internal void Apply()
    {
        if (_commands.Count == 0)
            return;

        foreach (var cmd in _commands)
        {
            cmd.Execute(_world!, this);
        }

        _commands.Clear();
        _spawnedEntities.Clear();
    }
}

/// <summary>
/// Builder for entity-specific commands.
/// </summary>
public readonly ref struct EntityCommands
{
    private readonly Commands _commands;
    private readonly int _spawnIndex; // -1 if existing entity
    private readonly Entity _entity;

    internal EntityCommands(Commands commands, int spawnIndex, Entity entity)
    {
        _commands = commands;
        _spawnIndex = spawnIndex;
        _entity = entity;
    }

    /// <summary>
    /// Inserts a component on this entity.
    /// </summary>
    public EntityCommands Insert<T>(T component) where T : struct
    {
        if (_spawnIndex >= 0)
        {
            _commands.QueueCommand(new InsertComponentCommand<T>(_commands, _spawnIndex, component));
        }
        else
        {
            _commands.QueueCommand(new InsertComponentCommand<T>(_entity, component));
        }
        return this;
    }

    /// <summary>
    /// Inserts a bundle of components on this entity.
    /// </summary>
    public EntityCommands InsertBundle<TBundle>(TBundle bundle) where TBundle : IBundle
    {
        if (_spawnIndex >= 0)
        {
            _commands.QueueCommand(new InsertBundleCommand<TBundle>(_commands, _spawnIndex, bundle));
        }
        else
        {
            _commands.QueueCommand(new InsertBundleCommand<TBundle>(_entity, bundle));
        }
        return this;
    }

    /// <summary>
    /// Removes a component from this entity.
    /// </summary>
    public EntityCommands Remove<T>() where T : struct
    {
        if (_spawnIndex >= 0)
        {
            _commands.QueueCommand(new RemoveComponentCommand<T>(_commands, _spawnIndex));
        }
        else
        {
            _commands.QueueCommand(new RemoveComponentCommand<T>(_entity));
        }
        return this;
    }

    /// <summary>
    /// Despawns this entity.
    /// </summary>
    public void Despawn()
    {
        if (_spawnIndex >= 0)
        {
            _commands.QueueCommand(new DespawnCommand(_commands, _spawnIndex));
        }
        else
        {
            _commands.QueueCommand(new DespawnCommand(_entity));
        }
    }

    /// <summary>
    /// Gets the entity (immediate, not deferred).
    /// </summary>
    public Entity Entity => _spawnIndex >= 0 ? _commands.GetSpawnedEntity(_spawnIndex) : _entity;

    /// <summary>
    /// Gets the entity ID (convenience property, same as Entity.Id).
    /// </summary>
    public ulong Id => Entity.Id;
}

// Internal command interfaces and implementations

internal interface IDeferredCommand
{
    void Execute(TinyWorld world, Commands commands);
}

internal readonly struct DeferredEntityRef
{
    public readonly int SpawnIndex; // -1 if existing entity
    public readonly Entity Entity;

    public DeferredEntityRef(int spawnIndex, Entity entity)
    {
        SpawnIndex = spawnIndex;
        Entity = entity;
    }
}

internal readonly struct InsertComponentCommand<T> : IDeferredCommand where T : struct
{
    private readonly DeferredEntityRef _entityRef;
    private readonly T _component;

    public InsertComponentCommand(Commands commands, int spawnIndex, T component)
    {
        _entityRef = new DeferredEntityRef(spawnIndex, default);
        _component = component;
    }

    public InsertComponentCommand(Entity entity, T component)
    {
        _entityRef = new DeferredEntityRef(-1, entity);
        _component = component;
    }

    public void Execute(TinyWorld world, Commands commands)
    {
        var entity = commands.ResolveEntity(_entityRef);
        if (world.Exists(entity))
        {
            var componentId = world.Component<T>();
            var hadComponent = world.Has(entity, componentId);
            world.Set(entity, componentId, _component);

            // Trigger observers
            if (world.TryGetResource<ObserverRegistry>(out var registry))
            {
                var eventType = hadComponent ? ObserverEvent.OnSet : ObserverEvent.OnAdd;
                var trigger = new ObserverTrigger(entity, typeof(T), eventType);
                registry.Trigger(world, trigger);
            }
        }
    }
}

internal readonly struct InsertBundleCommand<TBundle> : IDeferredCommand where TBundle : IBundle
{
    private readonly DeferredEntityRef _entityRef;
    private readonly TBundle _bundle;

    public InsertBundleCommand(Commands commands, int spawnIndex, TBundle bundle)
    {
        _entityRef = new DeferredEntityRef(spawnIndex, default);
        _bundle = bundle;
    }

    public InsertBundleCommand(Entity entity, TBundle bundle)
    {
        _entityRef = new DeferredEntityRef(-1, entity);
        _bundle = bundle;
    }

    public void Execute(TinyWorld world, Commands commands)
    {
        var entity = commands.ResolveEntity(_entityRef);
        if (world.Exists(entity))
        {
            _bundle.Insert(entity, world);
        }
    }
}

internal readonly struct RemoveComponentCommand<T> : IDeferredCommand where T : struct
{
    private readonly DeferredEntityRef _entityRef;

    public RemoveComponentCommand(Commands commands, int spawnIndex)
    {
        _entityRef = new DeferredEntityRef(spawnIndex, default);
    }

    public RemoveComponentCommand(Entity entity)
    {
        _entityRef = new DeferredEntityRef(-1, entity);
    }

    public void Execute(TinyWorld world, Commands commands)
    {
        var entity = commands.ResolveEntity(_entityRef);
        if (world.Exists(entity))
        {
            var componentId = world.Component<T>();
            world.Remove(entity, componentId);

            // Trigger observers
            if (world.TryGetResource<ObserverRegistry>(out var registry))
            {
                var trigger = new ObserverTrigger(entity, typeof(T), ObserverEvent.OnRemove);
                registry.Trigger(world, trigger);
            }
        }
    }
}

internal readonly struct DespawnCommand : IDeferredCommand
{
    private readonly DeferredEntityRef _entityRef;

    public DespawnCommand(Commands commands, int spawnIndex)
    {
        _entityRef = new DeferredEntityRef(spawnIndex, default);
    }

    public DespawnCommand(Entity entity)
    {
        _entityRef = new DeferredEntityRef(-1, entity);
    }

    public void Execute(TinyWorld world, Commands commands)
    {
        var entity = commands.ResolveEntity(_entityRef);
        if (world.Exists(entity))
        {
            world.Delete(entity);
        }
    }
}

internal readonly struct InsertResourceCommand<T> : IDeferredCommand where T : notnull
{
    private readonly T _resource;

    public InsertResourceCommand(T resource)
    {
        _resource = resource;
    }

    public void Execute(TinyWorld world, Commands commands)
    {
        world.SetResource(_resource);
    }
}

internal readonly struct RemoveResourceCommand : IDeferredCommand
{
    private readonly Type _resourceType;

    public RemoveResourceCommand(Type resourceType)
    {
        _resourceType = resourceType;
    }

    public void Execute(TinyWorld world, Commands commands)
    {
        world.RemoveResource(_resourceType);
    }
}
