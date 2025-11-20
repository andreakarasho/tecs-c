using System;
using System.Collections.Generic;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Tracks which resources a system parameter reads and writes.
/// Used for parallel system scheduling to detect resource conflicts.
/// </summary>
public sealed class SystemParamAccess
{
    /// <summary>
    /// Resources that this parameter reads from (shared access allowed).
    /// </summary>
    public HashSet<Type> ReadResources { get; } = new();

    /// <summary>
    /// Resources that this parameter writes to (exclusive access required).
    /// </summary>
    public HashSet<Type> WriteResources { get; } = new();

    /// <summary>
    /// Checks if this access pattern conflicts with another.
    /// Two systems conflict if one writes to a resource that the other reads or writes.
    /// </summary>
    public bool ConflictsWith(SystemParamAccess other)
    {
        // Write conflicts with any access (read or write) to the same resource
        foreach (var write in WriteResources)
        {
            if (other.ReadResources.Contains(write) || other.WriteResources.Contains(write))
            {
                return true;
            }
        }

        // Read conflicts with writes to the same resource
        foreach (var write in other.WriteResources)
        {
            if (ReadResources.Contains(write))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Merges another access pattern into this one.
    /// </summary>
    public void Merge(SystemParamAccess other)
    {
        foreach (var read in other.ReadResources)
            ReadResources.Add(read);

        foreach (var write in other.WriteResources)
            WriteResources.Add(write);
    }
}
