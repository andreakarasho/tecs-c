#pragma warning disable 1591
#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcsBindings.Bevy
{
    /// <summary>
    /// Combine 2 filters with AND logic
    /// </summary>
    [SkipLocalsInit]
    public ref struct And<TFilter0, TFilter1> : IFilter<And<TFilter0, TFilter1>>
        where TFilter0 : struct, IFilter<TFilter0>, allows ref struct
        where TFilter1 : struct, IFilter<TFilter1>, allows ref struct
    {
        private TFilter0 _filter0;
        private TFilter1 _filter1;

        internal And(QueryIterator iterator)
        {
            _filter0 = TFilter0.CreateIterator(iterator);
            _filter1 = TFilter1.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            TFilter0.Build(builder);
            TFilter1.Build(builder);
        }

        public static And<TFilter0, TFilter1> CreateIterator(QueryIterator iterator)
            => new And<TFilter0, TFilter1>(iterator);

        [UnscopedRef]
        public ref And<TFilter0, TFilter1> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_filter0.MoveNext()) return false;
            if (!_filter1.MoveNext()) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly And<TFilter0, TFilter1> GetEnumerator() => this;

        public void SetTicks(ulong lastRun, ulong thisRun)
        {
            _filter0.SetTicks(lastRun, thisRun);
            _filter1.SetTicks(lastRun, thisRun);
        }
    }

    /// <summary>
    /// Combine 3 filters with AND logic
    /// </summary>
    [SkipLocalsInit]
    public ref struct And<TFilter0, TFilter1, TFilter2> : IFilter<And<TFilter0, TFilter1, TFilter2>>
        where TFilter0 : struct, IFilter<TFilter0>, allows ref struct
        where TFilter1 : struct, IFilter<TFilter1>, allows ref struct
        where TFilter2 : struct, IFilter<TFilter2>, allows ref struct
    {
        private TFilter0 _filter0;
        private TFilter1 _filter1;
        private TFilter2 _filter2;

        internal And(QueryIterator iterator)
        {
            _filter0 = TFilter0.CreateIterator(iterator);
            _filter1 = TFilter1.CreateIterator(iterator);
            _filter2 = TFilter2.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            TFilter0.Build(builder);
            TFilter1.Build(builder);
            TFilter2.Build(builder);
        }

        public static And<TFilter0, TFilter1, TFilter2> CreateIterator(QueryIterator iterator)
            => new And<TFilter0, TFilter1, TFilter2>(iterator);

        [UnscopedRef]
        public ref And<TFilter0, TFilter1, TFilter2> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_filter0.MoveNext()) return false;
            if (!_filter1.MoveNext()) return false;
            if (!_filter2.MoveNext()) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly And<TFilter0, TFilter1, TFilter2> GetEnumerator() => this;

        public void SetTicks(ulong lastRun, ulong thisRun)
        {
            _filter0.SetTicks(lastRun, thisRun);
            _filter1.SetTicks(lastRun, thisRun);
            _filter2.SetTicks(lastRun, thisRun);
        }
    }

    /// <summary>
    /// Combine 4 filters with AND logic
    /// </summary>
    [SkipLocalsInit]
    public ref struct And<TFilter0, TFilter1, TFilter2, TFilter3> : IFilter<And<TFilter0, TFilter1, TFilter2, TFilter3>>
        where TFilter0 : struct, IFilter<TFilter0>, allows ref struct
        where TFilter1 : struct, IFilter<TFilter1>, allows ref struct
        where TFilter2 : struct, IFilter<TFilter2>, allows ref struct
        where TFilter3 : struct, IFilter<TFilter3>, allows ref struct
    {
        private TFilter0 _filter0;
        private TFilter1 _filter1;
        private TFilter2 _filter2;
        private TFilter3 _filter3;

        internal And(QueryIterator iterator)
        {
            _filter0 = TFilter0.CreateIterator(iterator);
            _filter1 = TFilter1.CreateIterator(iterator);
            _filter2 = TFilter2.CreateIterator(iterator);
            _filter3 = TFilter3.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            TFilter0.Build(builder);
            TFilter1.Build(builder);
            TFilter2.Build(builder);
            TFilter3.Build(builder);
        }

        public static And<TFilter0, TFilter1, TFilter2, TFilter3> CreateIterator(QueryIterator iterator)
            => new And<TFilter0, TFilter1, TFilter2, TFilter3>(iterator);

        [UnscopedRef]
        public ref And<TFilter0, TFilter1, TFilter2, TFilter3> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_filter0.MoveNext()) return false;
            if (!_filter1.MoveNext()) return false;
            if (!_filter2.MoveNext()) return false;
            if (!_filter3.MoveNext()) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly And<TFilter0, TFilter1, TFilter2, TFilter3> GetEnumerator() => this;

        public void SetTicks(ulong lastRun, ulong thisRun)
        {
            _filter0.SetTicks(lastRun, thisRun);
            _filter1.SetTicks(lastRun, thisRun);
            _filter2.SetTicks(lastRun, thisRun);
            _filter3.SetTicks(lastRun, thisRun);
        }
    }

    /// <summary>
    /// Combine 5 filters with AND logic
    /// </summary>
    [SkipLocalsInit]
    public ref struct And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4> : IFilter<And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4>>
        where TFilter0 : struct, IFilter<TFilter0>, allows ref struct
        where TFilter1 : struct, IFilter<TFilter1>, allows ref struct
        where TFilter2 : struct, IFilter<TFilter2>, allows ref struct
        where TFilter3 : struct, IFilter<TFilter3>, allows ref struct
        where TFilter4 : struct, IFilter<TFilter4>, allows ref struct
    {
        private TFilter0 _filter0;
        private TFilter1 _filter1;
        private TFilter2 _filter2;
        private TFilter3 _filter3;
        private TFilter4 _filter4;

        internal And(QueryIterator iterator)
        {
            _filter0 = TFilter0.CreateIterator(iterator);
            _filter1 = TFilter1.CreateIterator(iterator);
            _filter2 = TFilter2.CreateIterator(iterator);
            _filter3 = TFilter3.CreateIterator(iterator);
            _filter4 = TFilter4.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            TFilter0.Build(builder);
            TFilter1.Build(builder);
            TFilter2.Build(builder);
            TFilter3.Build(builder);
            TFilter4.Build(builder);
        }

        public static And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4> CreateIterator(QueryIterator iterator)
            => new And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4>(iterator);

        [UnscopedRef]
        public ref And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_filter0.MoveNext()) return false;
            if (!_filter1.MoveNext()) return false;
            if (!_filter2.MoveNext()) return false;
            if (!_filter3.MoveNext()) return false;
            if (!_filter4.MoveNext()) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4> GetEnumerator() => this;

        public void SetTicks(ulong lastRun, ulong thisRun)
        {
            _filter0.SetTicks(lastRun, thisRun);
            _filter1.SetTicks(lastRun, thisRun);
            _filter2.SetTicks(lastRun, thisRun);
            _filter3.SetTicks(lastRun, thisRun);
            _filter4.SetTicks(lastRun, thisRun);
        }
    }

    /// <summary>
    /// Combine 6 filters with AND logic
    /// </summary>
    [SkipLocalsInit]
    public ref struct And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5> : IFilter<And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5>>
        where TFilter0 : struct, IFilter<TFilter0>, allows ref struct
        where TFilter1 : struct, IFilter<TFilter1>, allows ref struct
        where TFilter2 : struct, IFilter<TFilter2>, allows ref struct
        where TFilter3 : struct, IFilter<TFilter3>, allows ref struct
        where TFilter4 : struct, IFilter<TFilter4>, allows ref struct
        where TFilter5 : struct, IFilter<TFilter5>, allows ref struct
    {
        private TFilter0 _filter0;
        private TFilter1 _filter1;
        private TFilter2 _filter2;
        private TFilter3 _filter3;
        private TFilter4 _filter4;
        private TFilter5 _filter5;

        internal And(QueryIterator iterator)
        {
            _filter0 = TFilter0.CreateIterator(iterator);
            _filter1 = TFilter1.CreateIterator(iterator);
            _filter2 = TFilter2.CreateIterator(iterator);
            _filter3 = TFilter3.CreateIterator(iterator);
            _filter4 = TFilter4.CreateIterator(iterator);
            _filter5 = TFilter5.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            TFilter0.Build(builder);
            TFilter1.Build(builder);
            TFilter2.Build(builder);
            TFilter3.Build(builder);
            TFilter4.Build(builder);
            TFilter5.Build(builder);
        }

        public static And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5> CreateIterator(QueryIterator iterator)
            => new And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5>(iterator);

        [UnscopedRef]
        public ref And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_filter0.MoveNext()) return false;
            if (!_filter1.MoveNext()) return false;
            if (!_filter2.MoveNext()) return false;
            if (!_filter3.MoveNext()) return false;
            if (!_filter4.MoveNext()) return false;
            if (!_filter5.MoveNext()) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5> GetEnumerator() => this;

        public void SetTicks(ulong lastRun, ulong thisRun)
        {
            _filter0.SetTicks(lastRun, thisRun);
            _filter1.SetTicks(lastRun, thisRun);
            _filter2.SetTicks(lastRun, thisRun);
            _filter3.SetTicks(lastRun, thisRun);
            _filter4.SetTicks(lastRun, thisRun);
            _filter5.SetTicks(lastRun, thisRun);
        }
    }

    /// <summary>
    /// Combine 7 filters with AND logic
    /// </summary>
    [SkipLocalsInit]
    public ref struct And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5, TFilter6> : IFilter<And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5, TFilter6>>
        where TFilter0 : struct, IFilter<TFilter0>, allows ref struct
        where TFilter1 : struct, IFilter<TFilter1>, allows ref struct
        where TFilter2 : struct, IFilter<TFilter2>, allows ref struct
        where TFilter3 : struct, IFilter<TFilter3>, allows ref struct
        where TFilter4 : struct, IFilter<TFilter4>, allows ref struct
        where TFilter5 : struct, IFilter<TFilter5>, allows ref struct
        where TFilter6 : struct, IFilter<TFilter6>, allows ref struct
    {
        private TFilter0 _filter0;
        private TFilter1 _filter1;
        private TFilter2 _filter2;
        private TFilter3 _filter3;
        private TFilter4 _filter4;
        private TFilter5 _filter5;
        private TFilter6 _filter6;

        internal And(QueryIterator iterator)
        {
            _filter0 = TFilter0.CreateIterator(iterator);
            _filter1 = TFilter1.CreateIterator(iterator);
            _filter2 = TFilter2.CreateIterator(iterator);
            _filter3 = TFilter3.CreateIterator(iterator);
            _filter4 = TFilter4.CreateIterator(iterator);
            _filter5 = TFilter5.CreateIterator(iterator);
            _filter6 = TFilter6.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            TFilter0.Build(builder);
            TFilter1.Build(builder);
            TFilter2.Build(builder);
            TFilter3.Build(builder);
            TFilter4.Build(builder);
            TFilter5.Build(builder);
            TFilter6.Build(builder);
        }

        public static And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5, TFilter6> CreateIterator(QueryIterator iterator)
            => new And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5, TFilter6>(iterator);

        [UnscopedRef]
        public ref And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5, TFilter6> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_filter0.MoveNext()) return false;
            if (!_filter1.MoveNext()) return false;
            if (!_filter2.MoveNext()) return false;
            if (!_filter3.MoveNext()) return false;
            if (!_filter4.MoveNext()) return false;
            if (!_filter5.MoveNext()) return false;
            if (!_filter6.MoveNext()) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5, TFilter6> GetEnumerator() => this;

        public void SetTicks(ulong lastRun, ulong thisRun)
        {
            _filter0.SetTicks(lastRun, thisRun);
            _filter1.SetTicks(lastRun, thisRun);
            _filter2.SetTicks(lastRun, thisRun);
            _filter3.SetTicks(lastRun, thisRun);
            _filter4.SetTicks(lastRun, thisRun);
            _filter5.SetTicks(lastRun, thisRun);
            _filter6.SetTicks(lastRun, thisRun);
        }
    }

    /// <summary>
    /// Combine 8 filters with AND logic
    /// </summary>
    [SkipLocalsInit]
    public ref struct And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5, TFilter6, TFilter7> : IFilter<And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5, TFilter6, TFilter7>>
        where TFilter0 : struct, IFilter<TFilter0>, allows ref struct
        where TFilter1 : struct, IFilter<TFilter1>, allows ref struct
        where TFilter2 : struct, IFilter<TFilter2>, allows ref struct
        where TFilter3 : struct, IFilter<TFilter3>, allows ref struct
        where TFilter4 : struct, IFilter<TFilter4>, allows ref struct
        where TFilter5 : struct, IFilter<TFilter5>, allows ref struct
        where TFilter6 : struct, IFilter<TFilter6>, allows ref struct
        where TFilter7 : struct, IFilter<TFilter7>, allows ref struct
    {
        private TFilter0 _filter0;
        private TFilter1 _filter1;
        private TFilter2 _filter2;
        private TFilter3 _filter3;
        private TFilter4 _filter4;
        private TFilter5 _filter5;
        private TFilter6 _filter6;
        private TFilter7 _filter7;

        internal And(QueryIterator iterator)
        {
            _filter0 = TFilter0.CreateIterator(iterator);
            _filter1 = TFilter1.CreateIterator(iterator);
            _filter2 = TFilter2.CreateIterator(iterator);
            _filter3 = TFilter3.CreateIterator(iterator);
            _filter4 = TFilter4.CreateIterator(iterator);
            _filter5 = TFilter5.CreateIterator(iterator);
            _filter6 = TFilter6.CreateIterator(iterator);
            _filter7 = TFilter7.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            TFilter0.Build(builder);
            TFilter1.Build(builder);
            TFilter2.Build(builder);
            TFilter3.Build(builder);
            TFilter4.Build(builder);
            TFilter5.Build(builder);
            TFilter6.Build(builder);
            TFilter7.Build(builder);
        }

        public static And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5, TFilter6, TFilter7> CreateIterator(QueryIterator iterator)
            => new And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5, TFilter6, TFilter7>(iterator);

        [UnscopedRef]
        public ref And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5, TFilter6, TFilter7> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_filter0.MoveNext()) return false;
            if (!_filter1.MoveNext()) return false;
            if (!_filter2.MoveNext()) return false;
            if (!_filter3.MoveNext()) return false;
            if (!_filter4.MoveNext()) return false;
            if (!_filter5.MoveNext()) return false;
            if (!_filter6.MoveNext()) return false;
            if (!_filter7.MoveNext()) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly And<TFilter0, TFilter1, TFilter2, TFilter3, TFilter4, TFilter5, TFilter6, TFilter7> GetEnumerator() => this;

        public void SetTicks(ulong lastRun, ulong thisRun)
        {
            _filter0.SetTicks(lastRun, thisRun);
            _filter1.SetTicks(lastRun, thisRun);
            _filter2.SetTicks(lastRun, thisRun);
            _filter3.SetTicks(lastRun, thisRun);
            _filter4.SetTicks(lastRun, thisRun);
            _filter5.SetTicks(lastRun, thisRun);
            _filter6.SetTicks(lastRun, thisRun);
            _filter7.SetTicks(lastRun, thisRun);
        }
    }

}
