using System;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Marker interface for system output types.
/// </summary>
public interface ISystemOutput { }

/// <summary>
/// System parameter that receives piped input from a previous system.
/// </summary>
public sealed class In<T> : ISystemParam where T : ISystemOutput
{
    private T? _value;

    public void Initialize(TinyWorld world)
    {
        // Input is set by the piping infrastructure
    }

    public void Fetch(TinyWorld world)
    {
        // Value is already set by the previous system
    }

    public SystemParamAccess GetAccess()
    {
        // Input parameters don't access world state
        return new SystemParamAccess();
    }

    /// <summary>
    /// Set the input value (called by piping infrastructure).
    /// </summary>
    internal void SetValue(T value)
    {
        _value = value;
    }

    /// <summary>
    /// Get the piped input value.
    /// </summary>
    public T Value => _value!;

    /// <summary>
    /// Implicit conversion to the underlying value.
    /// </summary>
    public static implicit operator T(In<T> input) => input.Value;
}

/// <summary>
/// Wraps a system that produces output.
/// </summary>
internal interface IPipedSystem : ISystem
{
    Type OutputType { get; }
    object? GetOutput();
}

/// <summary>
/// Wraps a system function that returns output.
/// </summary>
internal sealed class PipedSystem<TOutput> : IPipedSystem where TOutput : ISystemOutput
{
    private readonly Func<TOutput> _systemFunc;
    private TOutput? _lastOutput;

    public PipedSystem(Func<TOutput> systemFunc)
    {
        _systemFunc = systemFunc;
    }

    public Type OutputType => typeof(TOutput);

    public void Run(TinyWorld world)
    {
        _lastOutput = _systemFunc();
    }

    public object? GetOutput() => _lastOutput;

    public SystemParamAccess? GetAccess()
    {
        // Conservative: assume this system might access anything
        return null;
    }

    public bool ShouldRun(TinyWorld world) => true;
}

/// <summary>
/// Wraps a parameterized system that produces output.
/// </summary>
internal sealed class ParameterizedPipedSystem<TOutput> : IPipedSystem where TOutput : ISystemOutput
{
    private readonly ISystemParam[] _parameters;
    private readonly Func<object> _systemFunc;
    private TOutput? _lastOutput;

    public ParameterizedPipedSystem(ISystemParam[] parameters, Func<object> systemFunc)
    {
        _parameters = parameters;
        _systemFunc = systemFunc;
    }

    public Type OutputType => typeof(TOutput);

    public void Run(TinyWorld world)
    {
        // Initialize and fetch parameters
        foreach (var param in _parameters)
        {
            param.Initialize(world);
            param.Fetch(world);
        }

        _lastOutput = (TOutput)_systemFunc();
    }

    public object? GetOutput() => _lastOutput;

    public SystemParamAccess? GetAccess()
    {
        // Aggregate access from all parameters
        var access = new SystemParamAccess();
        foreach (var param in _parameters)
        {
            var paramAccess = param.GetAccess();
            access.ReadResources.UnionWith(paramAccess.ReadResources);
            access.WriteResources.UnionWith(paramAccess.WriteResources);
        }
        return access;
    }

    public bool ShouldRun(TinyWorld world) => true;
}

/// <summary>
/// System chain that pipes output from one system to the next.
/// </summary>
internal sealed class PipedSystemChain<TOutput> : ISystem where TOutput : ISystemOutput
{
    private readonly IPipedSystem _source;
    private readonly ISystem _target;
    private readonly In<TOutput> _input;

    public PipedSystemChain(IPipedSystem source, ISystem target, In<TOutput> input)
    {
        _source = source;
        _target = target;
        _input = input;
    }

    public void Run(TinyWorld world)
    {
        // Run source system
        _source.Run(world);

        // Pipe output to target input
        var output = _source.GetOutput();
        if (output != null)
        {
            _input.SetValue((TOutput)output);
        }

        // Run target system
        _target.Run(world);
    }

