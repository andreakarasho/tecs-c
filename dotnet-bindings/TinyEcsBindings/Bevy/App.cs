using System;
using System.Collections.Generic;
using System.Linq;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Bevy-style application with stages and system scheduling.
/// </summary>
public sealed class App
{
    private readonly TinyWorld _world;
    private readonly List<Stage> _stages = new();
    private readonly Dictionary<string, Stage> _stagesByName = new();
    private readonly Dictionary<Type, SystemSetConfig> _systemSets = new();
    private readonly Dictionary<string, Schedule> _schedules = new();
    private readonly ObserverRegistry _observers = new();
    private bool _scheduleDirty = true;

    public App(TinyWorld world)
    {
        _world = world;

        // Store observer registry as a resource for Commands to access
        _world.SetResource(_observers);

        // Create default main schedule with default stages
        var mainSchedule = new Schedule(CoreSchedules.Main);
        mainSchedule.AddStage(new Stage("Startup"));
        mainSchedule.AddStage(new Stage("First"));
        mainSchedule.AddStage(new Stage("PreUpdate"));
        mainSchedule.AddStage(new Stage("Update"));
        mainSchedule.AddStage(new Stage("PostUpdate"));
        mainSchedule.AddStage(new Stage("Last"));

        _schedules[CoreSchedules.Main] = mainSchedule;

        // Also add stages to the app for backwards compatibility
        foreach (var stage in new[] { "Startup", "First", "PreUpdate", "Update", "PostUpdate", "Last" })
        {
            _stagesByName[stage] = mainSchedule.GetStage(stage)!;
            _stages.Add(mainSchedule.GetStage(stage)!);
        }
    }

    public TinyWorld World => _world;

    /// <summary>
    /// Add a custom stage.
    /// </summary>
    public App AddStage(Stage stage)
    {
        _stages.Add(stage);
        _stagesByName[stage.Name] = stage;
        _scheduleDirty = true;
        return this;
    }

    /// <summary>
    /// Check if a stage exists.
    /// </summary>
    public bool HasStage(string name)
    {
        return _stagesByName.ContainsKey(name);
    }

    /// <summary>
    /// Add a stage with ordering constraints.
    /// </summary>
    public App AddStage(string name, string? after = null, string? before = null)
    {
        // Create stage and insert it at appropriate position
        var stage = new Stage(name);

        int insertIndex = _stages.Count;
        if (after != null && _stagesByName.TryGetValue(after, out var afterStage))
        {
            insertIndex = _stages.IndexOf(afterStage) + 1;
        }
        else if (before != null && _stagesByName.TryGetValue(before, out var beforeStage))
        {
            insertIndex = _stages.IndexOf(beforeStage);
        }

        _stages.Insert(insertIndex, stage);
        _stagesByName[name] = stage;
        _scheduleDirty = true;
        return this;
    }

    /// <summary>
    /// Get a stage by name.
    /// </summary>
    public Stage GetStage(string name)
    {
        if (!_stagesByName.TryGetValue(name, out var stage))
        {
            throw new InvalidOperationException($"Stage '{name}' does not exist");
        }
        return stage;
    }

    /// <summary>
    /// Add a system to the Update stage.
    /// </summary>
    public SystemConfigurator AddSystem(ISystem system)
    {
        return AddSystemToStage("Update", system);
    }

    /// <summary>
    /// Add a system to a specific stage.
    /// </summary>
    public SystemConfigurator AddSystemToStage(string stageName, ISystem system)
    {
        var stage = GetStage(stageName);
        stage.AddSystem(system);
        _scheduleDirty = true;
        return new SystemConfigurator(system, stage);
    }

    /// <summary>
    /// Run the Startup stage once.
    /// </summary>
    public void RunStartup()
    {
        if (_scheduleDirty)
        {
            RebuildSchedules();
        }

        var startupStage = GetStage("Startup");
        startupStage.Execute(_world);
    }

    /// <summary>
    /// Run all non-startup stages once (one frame).
    /// </summary>
    public void Update()
    {
        if (_scheduleDirty)
        {
            RebuildSchedules();
        }

        foreach (var stage in _stages)
        {
            if (stage.Name == "Startup")
                continue;

            stage.Execute(_world);
        }

        // Clean up events at the end of each frame
        CleanupEvents();
    }

