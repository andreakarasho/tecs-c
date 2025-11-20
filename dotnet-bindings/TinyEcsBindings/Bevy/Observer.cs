using System;
using System.Collections.Generic;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Marker interface for observer event types.
/// </summary>
public interface IObserverEvent { }

/// <summary>
/// Component was added to an entity.
/// </summary>
public struct Added : IObserverEvent { }

/// <summary>
/// Component was removed from an entity.
/// </summary>
public struct Removed : IObserverEvent { }

/// <summary>
/// Component value was changed/set.
/// </summary>
public struct Changed : IObserverEvent { }

/// <summary>
/// Observer trigger that provides entity and component data.
/// Can be used as a system parameter in observer callbacks.
/// </summary>
public readonly ref struct On<TComponent, TEvent>
    where TComponent : struct
    where TEvent : struct, IObserverEvent
{
    private readonly Entity _entity;
    private readonly TComponent _component;

    internal On(Entity entity, TComponent component)
    {
        _entity = entity;
        _component = component;
    }

    /// <summary>
    /// The entity that triggered this observer.
    /// </summary>
    public Entity Entity => _entity;

    /// <summary>
    /// The component data (may be default for Removed events).
    /// </summary>
    public TComponent Component => _component;

    /// <summary>
    /// The component type.
    /// </summary>
    public Type ComponentType => typeof(TComponent);

    /// <summary>
    /// The event type.
    /// </summary>
    public Type EventType => typeof(TEvent);
}

/// <summary>
/// Observer trigger for all events on a component.
/// </summary>
public readonly ref struct On<TComponent>
    where TComponent : struct
{
    private readonly Entity _entity;
    private readonly TComponent _component;

    internal On(Entity entity, TComponent component)
    {
        _entity = entity;
        _component = component;
    }

    public Entity Entity => _entity;
    public TComponent Component => _component;
    public Type ComponentType => typeof(TComponent);
}

/// <summary>
/// Internal event types for component observers.
/// </summary>
internal enum ObserverEvent
{
    OnAdd,
    OnRemove,
    OnSet
}

/// <summary>
/// Information about an observer trigger event.
/// </summary>
internal readonly struct ObserverTrigger
{
    public ObserverTrigger(Entity entity, Type componentType, ObserverEvent eventType)
    {
        Entity = entity;
        ComponentType = componentType;
        EventType = eventType;
    }

    public Entity Entity { get; }
    public Type ComponentType { get; }
    public ObserverEvent EventType { get; }
}

/// <summary>
/// Observer that reacts to component events.
/// </summary>
internal interface IObserver
{
    Type ComponentType { get; }
    Type EventType { get; }
    void Trigger(TinyWorld world, ObserverTrigger trigger);
}

/// <summary>
/// Generic observer for component events.
/// </summary>
internal sealed class Observer<TComponent, TEvent> : IObserver
    where TComponent : struct
    where TEvent : struct, IObserverEvent
{
    private readonly Action<On<TComponent, TEvent>> _callback;

    public Observer(Action<On<TComponent, TEvent>> callback)
    {
        _callback = callback;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(TEvent);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        var componentId = world.Component<TComponent>();
        TComponent component = default;

        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent, TEvent>(trigger.Entity, component);
        _callback(on);
    }
}

/// <summary>
/// Observer for all events on a component.
/// </summary>
internal sealed class ObserverAny<TComponent> : IObserver
    where TComponent : struct
{
    private readonly Action<On<TComponent>> _callback;

    public ObserverAny(Action<On<TComponent>> callback)
    {
        _callback = callback;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(Any);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        var componentId = world.Component<TComponent>();
        TComponent component = default;

        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent>(trigger.Entity, component);
        _callback(on);
    }
}

/// <summary>
/// Generic observer trigger - matches all events for a component.
/// </summary>
public struct Any : IObserverEvent { }

/// <summary>
/// Registry for observers.
/// </summary>
internal sealed class ObserverRegistry
{
    private readonly Dictionary<(Type ComponentType, Type EventType), List<IObserver>> _observers = new();

    public void Register(IObserver observer)
    {
        var key = (observer.ComponentType, observer.EventType);
        if (!_observers.TryGetValue(key, out var list))
        {
            list = new List<IObserver>();
            _observers[key] = list;
        }
        list.Add(observer);
    }

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        var eventType = trigger.EventType switch
        {
            ObserverEvent.OnAdd => typeof(Added),
            ObserverEvent.OnRemove => typeof(Removed),
            ObserverEvent.OnSet => typeof(Changed),
            _ => typeof(Any)
        };

        // Trigger specific event observers
        var key = (trigger.ComponentType, eventType);
        if (_observers.TryGetValue(key, out var list))
        {
            foreach (var observer in list)
            {
                observer.Trigger(world, trigger);
            }
        }

        // Also trigger Any observers
        var anyKey = (trigger.ComponentType, typeof(Any));
        if (_observers.TryGetValue(anyKey, out var anyList))
        {
            foreach (var observer in anyList)
            {
                observer.Trigger(world, trigger);
            }
        }
    }

    public bool HasObservers(Type componentType, Type eventType)
    {
        return _observers.ContainsKey((componentType, eventType));
    }
}

/// <summary>
/// Extension methods for observers.
/// Usage: app.AddObserver((On<Player, Removed> trigger) => { ... });
/// </summary>
public static class ObserverExtensions
{
    /// <summary>
    /// Register an observer for a specific component event.
    /// Example: app.AddObserver((On<Player, Added> trigger) => { var entity = trigger.Entity; });
    /// </summary>
    public static App AddObserver<TComponent, TEvent>(
        this App app,
        Action<On<TComponent, TEvent>> callback)
        where TComponent : struct
        where TEvent : struct, IObserverEvent
    {
        return app.AddObserverInternal(new Observer<TComponent, TEvent>(callback));
    }

    /// <summary>
    /// Register an observer that matches all events for a component.
    /// Example: app.AddObserver((On<Player> trigger) => { var entity = trigger.Entity; });
    /// </summary>
    public static App AddObserver<TComponent>(
        this App app,
        Action<On<TComponent>> callback)
        where TComponent : struct
    {
        return app.AddObserverInternal(new ObserverAny<TComponent>(callback));
    }
}
