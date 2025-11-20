using System;
using System.Collections.Generic;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Marker interface for system set types.
/// System sets group systems together for configuration.
/// </summary>
public interface ISystemSet
{
}

/// <summary>
/// Configuration for a system set.
/// </summary>
public sealed class SystemSetConfig
{
    internal readonly Type SetType;
    internal readonly HashSet<Type> BeforeSets = new();
    internal readonly HashSet<Type> AfterSets = new();
    internal RunCondition? RunCondition;

    internal SystemSetConfig(Type setType)
    {
        SetType = setType;
    }
}

/// <summary>
/// Fluent API for configuring system sets.
/// </summary>
public sealed class SystemSetConfigurator
{
    private readonly SystemSetConfig _config;
    private readonly App _app;

    internal SystemSetConfigurator(SystemSetConfig config, App app)
    {
        _config = config;
        _app = app;
    }

    /// <summary>
    /// This set runs after another set.
    /// </summary>
    public SystemSetConfigurator After<TOtherSet>() where TOtherSet : ISystemSet
    {
        _config.AfterSets.Add(typeof(TOtherSet));
        return this;
    }

    /// <summary>
    /// This set runs before another set.
    /// </summary>
    public SystemSetConfigurator Before<TOtherSet>() where TOtherSet : ISystemSet
    {
        _config.BeforeSets.Add(typeof(TOtherSet));
        return this;
    }

    /// <summary>
    /// Set a run condition for all systems in this set.
    /// </summary>
    public SystemSetConfigurator RunIf(RunCondition condition)
    {
        _config.RunCondition = condition;
        return this;
    }

    /// <summary>
    /// Get the app to continue configuration.
    /// </summary>
    public App App => _app;
}

/// <summary>
/// Extension methods for system sets.
/// </summary>
public static class SystemSetExtensions
{
    /// <summary>
    /// Configure a system set.
    /// </summary>
    public static SystemSetConfigurator ConfigureSet<TSet>(this App app) where TSet : ISystemSet
    {
        return app.ConfigureSetInternal(typeof(TSet));
    }

    /// <summary>
    /// Add a system to a set.
    /// </summary>
    public static SystemConfigurator InSet<TSet>(this SystemConfigurator configurator) where TSet : ISystemSet
    {
        return configurator.InSetInternal(typeof(TSet));
    }
}

/// <summary>
/// Example system sets for common groupings.
/// </summary>
public struct UpdateSet : ISystemSet { }
public struct RenderSet : ISystemSet { }
public struct PhysicsSet : ISystemSet { }
public struct InputSet : ISystemSet { }
public struct AISet : ISystemSet { }