    /// <summary>
    /// Cleanup events from all Events<T> resources.
    /// </summary>
    private void CleanupEvents()
    {
        // Get all Events<T> resources and call Update() on them
        var eventTypes = ResourceExtensions.GetAllResourceTypes(_world)
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Events<>));

        foreach (var eventType in eventTypes)
        {
            var eventsResource = ResourceExtensions.GetResourceByType(_world, eventType);
            if (eventsResource != null)
            {
                var updateMethod = eventType.GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                updateMethod?.Invoke(eventsResource, null);
            }
        }
    }

    /// <summary>
    /// Run the app in a loop until shouldQuit returns true.
    /// </summary>
    public void Run(Func<bool> shouldQuit)
    {
        RunStartup();

        while (!shouldQuit())
        {
            Update();
        }
    }

    private void RebuildSchedules()
    {
        foreach (var stage in _stages)
        {
            stage.BuildSchedule(_systemSets);
        }
        _scheduleDirty = false;
    }

    /// <summary>
    /// Configure a system set (internal).
    /// </summary>
    internal SystemSetConfigurator ConfigureSetInternal(Type setType)
    {
        if (!_systemSets.TryGetValue(setType, out var config))
        {
            config = new SystemSetConfig(setType);
            _systemSets[setType] = config;
        }
        return new SystemSetConfigurator(config, this);
    }

    /// <summary>
    /// Get set configuration (internal).
    /// </summary>
    internal SystemSetConfig? GetSetConfig(Type setType)
    {
        _systemSets.TryGetValue(setType, out var config);
        return config;
    }

    /// <summary>
    /// Add a custom schedule (internal).
    /// </summary>
    internal App AddScheduleInternal(Schedule schedule)
    {
        _schedules[schedule.Name] = schedule;
        _scheduleDirty = true;
        return this;
    }

    /// <summary>
    /// Get a schedule by name (internal).
    /// </summary>
    internal Schedule? GetScheduleInternal(string name)
    {
        _schedules.TryGetValue(name, out var schedule);
        return schedule;
    }

    /// <summary>
    /// Run a custom schedule (internal).
    /// </summary>
    internal void RunScheduleInternal(string name)
    {
        if (_schedules.TryGetValue(name, out var schedule))
        {
            if (_scheduleDirty)
            {
                schedule.BuildSchedules(_systemSets);
            }
            schedule.Run(_world);
        }
    }

    /// <summary>
    /// Add a system to a custom schedule (internal).
    /// </summary>
    internal SystemConfigurator AddSystemToScheduleInternal(string scheduleName, string stageName, ISystem system)
    {
        if (_schedules.TryGetValue(scheduleName, out var schedule))
        {
            var stage = schedule.GetStage(stageName);
            if (stage != null)
            {
                stage.AddSystem(system);
                _scheduleDirty = true;
                return new SystemConfigurator(system, stage);
            }
        }
        throw new InvalidOperationException($"Schedule '{scheduleName}' or stage '{stageName}' not found");
    }

    /// <summary>
    /// Add an observer (internal).
    /// </summary>
    internal App AddObserverInternal(IObserver observer)
    {
        _observers.Register(observer);
        return this;
    }

    /// <summary>
    /// Get the observer registry (internal).
    /// </summary>
    internal ObserverRegistry GetObserverRegistry()
    {
        return _observers;
    }
}

/// <summary>
/// A stage contains systems that run in a specific order.
/// </summary>
public sealed class Stage
{
    private readonly List<SystemNode> _systems = new();
    private readonly Dictionary<ISystem, SystemNode> _systemNodes = new();
    private List<List<ISystem>>? _executionBatches;
    private Dictionary<Type, SystemSetConfig>? _systemSets;

    public Stage(string name)
    {
        Name = name;
    }

    public string Name { get; }

    internal void AddSystem(ISystem system)
    {
        var node = new SystemNode(system);
        _systems.Add(node);
        _systemNodes[system] = node;
        _executionBatches = null; // Mark schedule as dirty
    }

    internal SystemNode GetNode(ISystem system)
    {
        if (!_systemNodes.TryGetValue(system, out var node))
        {
            throw new InvalidOperationException("System not found in this stage");
        }
        return node;
    }

    /// <summary>
    /// Get a node by label.
    /// </summary>
    private SystemNode? GetNodeByLabel(string label)
    {
        return _systems.FirstOrDefault(n => n.Label == label);
    }