    public SystemParamAccess? GetAccess()
    {
        // Combine access from both systems
        var sourceAccess = _source.GetAccess();
        var targetAccess = _target.GetAccess();

        if (sourceAccess == null || targetAccess == null)
        {
            return null; // Conservative: exclusive access
        }

        var combined = new SystemParamAccess();
        combined.ReadResources.UnionWith(sourceAccess.ReadResources);
        combined.ReadResources.UnionWith(targetAccess.ReadResources);
        combined.WriteResources.UnionWith(sourceAccess.WriteResources);
        combined.WriteResources.UnionWith(targetAccess.WriteResources);

        return combined;
    }

    public bool ShouldRun(TinyWorld world) =>
        _source.ShouldRun(world) && _target.ShouldRun(world);
}

/// <summary>
/// Extension methods for system piping.
/// </summary>
public static class SystemPipingExtensions
{
    /// <summary>
    /// Pipe the output of this system to another system.
    /// Example: app.AddSystem(() => new MyOutput()).Pipe((In<MyOutput> input) => { ... });
    /// </summary>
    public static SystemConfigurator Pipe<TOutput>(
        this SystemConfigurator configurator,
        Action<In<TOutput>> targetSystem)
        where TOutput : ISystemOutput
    {
        // This would require storing the source system and creating a chain
        // For now, throw to indicate this needs special handling
        throw new NotImplementedException(
            "System piping requires using AddPipedSystem. " +
            "Example: app.AddPipedSystem(() => new MyOutput(), (In<MyOutput> input) => { ... })");
    }

    /// <summary>
    /// Add a piped system chain to the app.
    /// The source system returns output which is piped to the target system's In parameter.
    /// </summary>
    public static App AddPipedSystem<TOutput>(
        this App app,
        Func<TOutput> sourceSystem,
        Action<In<TOutput>> targetSystem)
        where TOutput : ISystemOutput
    {
        var pipedSource = new PipedSystem<TOutput>(sourceSystem);
        var input = new In<TOutput>();
        var targetSystemAdapted = SystemAdapters.Create(targetSystem);

        var chain = new PipedSystemChain<TOutput>(pipedSource, targetSystemAdapted, input);

        app.AddSystem(chain);
        return app;
    }

    /// <summary>
    /// Add a piped system chain to a specific stage.
    /// </summary>
    public static SystemConfigurator AddPipedSystemToStage<TOutput>(
        this App app,
        string stageName,
        Func<TOutput> sourceSystem,
        Action<In<TOutput>> targetSystem)
        where TOutput : ISystemOutput
    {
        var pipedSource = new PipedSystem<TOutput>(sourceSystem);
        var input = new In<TOutput>();
        var targetSystemAdapted = SystemAdapters.Create(targetSystem);

        var chain = new PipedSystemChain<TOutput>(pipedSource, targetSystemAdapted, input);

        return app.AddSystemToStage(stageName, chain);
    }
}

/// <summary>
/// Common output types for system piping.
/// </summary>
public readonly struct Continue : ISystemOutput
{
    public static readonly Continue Value = new();
}

public readonly struct Success : ISystemOutput
{
    public readonly string Message;
    public Success(string message = "") => Message = message;
}

public readonly struct Failure : ISystemOutput
{
    public readonly string Error;
    public Failure(string error) => Error = error;
}

public readonly struct Count : ISystemOutput
{
    public readonly int Value;
    public Count(int value) => Value = value;
}

/// <summary>
/// Generic result type for system outputs.
/// </summary>
public readonly struct Result<T> : ISystemOutput where T : struct
{
    public readonly T Value;
    public readonly bool IsSuccess;
    public readonly string? ErrorMessage;

    private Result(T value, bool isSuccess, string? errorMessage = null)
    {
        Value = value;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Ok(T value) => new(value, true);
    public static Result<T> Err(string error) => new(default, false, error);
}
