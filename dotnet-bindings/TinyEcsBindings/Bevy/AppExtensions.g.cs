using System;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Extension methods for App that accept Action delegates directly.
/// Allows: app.AddSystem((Query<Data<Position>> query) => { ... })
/// </summary>
public static class AppExtensions
{
    /// <summary>
    /// Add a system with no parameters to the Update stage.
    /// </summary>
    public static SystemConfigurator AddSystem(this App app, Action system)
    {
        return app.AddSystem(SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with no parameters to a specific stage.
    /// </summary>
    public static SystemConfigurator AddSystemToStage(this App app, string stageName, Action system)
    {
        return app.AddSystemToStage(stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 1 parameter(s) to the Update stage.
    /// </summary>
    public static SystemConfigurator AddSystem<T0>(
        this App app,
        Action<T0> system)
        where T0 : ISystemParam, new()
    {
        return app.AddSystem(SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 1 parameter(s) to a specific stage.
    /// </summary>
    public static SystemConfigurator AddSystemToStage<T0>(
        this App app,
        string stageName,
        Action<T0> system)
        where T0 : ISystemParam, new()
    {
        return app.AddSystemToStage(stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 1 parameter(s) to a custom schedule.
    /// </summary>
    public static SystemConfigurator AddSystemToSchedule<T0>(
        this App app,
        string scheduleName,
        string stageName,
        Action<T0> system)
        where T0 : ISystemParam, new()
    {
        return app.AddSystemToScheduleInternal(scheduleName, stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 2 parameter(s) to the Update stage.
    /// </summary>
    public static SystemConfigurator AddSystem<T0, T1>(
        this App app,
        Action<T0, T1> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
    {
        return app.AddSystem(SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 2 parameter(s) to a specific stage.
    /// </summary>
    public static SystemConfigurator AddSystemToStage<T0, T1>(
        this App app,
        string stageName,
        Action<T0, T1> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
    {
        return app.AddSystemToStage(stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 2 parameter(s) to a custom schedule.
    /// </summary>
    public static SystemConfigurator AddSystemToSchedule<T0, T1>(
        this App app,
        string scheduleName,
        string stageName,
        Action<T0, T1> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
    {
        return app.AddSystemToScheduleInternal(scheduleName, stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 3 parameter(s) to the Update stage.
    /// </summary>
    public static SystemConfigurator AddSystem<T0, T1, T2>(
        this App app,
        Action<T0, T1, T2> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
    {
        return app.AddSystem(SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 3 parameter(s) to a specific stage.
    /// </summary>
    public static SystemConfigurator AddSystemToStage<T0, T1, T2>(
        this App app,
        string stageName,
        Action<T0, T1, T2> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
    {
        return app.AddSystemToStage(stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 3 parameter(s) to a custom schedule.
    /// </summary>
    public static SystemConfigurator AddSystemToSchedule<T0, T1, T2>(
        this App app,
        string scheduleName,
        string stageName,
        Action<T0, T1, T2> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
    {
        return app.AddSystemToScheduleInternal(scheduleName, stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 4 parameter(s) to the Update stage.
    /// </summary>
    public static SystemConfigurator AddSystem<T0, T1, T2, T3>(
        this App app,
        Action<T0, T1, T2, T3> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
    {
        return app.AddSystem(SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 4 parameter(s) to a specific stage.
    /// </summary>
    public static SystemConfigurator AddSystemToStage<T0, T1, T2, T3>(
        this App app,
        string stageName,
        Action<T0, T1, T2, T3> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
    {
        return app.AddSystemToStage(stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 4 parameter(s) to a custom schedule.
    /// </summary>
    public static SystemConfigurator AddSystemToSchedule<T0, T1, T2, T3>(
        this App app,
        string scheduleName,
        string stageName,
        Action<T0, T1, T2, T3> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
    {
        return app.AddSystemToScheduleInternal(scheduleName, stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 5 parameter(s) to the Update stage.
    /// </summary>
    public static SystemConfigurator AddSystem<T0, T1, T2, T3, T4>(
        this App app,
        Action<T0, T1, T2, T3, T4> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
    {
        return app.AddSystem(SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 5 parameter(s) to a specific stage.
    /// </summary>
    public static SystemConfigurator AddSystemToStage<T0, T1, T2, T3, T4>(
        this App app,
        string stageName,
        Action<T0, T1, T2, T3, T4> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
    {
        return app.AddSystemToStage(stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 5 parameter(s) to a custom schedule.
    /// </summary>
    public static SystemConfigurator AddSystemToSchedule<T0, T1, T2, T3, T4>(
        this App app,
        string scheduleName,
        string stageName,
        Action<T0, T1, T2, T3, T4> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
    {
        return app.AddSystemToScheduleInternal(scheduleName, stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 6 parameter(s) to the Update stage.
    /// </summary>
    public static SystemConfigurator AddSystem<T0, T1, T2, T3, T4, T5>(
        this App app,
        Action<T0, T1, T2, T3, T4, T5> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
    {
        return app.AddSystem(SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 6 parameter(s) to a specific stage.
    /// </summary>
    public static SystemConfigurator AddSystemToStage<T0, T1, T2, T3, T4, T5>(
        this App app,
        string stageName,
        Action<T0, T1, T2, T3, T4, T5> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
    {
        return app.AddSystemToStage(stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 6 parameter(s) to a custom schedule.
    /// </summary>
    public static SystemConfigurator AddSystemToSchedule<T0, T1, T2, T3, T4, T5>(
        this App app,
        string scheduleName,
        string stageName,
        Action<T0, T1, T2, T3, T4, T5> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
    {
        return app.AddSystemToScheduleInternal(scheduleName, stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 7 parameter(s) to the Update stage.
    /// </summary>
    public static SystemConfigurator AddSystem<T0, T1, T2, T3, T4, T5, T6>(
        this App app,
        Action<T0, T1, T2, T3, T4, T5, T6> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
        where T6 : ISystemParam, new()
    {
        return app.AddSystem(SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 7 parameter(s) to a specific stage.
    /// </summary>
    public static SystemConfigurator AddSystemToStage<T0, T1, T2, T3, T4, T5, T6>(
        this App app,
        string stageName,
        Action<T0, T1, T2, T3, T4, T5, T6> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
        where T6 : ISystemParam, new()
    {
        return app.AddSystemToStage(stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 7 parameter(s) to a custom schedule.
    /// </summary>
    public static SystemConfigurator AddSystemToSchedule<T0, T1, T2, T3, T4, T5, T6>(
        this App app,
        string scheduleName,
        string stageName,
        Action<T0, T1, T2, T3, T4, T5, T6> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
        where T6 : ISystemParam, new()
    {
        return app.AddSystemToScheduleInternal(scheduleName, stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 8 parameter(s) to the Update stage.
    /// </summary>
    public static SystemConfigurator AddSystem<T0, T1, T2, T3, T4, T5, T6, T7>(
        this App app,
        Action<T0, T1, T2, T3, T4, T5, T6, T7> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
        where T6 : ISystemParam, new()
        where T7 : ISystemParam, new()
    {
        return app.AddSystem(SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 8 parameter(s) to a specific stage.
    /// </summary>
    public static SystemConfigurator AddSystemToStage<T0, T1, T2, T3, T4, T5, T6, T7>(
        this App app,
        string stageName,
        Action<T0, T1, T2, T3, T4, T5, T6, T7> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
        where T6 : ISystemParam, new()
        where T7 : ISystemParam, new()
    {
        return app.AddSystemToStage(stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with 8 parameter(s) to a custom schedule.
    /// </summary>
    public static SystemConfigurator AddSystemToSchedule<T0, T1, T2, T3, T4, T5, T6, T7>(
        this App app,
        string scheduleName,
        string stageName,
        Action<T0, T1, T2, T3, T4, T5, T6, T7> system)
        where T0 : ISystemParam, new()
        where T1 : ISystemParam, new()
        where T2 : ISystemParam, new()
        where T3 : ISystemParam, new()
        where T4 : ISystemParam, new()
        where T5 : ISystemParam, new()
        where T6 : ISystemParam, new()
        where T7 : ISystemParam, new()
    {
        return app.AddSystemToScheduleInternal(scheduleName, stageName, SystemAdapters.Create(system));
    }

    /// <summary>
    /// Add a system with no parameters to a custom schedule.
    /// </summary>
    public static SystemConfigurator AddSystemToSchedule(
        this App app,
        string scheduleName,
        string stageName,
        Action system)
    {
        return app.AddSystemToScheduleInternal(scheduleName, stageName, SystemAdapters.Create(system));
    }
}
