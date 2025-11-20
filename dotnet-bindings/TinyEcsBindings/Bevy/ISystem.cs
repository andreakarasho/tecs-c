namespace TinyEcsBindings.Bevy;

/// <summary>
/// Base interface for systems that can be executed by the scheduler.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Run the system logic.
    /// </summary>
    void Run(TinyWorld world);

    /// <summary>
    /// Get the resource access pattern for this system (for parallel scheduling).
    /// Returns null if the system doesn't support parallel execution analysis.
    /// </summary>
    SystemParamAccess? GetAccess();

    /// <summary>
    /// Check if this system should run given the current world state.
    /// </summary>
    bool ShouldRun(TinyWorld world);
}
