namespace TinyEcsBindings.Bevy;

/// <summary>
/// Interface for modular plugin system.
/// Plugins configure the app by adding resources, systems, and stages.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Configure the app. Called once during plugin registration.
    /// </summary>
    void Build(App app);
}

/// <summary>
/// Extension methods for plugin registration.
/// </summary>
public static class PluginExtensions
{
    /// <summary>
    /// Add a plugin to the app.
    /// </summary>
    public static App AddPlugin(this App app, IPlugin plugin)
    {
        plugin.Build(app);
        return app;
    }

    /// <summary>
    /// Add a plugin by type (will create instance with parameterless constructor).
    /// </summary>
    public static App AddPlugin<T>(this App app) where T : IPlugin, new()
    {
        var plugin = new T();
        plugin.Build(app);
        return app;
    }
}

/// <summary>
/// Example plugin for common Bevy functionality.
/// </summary>
public sealed class DefaultPlugins : IPlugin
{
    public void Build(App app)
    {
        // Add default stages if not already present
        if (!app.HasStage("PreUpdate"))
        {
            app.AddStage("PreUpdate", after: "Startup", before: "Update");
        }

        if (!app.HasStage("PostUpdate"))
        {
            app.AddStage("PostUpdate", after: "Update");
        }

        // Could add default systems here
    }
}
