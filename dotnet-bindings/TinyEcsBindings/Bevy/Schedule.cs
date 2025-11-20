using System;
using System.Collections.Generic;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// A named schedule containing multiple stages.
/// Schedules can be run independently (e.g., FixedUpdate, Render).
/// </summary>
public sealed class Schedule
{
    private readonly List<Stage> _stages = new();
    private readonly Dictionary<string, Stage> _stagesByName = new();

    public Schedule(string name)
    {
        Name = name;
    }

    public string Name { get; }

    /// <summary>
    /// Add a stage to this schedule.
    /// </summary>
    public Schedule AddStage(Stage stage)
    {
        _stages.Add(stage);
        _stagesByName[stage.Name] = stage;
        return this;
    }

    /// <summary>
    /// Check if a stage exists in this schedule.
    /// </summary>
    public bool HasStage(string name)
    {
        return _stagesByName.ContainsKey(name);
    }

    /// <summary>
    /// Get a stage by name.
    /// </summary>
    public Stage? GetStage(string name)
    {
        _stagesByName.TryGetValue(name, out var stage);
        return stage;
    }

    /// <summary>
    /// Run all stages in this schedule.
    /// </summary>
    public void Run(TinyWorld world)
    {
        foreach (var stage in _stages)
        {
            stage.Execute(world);
        }
    }

    /// <summary>
    /// Build execution schedules for all stages.
    /// </summary>
    internal void BuildSchedules(Dictionary<Type, SystemSetConfig> systemSets)
    {
        foreach (var stage in _stages)
        {
            stage.BuildSchedule(systemSets);
        }
    }
}

/// <summary>
/// Extension methods for custom schedules.
/// </summary>
public static class ScheduleExtensions
{
    /// <summary>
    /// Add a custom schedule to the app.
    /// </summary>
    public static App AddSchedule(this App app, Schedule schedule)
    {
        return app.AddScheduleInternal(schedule);
    }

    /// <summary>
    /// Get a schedule by name.
    /// </summary>
    public static Schedule? GetSchedule(this App app, string name)
    {
        return app.GetScheduleInternal(name);
    }

    /// <summary>
    /// Run a custom schedule by name.
    /// </summary>
    public static void RunSchedule(this App app, string name)
    {
        app.RunScheduleInternal(name);
    }

    /// <summary>
    /// Add a system to a custom schedule.
    /// </summary>
    public static SystemConfigurator AddSystemToSchedule(this App app, string scheduleName, string stageName, ISystem system)
    {
        return app.AddSystemToScheduleInternal(scheduleName, stageName, system);
    }
}

/// <summary>
/// Common schedule labels.
/// </summary>
public static class CoreSchedules
{
    /// <summary>
    /// The main schedule that runs every frame.
    /// </summary>
    public const string Main = "Main";

    /// <summary>
    /// Schedule for fixed timestep updates (physics, etc).
    /// </summary>
    public const string FixedUpdate = "FixedUpdate";

    /// <summary>
    /// Schedule for rendering operations.
    /// </summary>
    public const string Render = "Render";

    /// <summary>
    /// Schedule for startup initialization.
    /// </summary>
    public const string Startup = "Startup";
}
