using System;
using System.Linq;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Wrapper for systems that use ISystemParam dependency injection.
/// Manages parameter lifecycle and execution.
/// </summary>
internal sealed class ParameterizedSystem : ISystem
{
    private readonly ISystemParam[] _parameters;
    private readonly Action _systemFunc;
    private readonly SystemParamAccess? _access;
    private bool _initialized;
    private RunCondition? _runCondition;

    public ParameterizedSystem(ISystemParam[] parameters, Action systemFunc, RunCondition? runCondition = null)
    {
        _parameters = parameters;
        _systemFunc = systemFunc;
        _runCondition = runCondition;

        // Aggregate access patterns from all parameters
        var hasAnyAccess = false;
        var aggregated = new SystemParamAccess();

        foreach (var param in parameters)
        {
            var access = param.GetAccess();
            if (access != null)
            {
                hasAnyAccess = true;
                foreach (var read in access.ReadResources)
                    aggregated.ReadResources.Add(read);
                foreach (var write in access.WriteResources)
                    aggregated.WriteResources.Add(write);
            }
        }

        _access = hasAnyAccess ? aggregated : null;
    }

    public void Run(TinyWorld world)
    {
        // Initialize parameters on first run
        if (!_initialized)
        {
            foreach (var param in _parameters)
            {
                param.Initialize(world);
            }
            _initialized = true;
        }

        // Fetch fresh data for all parameters
        foreach (var param in _parameters)
        {
            param.Fetch(world);
        }

        // Execute the user's system function
        _systemFunc();

        // Apply deferred commands if any Commands parameters exist
        foreach (var param in _parameters)
        {
            if (param is Commands commands)
            {
                commands.Apply();
            }
        }
    }

    public SystemParamAccess? GetAccess()
    {
        return _access;
    }

    public bool ShouldRun(TinyWorld world)
    {
        return _runCondition == null || _runCondition(world);
    }

    /// <summary>
    /// Set or update the run condition for this system.
    /// </summary>
    public void SetRunCondition(RunCondition? condition)
    {
        _runCondition = condition;
    }
}
