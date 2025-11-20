using System;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Generated observer extension methods with system parameters.
/// Observers can request system parameters just like regular systems.
/// </summary>
public static class ObserverSystemExtensions
{
    /// <summary>
    /// Register an observer with 1 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player, Added&gt; trigger, Query&lt;Health&gt; query) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, TEvent, T0>(
        this App app,
        Action<On<TComponent, TEvent>, T0> callback)
        where TComponent : struct
        where TEvent : struct, IObserverEvent
        where T0 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0() };
        return app.AddObserverInternal(new ObserverWithParams<TComponent, TEvent, T0>(callback, parameters));
    }

    /// <summary>
    /// Register an observer that matches all events with 1 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player&gt; trigger, Commands commands) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, T0>(
        this App app,
        Action<On<TComponent>, T0> callback)
        where TComponent : struct
        where T0 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0() };
        return app.AddObserverInternal(new ObserverAnyWithParams<TComponent, T0>(callback, parameters));
    }

    /// <summary>
    /// Register an observer with 2 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player, Added&gt; trigger, Query&lt;Health&gt; query) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, TEvent, T0, T1>(
        this App app,
        Action<On<TComponent, TEvent>, T0, T1> callback)
        where TComponent : struct
        where TEvent : struct, IObserverEvent
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1() };
        return app.AddObserverInternal(new ObserverWithParams<TComponent, TEvent, T0, T1>(callback, parameters));
    }

    /// <summary>
    /// Register an observer that matches all events with 2 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player&gt; trigger, Commands commands) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, T0, T1>(
        this App app,
        Action<On<TComponent>, T0, T1> callback)
        where TComponent : struct
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1() };
        return app.AddObserverInternal(new ObserverAnyWithParams<TComponent, T0, T1>(callback, parameters));
    }

    /// <summary>
    /// Register an observer with 3 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player, Added&gt; trigger, Query&lt;Health&gt; query) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, TEvent, T0, T1, T2>(
        this App app,
        Action<On<TComponent, TEvent>, T0, T1, T2> callback)
        where TComponent : struct
        where TEvent : struct, IObserverEvent
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1(), new T2() };
        return app.AddObserverInternal(new ObserverWithParams<TComponent, TEvent, T0, T1, T2>(callback, parameters));
    }

    /// <summary>
    /// Register an observer that matches all events with 3 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player&gt; trigger, Commands commands) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, T0, T1, T2>(
        this App app,
        Action<On<TComponent>, T0, T1, T2> callback)
        where TComponent : struct
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1(), new T2() };
        return app.AddObserverInternal(new ObserverAnyWithParams<TComponent, T0, T1, T2>(callback, parameters));
    }

    /// <summary>
    /// Register an observer with 4 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player, Added&gt; trigger, Query&lt;Health&gt; query) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, TEvent, T0, T1, T2, T3>(
        this App app,
        Action<On<TComponent, TEvent>, T0, T1, T2, T3> callback)
        where TComponent : struct
        where TEvent : struct, IObserverEvent
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1(), new T2(), new T3() };
        return app.AddObserverInternal(new ObserverWithParams<TComponent, TEvent, T0, T1, T2, T3>(callback, parameters));
    }

    /// <summary>
    /// Register an observer that matches all events with 4 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player&gt; trigger, Commands commands) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, T0, T1, T2, T3>(
        this App app,
        Action<On<TComponent>, T0, T1, T2, T3> callback)
        where TComponent : struct
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1(), new T2(), new T3() };
        return app.AddObserverInternal(new ObserverAnyWithParams<TComponent, T0, T1, T2, T3>(callback, parameters));
    }

    /// <summary>
    /// Register an observer with 5 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player, Added&gt; trigger, Query&lt;Health&gt; query) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, TEvent, T0, T1, T2, T3, T4>(
        this App app,
        Action<On<TComponent, TEvent>, T0, T1, T2, T3, T4> callback)
        where TComponent : struct
        where TEvent : struct, IObserverEvent
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1(), new T2(), new T3(), new T4() };
        return app.AddObserverInternal(new ObserverWithParams<TComponent, TEvent, T0, T1, T2, T3, T4>(callback, parameters));
    }

    /// <summary>
    /// Register an observer that matches all events with 5 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player&gt; trigger, Commands commands) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, T0, T1, T2, T3, T4>(
        this App app,
        Action<On<TComponent>, T0, T1, T2, T3, T4> callback)
        where TComponent : struct
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1(), new T2(), new T3(), new T4() };
        return app.AddObserverInternal(new ObserverAnyWithParams<TComponent, T0, T1, T2, T3, T4>(callback, parameters));
    }

    /// <summary>
    /// Register an observer with 6 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player, Added&gt; trigger, Query&lt;Health&gt; query) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, TEvent, T0, T1, T2, T3, T4, T5>(
        this App app,
        Action<On<TComponent, TEvent>, T0, T1, T2, T3, T4, T5> callback)
        where TComponent : struct
        where TEvent : struct, IObserverEvent
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1(), new T2(), new T3(), new T4(), new T5() };
        return app.AddObserverInternal(new ObserverWithParams<TComponent, TEvent, T0, T1, T2, T3, T4, T5>(callback, parameters));
    }

    /// <summary>
    /// Register an observer that matches all events with 6 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player&gt; trigger, Commands commands) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, T0, T1, T2, T3, T4, T5>(
        this App app,
        Action<On<TComponent>, T0, T1, T2, T3, T4, T5> callback)
        where TComponent : struct
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1(), new T2(), new T3(), new T4(), new T5() };
        return app.AddObserverInternal(new ObserverAnyWithParams<TComponent, T0, T1, T2, T3, T4, T5>(callback, parameters));
    }

    /// <summary>
    /// Register an observer with 7 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player, Added&gt; trigger, Query&lt;Health&gt; query) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, TEvent, T0, T1, T2, T3, T4, T5, T6>(
        this App app,
        Action<On<TComponent, TEvent>, T0, T1, T2, T3, T4, T5, T6> callback)
        where TComponent : struct
        where TEvent : struct, IObserverEvent
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
        where T6 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1(), new T2(), new T3(), new T4(), new T5(), new T6() };
        return app.AddObserverInternal(new ObserverWithParams<TComponent, TEvent, T0, T1, T2, T3, T4, T5, T6>(callback, parameters));
    }

    /// <summary>
    /// Register an observer that matches all events with 7 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player&gt; trigger, Commands commands) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, T0, T1, T2, T3, T4, T5, T6>(
        this App app,
        Action<On<TComponent>, T0, T1, T2, T3, T4, T5, T6> callback)
        where TComponent : struct
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
        where T6 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1(), new T2(), new T3(), new T4(), new T5(), new T6() };
        return app.AddObserverInternal(new ObserverAnyWithParams<TComponent, T0, T1, T2, T3, T4, T5, T6>(callback, parameters));
    }

    /// <summary>
    /// Register an observer with 8 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player, Added&gt; trigger, Query&lt;Health&gt; query) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, TEvent, T0, T1, T2, T3, T4, T5, T6, T7>(
        this App app,
        Action<On<TComponent, TEvent>, T0, T1, T2, T3, T4, T5, T6, T7> callback)
        where TComponent : struct
        where TEvent : struct, IObserverEvent
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
        where T6 : ISystemParam, new()
        where T7 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7() };
        return app.AddObserverInternal(new ObserverWithParams<TComponent, TEvent, T0, T1, T2, T3, T4, T5, T6, T7>(callback, parameters));
    }

    /// <summary>
    /// Register an observer that matches all events with 8 system parameter(s).
    /// Example: app.AddObserver((On&lt;Player&gt; trigger, Commands commands) =&gt; { ... });
    /// </summary>
    public static App AddObserver<TComponent, T0, T1, T2, T3, T4, T5, T6, T7>(
        this App app,
        Action<On<TComponent>, T0, T1, T2, T3, T4, T5, T6, T7> callback)
        where TComponent : struct
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
        where T6 : ISystemParam, new()
        where T7 : ISystemParam, new()
    {
        var parameters = new ISystemParam[] { new T0(), new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7() };
        return app.AddObserverInternal(new ObserverAnyWithParams<TComponent, T0, T1, T2, T3, T4, T5, T6, T7>(callback, parameters));
    }

}

