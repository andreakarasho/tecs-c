using System;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Marker interface for exclusive systems.
/// Exclusive systems get full access to the world and never run in parallel with other systems.
/// </summary>
public interface IExclusiveSystem : ISystem
{
}

/// <summary>
/// Base class for exclusive systems that need direct world access.
/// </summary>
public abstract class ExclusiveSystem : IExclusiveSystem
{
    public abstract void Run(TinyWorld world);

    public SystemParamAccess? GetAccess()
    {
        // Exclusive systems claim exclusive access to everything
        return null; // null indicates exclusive access
    }

    public virtual bool ShouldRun(TinyWorld world) => true;
}

/// <summary>
/// Extension methods for exclusive systems.
/// </summary>
public static class ExclusiveSystemExtensions
{
    /// <summary>
    /// Mark this system as exclusive (runs alone, never in parallel).
    /// </summary>
    public static SystemConfigurator Exclusive(this SystemConfigurator configurator)
    {
        return configurator.ExclusiveInternal();
    }
}
