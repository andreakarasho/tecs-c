using System;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// State resource for managing application states.
/// </summary>
public sealed class State<T> where T : struct, Enum
{
    private T _current;
    private T? _next;

    public State(T initial)
    {
        _current = initial;
    }

    /// <summary>
    /// Get the current state.
    /// </summary>
    public T Current => _current;

    /// <summary>
    /// Get the next state if a transition is pending.
    /// </summary>
    public T? Next => _next;

    /// <summary>
    /// Set the next state (transition occurs at end of frame).
    /// </summary>
    public void Set(T newState)
    {
        _next = newState;
    }

    /// <summary>
    /// Check if currently in a specific state.
    /// </summary>
    public bool InState(T state)
    {
        return _current.Equals(state);
    }

    /// <summary>
    /// Apply pending state transition (called internally).
    /// </summary>
    internal bool ApplyTransition()
    {
        if (_next.HasValue)
        {
            _current = _next.Value;
            _next = null;
            return true;
        }
        return false;
    }
}

/// <summary>
/// System parameter for accessing state (read-only).
/// </summary>
public sealed class StateParam<T> : ISystemParam where T : struct, Enum
{
    private State<T>? _state;

    public void Initialize(TinyWorld world)
    {
        if (!world.HasResource<State<T>>())
        {
            throw new InvalidOperationException($"State<{typeof(T).Name}> resource does not exist. Call app.AddState<{typeof(T).Name}>() first.");
        }
    }

    public void Fetch(TinyWorld world)
    {
        _state = world.GetResource<State<T>>();
    }

    public SystemParamAccess GetAccess()
    {
        var access = new SystemParamAccess();
        access.ReadResources.Add(typeof(State<T>));
        return access;
    }

    /// <summary>
    /// Get the current state value.
    /// </summary>
    public T Get() => _state!.Current;

    /// <summary>
    /// Check if in a specific state.
    /// </summary>
    public bool InState(T state) => _state!.InState(state);
}

/// <summary>
/// System parameter for mutating state.
/// </summary>
public sealed class NextState<T> : ISystemParam where T : struct, Enum
{
    private State<T>? _state;

    public void Initialize(TinyWorld world)
    {
        if (!world.HasResource<State<T>>())
        {
            throw new InvalidOperationException($"State<{typeof(T).Name}> resource does not exist. Call app.AddState<{typeof(T).Name}>() first.");
        }
    }

    public void Fetch(TinyWorld world)
    {
        _state = world.GetResource<State<T>>();
    }

    public SystemParamAccess GetAccess()
    {
        var access = new SystemParamAccess();
        access.WriteResources.Add(typeof(State<T>));
        return access;
    }

    /// <summary>
    /// Set the next state.
    /// </summary>
    public void Set(T newState)
    {
        _state!.Set(newState);
    }
}

/// <summary>
/// Internal system for applying state transitions.
/// </summary>
internal sealed class StateTransitionSystem<T> : ISystem where T : struct, Enum
{
    public void Run(TinyWorld world)
    {
        if (world.TryGetResource<State<T>>(out var state))
        {
            state.ApplyTransition();
        }
    }

    public SystemParamAccess? GetAccess()
    {
        var access = new SystemParamAccess();
        access.WriteResources.Add(typeof(State<T>));
        return access;
    }

    public bool ShouldRun(TinyWorld world) => true;
}

/// <summary>
/// Run condition for state-based systems.
/// </summary>
public static class StateConditions
{
    /// <summary>
    /// System runs only when in a specific state.
    /// </summary>
    public static RunCondition InState<T>(T state) where T : struct, Enum
    {
        return world =>
        {
            if (world.TryGetResource<State<T>>(out var stateResource))
            {
                return stateResource.InState(state);
            }
            return false;
        };
    }
}

/// <summary>
/// Extension methods for state management.
/// </summary>
public static class StateExtensions
{
    /// <summary>
    /// Add a state resource with an initial value.
    /// </summary>
    public static App AddState<T>(this App app, T initialState) where T : struct, Enum
    {
        app.World.SetResource(new State<T>(initialState));

        // Add state transition system at end of PostUpdate
        var transitionSystem = new StateTransitionSystem<T>();

        app.AddSystemToStage("PostUpdate", transitionSystem)
           .Label($"StateTransition<{typeof(T).Name}>");

        return app;
    }

    /// <summary>
    /// Add a system that only runs in a specific state.
    /// </summary>
    public static SystemConfigurator InState<T>(this SystemConfigurator configurator, T state)
        where T : struct, Enum
    {
        return configurator.RunIf(StateConditions.InState(state));
    }
}
