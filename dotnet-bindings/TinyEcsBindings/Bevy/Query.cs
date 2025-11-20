using System;

namespace TinyEcsBindings.Bevy;

/// <summary>
/// System parameter for querying entities with specific components.
/// Combines data (components to fetch) with filters (constraints on entities).
/// </summary>
public class Query<TData, TFilter> : ISystemParam
    where TData : struct, IData<TData>, allows ref struct
    where TFilter : struct, IFilter<TFilter>, allows ref struct
{
    private TinyWorld? _world;
    private bool _built;
    private ulong _lastRun;
    private ulong _thisRun;

    public void Initialize(TinyWorld world)
    {
        _world = world;
        _built = false;
        _lastRun = 0;
        _thisRun = 0;
    }

    public void Fetch(TinyWorld world)
    {
        _world = world;

        // Build query on first fetch
        if (!_built)
        {
            BuildQuery();
            _built = true;
        }

        // Update tick tracking for change detection
        _lastRun = _thisRun;
        _thisRun = world.Tick;
    }

    public SystemParamAccess GetAccess()
    {
        // For now, we conservatively mark all component types as read/write
        // A more sophisticated implementation would track this per-type
        var access = new SystemParamAccess();

        // Query has read access to all queried components
        // This is conservative - actual access depends on TData
        access.ReadResources.Add(typeof(Query<TData, TFilter>));

        return access;
    }

    private void BuildQuery()
    {
        // Nothing to do - query is built on each Iter() call
    }

    /// <summary>
    /// Get an iterator over all matching entities.
    /// </summary>
    public QueryIter Iter()
    {
        if (!_built)
        {
            BuildQuery();
            _built = true;
        }

        // Build the query directly
        var queryBuilder = _world!.Query();
        TData.Build(queryBuilder);
        TFilter.Build(queryBuilder);
        var iterator = queryBuilder.Iter();

        var dataIter = TData.CreateIterator(iterator);
        var filterIter = TFilter.CreateIterator(iterator);

        // Set ticks for change detection filters
        filterIter.SetTicks(_lastRun, _thisRun);

        return new QueryIter(dataIter, filterIter);
    }

    /// <summary>
    /// Get enumerator for foreach support.
    /// Allows: foreach (var (pos, vel) in query) { ... }
    /// </summary>
    public QueryIter GetEnumerator() => Iter();

    /// <summary>
    /// Iterator that combines data and filter iteration.
    /// </summary>
    public ref struct QueryIter
    {
        private TData _data;
        private TFilter _filter;

        internal QueryIter(TData data, TFilter filter)
        {
            _data = data;
            _filter = filter;
        }

        public bool MoveNext()
        {
            // Both data and filter must advance together
            bool dataNext = _data.MoveNext();
            bool filterNext = _filter.MoveNext();
            return dataNext && filterNext;
        }

        public readonly TData Current => _data;

        public QueryIter GetEnumerator() => this;
    }

    /// <summary>
    /// Get the number of entities matching the query.
    /// </summary>
    public int Count()
    {
        int count = 0;
        foreach (var _ in Iter())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Check if the query matches any entities.
    /// </summary>
    public bool IsEmpty()
    {
        var iter = Iter();
        return !iter.MoveNext();
    }

    /// <summary>
    /// Get the first matching entity's data, or throw if none exist.
    /// </summary>
    public TData Single()
    {
        var iter = Iter();
        if (!iter.MoveNext())
        {
            throw new InvalidOperationException("Query matched no entities");
        }
        var result = iter.Current;
        if (iter.MoveNext())
        {
            throw new InvalidOperationException("Query matched multiple entities");
        }
        return result;
    }

    /// <summary>
    /// Try to get the first matching entity's data.
    /// Returns false if no entities match, throws if multiple entities match.
    /// </summary>
    public bool TrySingle(out TData result)
    {
        var iter = Iter();
        if (!iter.MoveNext())
        {
            result = default;
            return false;
        }
        result = iter.Current;
        if (iter.MoveNext())
        {
            throw new InvalidOperationException("Query matched multiple entities");
        }
        return true;
    }
}

/// <summary>
/// Query with only data (no filter).
/// Convenience wrapper for Query<TData, Empty>.
/// </summary>
public sealed class Query<TData> : ISystemParam
    where TData : struct, IData<TData>, allows ref struct
{
    private TinyWorld? _world;
    private bool _built;
    private ulong _lastRun;
    private ulong _thisRun;

    public void Initialize(TinyWorld world)
    {
        _world = world;
        _built = false;
        _lastRun = 0;
        _thisRun = 0;
    }

    public void Fetch(TinyWorld world)
    {
        _world = world;

        if (!_built)
        {
            BuildQuery();
            _built = true;
        }

        _lastRun = _thisRun;
        _thisRun = world.Tick;
    }

    public SystemParamAccess GetAccess()
    {
        var access = new SystemParamAccess();
        access.ReadResources.Add(typeof(Query<TData>));
        return access;
    }

    private void BuildQuery()
    {
        // Nothing to do - query is built on each Iter() call
    }

    public QueryIter Iter()
    {
        if (!_built)
        {
            BuildQuery();
            _built = true;
        }

        // Build the query directly
        var queryBuilder = _world!.Query();
        TData.Build(queryBuilder);
        var iterator = queryBuilder.Iter();
        var dataIter = TData.CreateIterator(iterator);

        return new QueryIter(dataIter);
    }

    /// <summary>
    /// Get enumerator for foreach support.
    /// Allows: foreach (var (pos, vel) in query) { ... }
    /// </summary>
    public QueryIter GetEnumerator() => Iter();

    public ref struct QueryIter
    {
        private TData _data;

        internal QueryIter(TData data)
        {
            _data = data;
        }

        public bool MoveNext() => _data.MoveNext();
        public readonly TData Current => _data;
        public QueryIter GetEnumerator() => this;
    }

    public int Count()
    {
        int count = 0;
        foreach (var _ in Iter())
        {
            count++;
        }
        return count;
    }

    public bool IsEmpty()
    {
        var iter = Iter();
        return !iter.MoveNext();
    }

    /// <summary>
    /// Get the first matching entity's data, or throw if none exist.
    /// </summary>
    public TData Single()
    {
        var iter = Iter();
        if (!iter.MoveNext())
        {
            throw new InvalidOperationException("Query matched no entities");
        }
        var result = iter.Current;
        if (iter.MoveNext())
        {
            throw new InvalidOperationException("Query matched multiple entities");
        }
        return result;
    }

    /// <summary>
    /// Try to get the first matching entity's data.
    /// Returns false if no entities match, throws if multiple entities match.
    /// </summary>
    public bool TrySingle(out TData result)
    {
        var iter = Iter();
        if (!iter.MoveNext())
        {
            result = default;
            return false;
        }
        result = iter.Current;
        if (iter.MoveNext())
        {
            throw new InvalidOperationException("Query matched multiple entities");
        }
        return true;
    }
}