    /// <summary>
    /// Resolve pending label dependencies.
    /// </summary>
    private void ResolveLabelDependencies()
    {
        foreach (var node in _systems)
        {
            foreach (var (label, isAfter) in node.PendingLabelDependencies)
            {
                var targetNode = GetNodeByLabel(label);
                if (targetNode != null)
                {
                    if (isAfter)
                    {
                        // This system runs after target
                        node.Dependencies.Add(targetNode);
                        targetNode.Dependents.Add(node);
                    }
                    else
                    {
                        // This system runs before target
                        targetNode.Dependencies.Add(node);
                        node.Dependents.Add(targetNode);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Resolve system set dependencies.
    /// Systems in sets inherit the ordering constraints of their sets.
    /// </summary>
    private void ResolveSystemSetDependencies(Dictionary<Type, SystemSetConfig> systemSets)
    {
        foreach (var node in _systems)
        {
            foreach (var setType in node.SystemSets)
            {
                if (systemSets.TryGetValue(setType, out var setConfig))
                {
                    // Apply run condition from set if system doesn't have one
                    if (setConfig.RunCondition != null && node.System is ParameterizedSystem paramSystem)
                    {
                        // Note: Combining run conditions would require tracking them separately
                        // For now, we skip applying set-level run conditions to avoid overriding
                        // This is a known limitation that could be improved
                    }

                    // For each set this system belongs to, apply the set's ordering constraints
                    foreach (var beforeSetType in setConfig.BeforeSets)
                    {
                        // This set (and all systems in it) should run before systems in beforeSetType
                        var systemsInBeforeSet = _systems.Where(n => n.SystemSets.Contains(beforeSetType));
                        foreach (var targetNode in systemsInBeforeSet)
                        {
                            if (targetNode != node && !node.Dependencies.Contains(targetNode))
                            {
                                targetNode.Dependencies.Add(node);
                                node.Dependents.Add(targetNode);
                            }
                        }
                    }

                    foreach (var afterSetType in setConfig.AfterSets)
                    {
                        // This set (and all systems in it) should run after systems in afterSetType
                        var systemsInAfterSet = _systems.Where(n => n.SystemSets.Contains(afterSetType));
                        foreach (var targetNode in systemsInAfterSet)
                        {
                            if (targetNode != node && !targetNode.Dependencies.Contains(node))
                            {
                                node.Dependencies.Add(targetNode);
                                targetNode.Dependents.Add(node);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Build the execution schedule with parallel batching.
    /// </summary>
    internal void BuildSchedule(Dictionary<Type, SystemSetConfig> systemSets)
    {
        _systemSets = systemSets;

        // Resolve label-based dependencies
        ResolveLabelDependencies();

        // Resolve system set dependencies
        ResolveSystemSetDependencies(systemSets);

        // Topological sort with parallel batching
        var batches = new List<List<ISystem>>();
        var remaining = new HashSet<SystemNode>(_systems);
        var inDegree = _systems.ToDictionary(n => n, n => n.Dependencies.Count);

        while (remaining.Count > 0)
        {
            // Find all systems with no remaining dependencies
            var ready = remaining
                .Where(n => inDegree[n] == 0)
                .ToList();

            if (ready.Count == 0)
            {
                throw new InvalidOperationException("Circular dependency detected in system ordering");
            }

            // Batch systems that don't conflict
            var batch = ParallelBatch(ready);
            batches.Add(batch.Select(n => n.System).ToList());

            // Remove executed systems and update in-degrees
            foreach (var node in batch)
            {
                remaining.Remove(node);
                foreach (var dependent in _systems)
                {
                    if (dependent.Dependencies.Contains(node))
                    {
                        inDegree[dependent]--;
                    }
                }
            }
        }

        _executionBatches = batches;
    }

    /// <summary>
    /// Batch systems for parallel execution.
    /// Exclusive systems always run alone in their own batch.
    /// </summary>
    private List<SystemNode> ParallelBatch(List<SystemNode> ready)
    {
        var batch = new List<SystemNode>();

        // Check if any ready systems are exclusive
        var exclusiveSystem = ready.FirstOrDefault(n => n.IsExclusive || n.System is IExclusiveSystem);
        if (exclusiveSystem != null)
        {
            // Exclusive systems always run alone
            return new List<SystemNode> { exclusiveSystem };
        }

        foreach (var node in ready)
        {
            // Check if this system conflicts with any already in the batch
            var conflicts = false;
            var access = node.System.GetAccess();

            if (access != null)
            {
                foreach (var existing in batch)
                {
                    var existingAccess = existing.System.GetAccess();
                    if (existingAccess != null && access.ConflictsWith(existingAccess))
                    {
                        conflicts = true;
                        break;
                    }
                }
            }

            if (!conflicts)
            {
                batch.Add(node);
            }
        }

        // If nothing could be batched, take the first one
        if (batch.Count == 0 && ready.Count > 0)
        {
            batch.Add(ready[0]);
        }

        return batch;
    }

    /// <summary>
    /// Execute all systems in this stage.
    /// </summary>
    internal void Execute(TinyWorld world)
    {
        if (_executionBatches == null)
        {
            // If systemSets is null, build with empty dictionary
            BuildSchedule(_systemSets ?? new Dictionary<Type, SystemSetConfig>());
        }

        foreach (var batch in _executionBatches!)
        {
            // Execute batch in parallel if it has multiple systems
            if (batch.Count > 1)
            {
                System.Threading.Tasks.Parallel.ForEach(batch, system =>
                {
                    if (system.ShouldRun(world))
                    {
                        system.Run(world);
                    }
                });
            }
            else
            {
                // Single system - execute directly
                foreach (var system in batch)
                {
                    if (system.ShouldRun(world))
                    {
                        system.Run(world);
                    }
                }
            }
        }
    }
}

/// <summary>
/// Internal node for system dependency tracking.
/// </summary>
internal sealed class SystemNode
{
    public SystemNode(ISystem system)
    {
        System = system;
    }

    public ISystem System { get; }
    public HashSet<SystemNode> Dependencies { get; } = new();
    public HashSet<SystemNode> Dependents { get; } = new();
    public string? Label { get; set; }
    public HashSet<Type> SystemSets { get; } = new();
    public List<(string Label, bool IsAfter)> PendingLabelDependencies { get; } = new();
    public bool IsExclusive { get; set; }
}

/// <summary>
/// Fluent API for configuring systems.
/// </summary>
public sealed class SystemConfigurator
{
    private readonly ISystem _system;
    private readonly Stage _stage;

    internal SystemConfigurator(ISystem system, Stage stage)
    {
        _system = system;
        _stage = stage;
    }

    /// <summary>
    /// Set a label for this system (for ordering).
    /// </summary>
    public SystemConfigurator Label(string label)
    {
        var node = _stage.GetNode(_system);
        node.Label = label;
        return this;
    }

    /// <summary>
    /// This system runs after another system.
    /// </summary>
    public SystemConfigurator After(ISystem other)
    {
        var node = _stage.GetNode(_system);
        var otherNode = _stage.GetNode(other);
        node.Dependencies.Add(otherNode);
        otherNode.Dependents.Add(node);
        return this;
    }

    /// <summary>
    /// This system runs after a labeled system.
    /// </summary>
    public SystemConfigurator After(string label)
    {
        var node = _stage.GetNode(_system);
        node.PendingLabelDependencies.Add((label, IsAfter: true));
        return this;
    }

    /// <summary>
    /// This system runs before another system.
    /// </summary>
    public SystemConfigurator Before(ISystem other)
    {
        var node = _stage.GetNode(_system);
        var otherNode = _stage.GetNode(other);
        otherNode.Dependencies.Add(node);
        node.Dependents.Add(otherNode);
        return this;
    }

    /// <summary>
    /// This system runs before a labeled system.
    /// </summary>
    public SystemConfigurator Before(string label)
    {
        var node = _stage.GetNode(_system);
        node.PendingLabelDependencies.Add((label, IsAfter: false));
        return this;
    }

    /// <summary>
    /// Set a run condition for this system.
    /// </summary>
    public SystemConfigurator RunIf(RunCondition condition)
    {
        if (_system is ParameterizedSystem paramSys)
        {
            paramSys.SetRunCondition(condition);
        }
        return this;
    }

    /// <summary>
    /// Add this system to a set (internal).
    /// </summary>
    internal SystemConfigurator InSetInternal(Type setType)
    {
        var node = _stage.GetNode(_system);
        node.SystemSets.Add(setType);
        return this;
    }

    /// <summary>
    /// Mark this system as exclusive (internal).
    /// </summary>
    internal SystemConfigurator ExclusiveInternal()
    {
        var node = _stage.GetNode(_system);
        node.IsExclusive = true;
        return this;
    }
}
