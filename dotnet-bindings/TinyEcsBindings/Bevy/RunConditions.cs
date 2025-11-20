using System;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Delegate for run condition functions.
/// </summary>
public delegate bool RunCondition(TinyWorld world);

/// <summary>
/// Common run conditions for systems.
/// </summary>
public static class RunConditions
{
    /// <summary>
    /// System runs only if the specified resource exists.
    /// </summary>
    public static RunCondition ResourceExists<T>() where T : notnull
    {
        return world => world.HasResource<T>();
    }

    /// <summary>
    /// System runs only if the specified resource does NOT exist.
    /// </summary>
    public static RunCondition ResourceNotExists<T>() where T : notnull
    {
        return world => !world.HasResource<T>();
    }

    /// <summary>
    /// System runs only if the predicate returns true for the resource.
    /// </summary>
    public static RunCondition ResourceMatches<T>(Func<T, bool> predicate) where T : notnull
    {
        return world =>
        {
            if (world.TryGetResource<T>(out var resource))
            {
                return predicate(resource!);
            }
            return false;
        };
    }

    /// <summary>
    /// System runs when ANY of the provided conditions are true (OR).
    /// </summary>
    public static RunCondition Any(params RunCondition[] conditions)
    {
        return world =>
        {
            foreach (var condition in conditions)
            {
                if (condition(world))
                    return true;
            }
            return false;
        };
    }

    /// <summary>
    /// System runs when ALL of the provided conditions are true (AND).
    /// </summary>
    public static RunCondition All(params RunCondition[] conditions)
    {
        return world =>
        {
            foreach (var condition in conditions)
            {
                if (!condition(world))
                    return false;
            }
            return true;
        };
    }

    /// <summary>
    /// System runs when the condition is NOT true.
    /// </summary>
    public static RunCondition Not(RunCondition condition)
    {
        return world => !condition(world);
    }

    /// <summary>
    /// Always run (default behavior).
    /// </summary>
    public static RunCondition Always()
    {
        return _ => true;
    }

    /// <summary>
    /// Never run (useful for temporarily disabling systems).
    /// </summary>
    public static RunCondition Never()
    {
        return _ => false;
    }
}