// Internal observer implementations with system parameters

internal sealed class ObserverWithParams<TComponent, TEvent, T0> : IObserver
    where TComponent : struct
    where TEvent : struct, IObserverEvent
    where T0 : ISystemParam, new()
{
    private readonly Action<On<TComponent, TEvent>, T0> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverWithParams(Action<On<TComponent, TEvent>, T0> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(TEvent);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent, TEvent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0]);
    }
}

internal sealed class ObserverAnyWithParams<TComponent, T0> : IObserver
    where TComponent : struct
    where T0 : ISystemParam, new()
{
    private readonly Action<On<TComponent>, T0> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverAnyWithParams(Action<On<TComponent>, T0> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(Any);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0]);
    }
}


internal sealed class ObserverWithParams<TComponent, TEvent, T0, T1> : IObserver
    where TComponent : struct
    where TEvent : struct, IObserverEvent
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
{
    private readonly Action<On<TComponent, TEvent>, T0, T1> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverWithParams(Action<On<TComponent, TEvent>, T0, T1> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(TEvent);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent, TEvent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1]);
    }
}

internal sealed class ObserverAnyWithParams<TComponent, T0, T1> : IObserver
    where TComponent : struct
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
{
    private readonly Action<On<TComponent>, T0, T1> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverAnyWithParams(Action<On<TComponent>, T0, T1> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(Any);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1]);
    }
}


