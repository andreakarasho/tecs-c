using System;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Interface for system parameters that can be injected into systems.
/// System parameters are fetched before each system execution and provide access to world data.
/// </summary>
public interface ISystemParam
{
    /// <summary>
    /// Called once when the system is first created.
    /// Use this for one-time initialization.
    /// </summary>
    void Initialize(TinyWorld world);

    /// <summary>
    /// Called before each system execution to refresh the parameter's data.
    /// </summary>
    void Fetch(TinyWorld world);

    /// <summary>
    /// Returns the resource access pattern for this parameter.
    /// Used for parallel system scheduling to detect conflicts.
    /// </summary>
    SystemParamAccess GetAccess();
}
