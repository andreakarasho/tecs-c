using System;
using System.Collections.Generic;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// Event storage for a specific event type.
/// </summary>
public class Events<T> where T : struct
{
    private List<T> _events = new();
    private int _startEventCount = 0;

    /// <summary>
    /// Send an event.
    /// </summary>
    public void Send(T evt)
    {
        _events.Add(evt);
    }

    /// <summary>
    /// Get all events since the given cursor position.
    /// </summary>
    internal ReadOnlySpan<T> GetEventsSince(int cursor)
    {
        if (cursor >= _events.Count)
            return ReadOnlySpan<T>.Empty;

        return System.Runtime.InteropServices.CollectionsMarshal
            .AsSpan(_events)
            .Slice(cursor);
    }

    /// <summary>
    /// Get the current event count (used for cursor tracking).
    /// </summary>
    internal int EventCount => _events.Count;

    /// <summary>
    /// Clear old events (called at the end of each frame).
    /// </summary>
    internal void Update()
    {
        // Clear events from previous frame
        if (_startEventCount > 0)
        {
            _events.RemoveRange(0, _startEventCount);
        }
        _startEventCount = _events.Count;
    }

    /// <summary>
    /// Clear all events immediately.
    /// </summary>
    public void Clear()
    {
        _events.Clear();
        _startEventCount = 0;
    }
}

/// <summary>
/// System parameter for reading events.
/// </summary>
public sealed class EventReader<T> : ISystemParam where T : struct
{
    private Events<T>? _events;
    private int _lastEventCount;

    public void Initialize(TinyWorld world)
    {
        _lastEventCount = 0;

        // Ensure Events<T> resource exists
        if (!world.HasResource<Events<T>>())
        {
            world.SetResource(new Events<T>());
        }
    }

    public void Fetch(TinyWorld world)
    {
        _events = world.GetResource<Events<T>>();
    }

    public SystemParamAccess GetAccess()
    {
        var access = new SystemParamAccess();
        access.ReadResources.Add(typeof(Events<T>));
        return access;
    }

    /// <summary>
    /// Get an iterator over all events since the last read.
    /// </summary>
    public EventIterator Iter()
    {
        return new EventIterator(_events!, _lastEventCount, out _lastEventCount);
    }

    /// <summary>
    /// Iterator over events.
    /// </summary>
    public ref struct EventIterator
    {
        private ReadOnlySpan<T> _events;
        private int _index;

        internal EventIterator(Events<T> events, int lastEventCount, out int newEventCount)
        {
            _events = events.GetEventsSince(lastEventCount);
            newEventCount = events.EventCount;
            _index = -1;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _events.Length;
        }

        public readonly ref readonly T Current => ref _events[_index];

        public EventIterator GetEnumerator() => this;
    }

    /// <summary>
    /// Check if there are any unread events.
    /// </summary>
    public bool IsEmpty()
    {
        if (_events == null) return true;
        return _lastEventCount >= _events.EventCount;
    }

    /// <summary>
    /// Get the number of unread events.
    /// </summary>
    public int Length()
    {
        if (_events == null) return 0;
        return Math.Max(0, _events.EventCount - _lastEventCount);
    }

    /// <summary>
    /// Clear the reader's cursor (will re-read all events on next Iter).
    /// </summary>
    public void Clear()
    {
        _lastEventCount = 0;
    }
}

/// <summary>
/// System parameter for writing events.
/// </summary>
public sealed class EventWriter<T> : ISystemParam where T : struct
{
    private Events<T>? _events;

    public void Initialize(TinyWorld world)
    {
        // Ensure Events<T> resource exists
        if (!world.HasResource<Events<T>>())
        {
            world.SetResource(new Events<T>());
        }
    }

    public void Fetch(TinyWorld world)
    {
        _events = world.GetResource<Events<T>>();
    }

    public SystemParamAccess GetAccess()
    {
        var access = new SystemParamAccess();
        access.WriteResources.Add(typeof(Events<T>));
        return access;
    }

    /// <summary>
    /// Send an event.
    /// </summary>
    public void Send(T evt)
    {
        _events?.Send(evt);
    }

    /// <summary>
    /// Send multiple events.
    /// </summary>
    public void SendBatch(ReadOnlySpan<T> events)
    {
        if (_events == null) return;

        foreach (var evt in events)
        {
            _events.Send(evt);
        }
    }
}