internal sealed class ObserverWithParams<TComponent, TEvent, T0, T1, T2> : IObserver
    where TComponent : struct
    where TEvent : struct, IObserverEvent
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
    where T2 : ISystemParam, new()
{
    private readonly Action<On<TComponent, TEvent>, T0, T1, T2> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverWithParams(Action<On<TComponent, TEvent>, T0, T1, T2> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(TEvent);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent, TEvent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1], (T2)_parameters[2]);
    }
}

internal sealed class ObserverAnyWithParams<TComponent, T0, T1, T2> : IObserver
    where TComponent : struct
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
    where T2 : ISystemParam, new()
{
    private readonly Action<On<TComponent>, T0, T1, T2> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverAnyWithParams(Action<On<TComponent>, T0, T1, T2> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(Any);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1], (T2)_parameters[2]);
    }
}


internal sealed class ObserverWithParams<TComponent, TEvent, T0, T1, T2, T3> : IObserver
    where TComponent : struct
    where TEvent : struct, IObserverEvent
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
    where T2 : ISystemParam, new()
    where T3 : ISystemParam, new()
{
    private readonly Action<On<TComponent, TEvent>, T0, T1, T2, T3> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverWithParams(Action<On<TComponent, TEvent>, T0, T1, T2, T3> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(TEvent);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent, TEvent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1], (T2)_parameters[2], (T3)_parameters[3]);
    }
}

internal sealed class ObserverAnyWithParams<TComponent, T0, T1, T2, T3> : IObserver
    where TComponent : struct
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
    where T2 : ISystemParam, new()
    where T3 : ISystemParam, new()
{
    private readonly Action<On<TComponent>, T0, T1, T2, T3> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverAnyWithParams(Action<On<TComponent>, T0, T1, T2, T3> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(Any);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1], (T2)_parameters[2], (T3)_parameters[3]);
    }
}


internal sealed class ObserverWithParams<TComponent, TEvent, T0, T1, T2, T3, T4> : IObserver
    where TComponent : struct
    where TEvent : struct, IObserverEvent
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
    where T2 : ISystemParam, new()
    where T3 : ISystemParam, new()
    where T4 : ISystemParam, new()
{
    private readonly Action<On<TComponent, TEvent>, T0, T1, T2, T3, T4> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverWithParams(Action<On<TComponent, TEvent>, T0, T1, T2, T3, T4> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(TEvent);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent, TEvent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1], (T2)_parameters[2], (T3)_parameters[3], (T4)_parameters[4]);
    }
}

internal sealed class ObserverAnyWithParams<TComponent, T0, T1, T2, T3, T4> : IObserver
    where TComponent : struct
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
    where T2 : ISystemParam, new()
    where T3 : ISystemParam, new()
    where T4 : ISystemParam, new()
{
    private readonly Action<On<TComponent>, T0, T1, T2, T3, T4> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverAnyWithParams(Action<On<TComponent>, T0, T1, T2, T3, T4> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(Any);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1], (T2)_parameters[2], (T3)_parameters[3], (T4)_parameters[4]);
    }
}


internal sealed class ObserverWithParams<TComponent, TEvent, T0, T1, T2, T3, T4, T5> : IObserver
    where TComponent : struct
    where TEvent : struct, IObserverEvent
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
    where T2 : ISystemParam, new()
    where T3 : ISystemParam, new()
    where T4 : ISystemParam, new()
    where T5 : ISystemParam, new()
{
    private readonly Action<On<TComponent, TEvent>, T0, T1, T2, T3, T4, T5> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverWithParams(Action<On<TComponent, TEvent>, T0, T1, T2, T3, T4, T5> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(TEvent);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent, TEvent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1], (T2)_parameters[2], (T3)_parameters[3], (T4)_parameters[4], (T5)_parameters[5]);
    }
}

internal sealed class ObserverAnyWithParams<TComponent, T0, T1, T2, T3, T4, T5> : IObserver
    where TComponent : struct
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
    where T2 : ISystemParam, new()
    where T3 : ISystemParam, new()
    where T4 : ISystemParam, new()
    where T5 : ISystemParam, new()
{
    private readonly Action<On<TComponent>, T0, T1, T2, T3, T4, T5> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverAnyWithParams(Action<On<TComponent>, T0, T1, T2, T3, T4, T5> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(Any);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1], (T2)_parameters[2], (T3)_parameters[3], (T4)_parameters[4], (T5)_parameters[5]);
    }
}


internal sealed class ObserverWithParams<TComponent, TEvent, T0, T1, T2, T3, T4, T5, T6> : IObserver
    where TComponent : struct
    where TEvent : struct, IObserverEvent
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
    where T2 : ISystemParam, new()
    where T3 : ISystemParam, new()
    where T4 : ISystemParam, new()
    where T5 : ISystemParam, new()
    where T6 : ISystemParam, new()
{
    private readonly Action<On<TComponent, TEvent>, T0, T1, T2, T3, T4, T5, T6> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverWithParams(Action<On<TComponent, TEvent>, T0, T1, T2, T3, T4, T5, T6> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(TEvent);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent, TEvent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1], (T2)_parameters[2], (T3)_parameters[3], (T4)_parameters[4], (T5)_parameters[5], (T6)_parameters[6]);
    }
}

internal sealed class ObserverAnyWithParams<TComponent, T0, T1, T2, T3, T4, T5, T6> : IObserver
    where TComponent : struct
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
    where T2 : ISystemParam, new()
    where T3 : ISystemParam, new()
    where T4 : ISystemParam, new()
    where T5 : ISystemParam, new()
    where T6 : ISystemParam, new()
{
    private readonly Action<On<TComponent>, T0, T1, T2, T3, T4, T5, T6> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverAnyWithParams(Action<On<TComponent>, T0, T1, T2, T3, T4, T5, T6> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(Any);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1], (T2)_parameters[2], (T3)_parameters[3], (T4)_parameters[4], (T5)_parameters[5], (T6)_parameters[6]);
    }
}


internal sealed class ObserverWithParams<TComponent, TEvent, T0, T1, T2, T3, T4, T5, T6, T7> : IObserver
    where TComponent : struct
    where TEvent : struct, IObserverEvent
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
    where T2 : ISystemParam, new()
    where T3 : ISystemParam, new()
    where T4 : ISystemParam, new()
    where T5 : ISystemParam, new()
    where T6 : ISystemParam, new()
    where T7 : ISystemParam, new()
{
    private readonly Action<On<TComponent, TEvent>, T0, T1, T2, T3, T4, T5, T6, T7> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverWithParams(Action<On<TComponent, TEvent>, T0, T1, T2, T3, T4, T5, T6, T7> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(TEvent);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent, TEvent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1], (T2)_parameters[2], (T3)_parameters[3], (T4)_parameters[4], (T5)_parameters[5], (T6)_parameters[6], (T7)_parameters[7]);
    }
}

internal sealed class ObserverAnyWithParams<TComponent, T0, T1, T2, T3, T4, T5, T6, T7> : IObserver
    where TComponent : struct
    where T0 : ISystemParam, new()
    where T1 : ISystemParam, new()
    where T2 : ISystemParam, new()
    where T3 : ISystemParam, new()
    where T4 : ISystemParam, new()
    where T5 : ISystemParam, new()
    where T6 : ISystemParam, new()
    where T7 : ISystemParam, new()
{
    private readonly Action<On<TComponent>, T0, T1, T2, T3, T4, T5, T6, T7> _callback;
    private readonly ISystemParam[] _parameters;

    public ObserverAnyWithParams(Action<On<TComponent>, T0, T1, T2, T3, T4, T5, T6, T7> callback, ISystemParam[] parameters)
    {
        _callback = callback;
        _parameters = parameters;
    }

    public Type ComponentType => typeof(TComponent);
    public Type EventType => typeof(Any);

    public void Trigger(TinyWorld world, ObserverTrigger trigger)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        // Get component data
        var componentId = world.Component<TComponent>();
        TComponent component = default;
        if (world.Has(trigger.Entity, componentId))
        {
            component = world.Get<TComponent>(trigger.Entity, componentId);
        }

        var on = new On<TComponent>(trigger.Entity, component);
        _callback(on, (T0)_parameters[0], (T1)_parameters[1], (T2)_parameters[2], (T3)_parameters[3], (T4)_parameters[4], (T5)_parameters[5], (T6)_parameters[6], (T7)_parameters[7]);
    }
}

