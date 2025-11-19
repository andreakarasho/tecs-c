#pragma warning disable 1591
#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcsBindings.Bevy
{
    /// <summary>
    /// Query data for 1 component
    /// </summary>
    [SkipLocalsInit]
    public ref struct Data<T0> : IData<Data<T0>>
        where T0 : struct
    {
        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<Entity> _entities;
        private Span<T0> _column0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
            _column0 = default;
        }

        public static void Build(TinyWorld world)
        {
            world.Component<T0>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0> CreateIterator(QueryIterator iterator)
            => new Data<T0>(iterator);

        [UnscopedRef]
        public ref Data<T0> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        /// <summary>
        /// Deconstruct into component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ref<T0> c0)
        {
            c0 = new Ref<T0>(ref _column0[_index]);
        }

        /// <summary>
        /// Deconstruct into component spans (chunk access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void DeconstructSpans(out Span<T0> c0)
        {
            c0 = _column0;
        }

        /// <summary>
        /// Deconstruct into entities and component spans
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out ReadOnlySpan<Entity> entities, out Span<T0> c0)
        {
            entities = _entities;
            c0 = _column0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.MoveNext())
                    return false;

                _index = 0;
                _count = _iterator.Count;
                _entities = _iterator.Entities;
                _column0 = _iterator.Column<T0>();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0> GetEnumerator() => this;

        /// <summary>
        /// Get entity at current index
        /// </summary>
        public readonly Entity Entity => _entities[_index];

        /// <summary>
        /// Get component reference at current index
        /// </summary>
        public readonly ref T0 Item0 => ref _column0[_index];
    }

    /// <summary>
    /// Query data for 2 components
    /// </summary>
    [SkipLocalsInit]
    public ref struct Data<T0, T1> : IData<Data<T0, T1>>
        where T0 : struct where T1 : struct
    {
        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<Entity> _entities;
        private Span<T0> _column0;
        private Span<T1> _column1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
            _column0 = default;
            _column1 = default;
        }

        public static void Build(TinyWorld world)
        {
            world.Component<T0>();
            world.Component<T1>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1>(iterator);

        [UnscopedRef]
        public ref Data<T0, T1> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        /// <summary>
        /// Deconstruct into component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ref<T0> c0, out Ref<T1> c1)
        {
            c0 = new Ref<T0>(ref _column0[_index]);
            c1 = new Ref<T1>(ref _column1[_index]);
        }

        /// <summary>
        /// Deconstruct into component spans (chunk access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void DeconstructSpans(out Span<T0> c0, out Span<T1> c1)
        {
            c0 = _column0;
            c1 = _column1;
        }

        /// <summary>
        /// Deconstruct into entities and component spans
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out ReadOnlySpan<Entity> entities, out Span<T0> c0, out Span<T1> c1)
        {
            entities = _entities;
            c0 = _column0;
            c1 = _column1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.MoveNext())
                    return false;

                _index = 0;
                _count = _iterator.Count;
                _entities = _iterator.Entities;
                _column0 = _iterator.Column<T0>();
                _column1 = _iterator.Column<T1>();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1> GetEnumerator() => this;

        /// <summary>
        /// Get entity at current index
        /// </summary>
        public readonly Entity Entity => _entities[_index];

        /// <summary>
        /// Get component reference at current index
        /// </summary>
        public readonly ref T0 Item0 => ref _column0[_index];
        public readonly ref T1 Item1 => ref _column1[_index];
    }

    /// <summary>
    /// Query data for 3 components
    /// </summary>
    [SkipLocalsInit]
    public ref struct Data<T0, T1, T2> : IData<Data<T0, T1, T2>>
        where T0 : struct where T1 : struct where T2 : struct
    {
        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<Entity> _entities;
        private Span<T0> _column0;
        private Span<T1> _column1;
        private Span<T2> _column2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
            _column0 = default;
            _column1 = default;
            _column2 = default;
        }

        public static void Build(TinyWorld world)
        {
            world.Component<T0>();
            world.Component<T1>();
            world.Component<T2>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2>(iterator);

        [UnscopedRef]
        public ref Data<T0, T1, T2> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        /// <summary>
        /// Deconstruct into component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ref<T0> c0, out Ref<T1> c1, out Ref<T2> c2)
        {
            c0 = new Ref<T0>(ref _column0[_index]);
            c1 = new Ref<T1>(ref _column1[_index]);
            c2 = new Ref<T2>(ref _column2[_index]);
        }

        /// <summary>
        /// Deconstruct into component spans (chunk access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void DeconstructSpans(out Span<T0> c0, out Span<T1> c1, out Span<T2> c2)
        {
            c0 = _column0;
            c1 = _column1;
            c2 = _column2;
        }

        /// <summary>
        /// Deconstruct into entities and component spans
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out ReadOnlySpan<Entity> entities, out Span<T0> c0, out Span<T1> c1, out Span<T2> c2)
        {
            entities = _entities;
            c0 = _column0;
            c1 = _column1;
            c2 = _column2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.MoveNext())
                    return false;

                _index = 0;
                _count = _iterator.Count;
                _entities = _iterator.Entities;
                _column0 = _iterator.Column<T0>();
                _column1 = _iterator.Column<T1>();
                _column2 = _iterator.Column<T2>();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2> GetEnumerator() => this;

        /// <summary>
        /// Get entity at current index
        /// </summary>
        public readonly Entity Entity => _entities[_index];

        /// <summary>
        /// Get component reference at current index
        /// </summary>
        public readonly ref T0 Item0 => ref _column0[_index];
        public readonly ref T1 Item1 => ref _column1[_index];
        public readonly ref T2 Item2 => ref _column2[_index];
    }

    /// <summary>
    /// Query data for 4 components
    /// </summary>
    [SkipLocalsInit]
    public ref struct Data<T0, T1, T2, T3> : IData<Data<T0, T1, T2, T3>>
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct
    {
        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<Entity> _entities;
        private Span<T0> _column0;
        private Span<T1> _column1;
        private Span<T2> _column2;
        private Span<T3> _column3;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
            _column0 = default;
            _column1 = default;
            _column2 = default;
            _column3 = default;
        }

        public static void Build(TinyWorld world)
        {
            world.Component<T0>();
            world.Component<T1>();
            world.Component<T2>();
            world.Component<T3>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3>(iterator);

        [UnscopedRef]
        public ref Data<T0, T1, T2, T3> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        /// <summary>
        /// Deconstruct into component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ref<T0> c0, out Ref<T1> c1, out Ref<T2> c2, out Ref<T3> c3)
        {
            c0 = new Ref<T0>(ref _column0[_index]);
            c1 = new Ref<T1>(ref _column1[_index]);
            c2 = new Ref<T2>(ref _column2[_index]);
            c3 = new Ref<T3>(ref _column3[_index]);
        }

        /// <summary>
        /// Deconstruct into component spans (chunk access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void DeconstructSpans(out Span<T0> c0, out Span<T1> c1, out Span<T2> c2, out Span<T3> c3)
        {
            c0 = _column0;
            c1 = _column1;
            c2 = _column2;
            c3 = _column3;
        }

        /// <summary>
        /// Deconstruct into entities and component spans
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out ReadOnlySpan<Entity> entities, out Span<T0> c0, out Span<T1> c1, out Span<T2> c2, out Span<T3> c3)
        {
            entities = _entities;
            c0 = _column0;
            c1 = _column1;
            c2 = _column2;
            c3 = _column3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.MoveNext())
                    return false;

                _index = 0;
                _count = _iterator.Count;
                _entities = _iterator.Entities;
                _column0 = _iterator.Column<T0>();
                _column1 = _iterator.Column<T1>();
                _column2 = _iterator.Column<T2>();
                _column3 = _iterator.Column<T3>();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3> GetEnumerator() => this;

        /// <summary>
        /// Get entity at current index
        /// </summary>
        public readonly Entity Entity => _entities[_index];

        /// <summary>
        /// Get component reference at current index
        /// </summary>
        public readonly ref T0 Item0 => ref _column0[_index];
        public readonly ref T1 Item1 => ref _column1[_index];
        public readonly ref T2 Item2 => ref _column2[_index];
        public readonly ref T3 Item3 => ref _column3[_index];
    }

    /// <summary>
    /// Query data for 5 components
    /// </summary>
    [SkipLocalsInit]
    public ref struct Data<T0, T1, T2, T3, T4> : IData<Data<T0, T1, T2, T3, T4>>
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<Entity> _entities;
        private Span<T0> _column0;
        private Span<T1> _column1;
        private Span<T2> _column2;
        private Span<T3> _column3;
        private Span<T4> _column4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
            _column0 = default;
            _column1 = default;
            _column2 = default;
            _column3 = default;
            _column4 = default;
        }

        public static void Build(TinyWorld world)
        {
            world.Component<T0>();
            world.Component<T1>();
            world.Component<T2>();
            world.Component<T3>();
            world.Component<T4>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4>(iterator);

        [UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        /// <summary>
        /// Deconstruct into component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ref<T0> c0, out Ref<T1> c1, out Ref<T2> c2, out Ref<T3> c3, out Ref<T4> c4)
        {
            c0 = new Ref<T0>(ref _column0[_index]);
            c1 = new Ref<T1>(ref _column1[_index]);
            c2 = new Ref<T2>(ref _column2[_index]);
            c3 = new Ref<T3>(ref _column3[_index]);
            c4 = new Ref<T4>(ref _column4[_index]);
        }

        /// <summary>
        /// Deconstruct into component spans (chunk access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void DeconstructSpans(out Span<T0> c0, out Span<T1> c1, out Span<T2> c2, out Span<T3> c3, out Span<T4> c4)
        {
            c0 = _column0;
            c1 = _column1;
            c2 = _column2;
            c3 = _column3;
            c4 = _column4;
        }

        /// <summary>
        /// Deconstruct into entities and component spans
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out ReadOnlySpan<Entity> entities, out Span<T0> c0, out Span<T1> c1, out Span<T2> c2, out Span<T3> c3, out Span<T4> c4)
        {
            entities = _entities;
            c0 = _column0;
            c1 = _column1;
            c2 = _column2;
            c3 = _column3;
            c4 = _column4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.MoveNext())
                    return false;

                _index = 0;
                _count = _iterator.Count;
                _entities = _iterator.Entities;
                _column0 = _iterator.Column<T0>();
                _column1 = _iterator.Column<T1>();
                _column2 = _iterator.Column<T2>();
                _column3 = _iterator.Column<T3>();
                _column4 = _iterator.Column<T4>();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4> GetEnumerator() => this;

        /// <summary>
        /// Get entity at current index
        /// </summary>
        public readonly Entity Entity => _entities[_index];

        /// <summary>
        /// Get component reference at current index
        /// </summary>
        public readonly ref T0 Item0 => ref _column0[_index];
        public readonly ref T1 Item1 => ref _column1[_index];
        public readonly ref T2 Item2 => ref _column2[_index];
        public readonly ref T3 Item3 => ref _column3[_index];
        public readonly ref T4 Item4 => ref _column4[_index];
    }

    /// <summary>
    /// Query data for 6 components
    /// </summary>
    [SkipLocalsInit]
    public ref struct Data<T0, T1, T2, T3, T4, T5> : IData<Data<T0, T1, T2, T3, T4, T5>>
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<Entity> _entities;
        private Span<T0> _column0;
        private Span<T1> _column1;
        private Span<T2> _column2;
        private Span<T3> _column3;
        private Span<T4> _column4;
        private Span<T5> _column5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
            _column0 = default;
            _column1 = default;
            _column2 = default;
            _column3 = default;
            _column4 = default;
            _column5 = default;
        }

        public static void Build(TinyWorld world)
        {
            world.Component<T0>();
            world.Component<T1>();
            world.Component<T2>();
            world.Component<T3>();
            world.Component<T4>();
            world.Component<T5>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5>(iterator);

        [UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        /// <summary>
        /// Deconstruct into component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ref<T0> c0, out Ref<T1> c1, out Ref<T2> c2, out Ref<T3> c3, out Ref<T4> c4, out Ref<T5> c5)
        {
            c0 = new Ref<T0>(ref _column0[_index]);
            c1 = new Ref<T1>(ref _column1[_index]);
            c2 = new Ref<T2>(ref _column2[_index]);
            c3 = new Ref<T3>(ref _column3[_index]);
            c4 = new Ref<T4>(ref _column4[_index]);
            c5 = new Ref<T5>(ref _column5[_index]);
        }

        /// <summary>
        /// Deconstruct into component spans (chunk access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void DeconstructSpans(out Span<T0> c0, out Span<T1> c1, out Span<T2> c2, out Span<T3> c3, out Span<T4> c4, out Span<T5> c5)
        {
            c0 = _column0;
            c1 = _column1;
            c2 = _column2;
            c3 = _column3;
            c4 = _column4;
            c5 = _column5;
        }

        /// <summary>
        /// Deconstruct into entities and component spans
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out ReadOnlySpan<Entity> entities, out Span<T0> c0, out Span<T1> c1, out Span<T2> c2, out Span<T3> c3, out Span<T4> c4, out Span<T5> c5)
        {
            entities = _entities;
            c0 = _column0;
            c1 = _column1;
            c2 = _column2;
            c3 = _column3;
            c4 = _column4;
            c5 = _column5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.MoveNext())
                    return false;

                _index = 0;
                _count = _iterator.Count;
                _entities = _iterator.Entities;
                _column0 = _iterator.Column<T0>();
                _column1 = _iterator.Column<T1>();
                _column2 = _iterator.Column<T2>();
                _column3 = _iterator.Column<T3>();
                _column4 = _iterator.Column<T4>();
                _column5 = _iterator.Column<T5>();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5> GetEnumerator() => this;

        /// <summary>
        /// Get entity at current index
        /// </summary>
        public readonly Entity Entity => _entities[_index];

        /// <summary>
        /// Get component reference at current index
        /// </summary>
        public readonly ref T0 Item0 => ref _column0[_index];
        public readonly ref T1 Item1 => ref _column1[_index];
        public readonly ref T2 Item2 => ref _column2[_index];
        public readonly ref T3 Item3 => ref _column3[_index];
        public readonly ref T4 Item4 => ref _column4[_index];
        public readonly ref T5 Item5 => ref _column5[_index];
    }

    /// <summary>
    /// Query data for 7 components
    /// </summary>
    [SkipLocalsInit]
    public ref struct Data<T0, T1, T2, T3, T4, T5, T6> : IData<Data<T0, T1, T2, T3, T4, T5, T6>>
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
    {
        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<Entity> _entities;
        private Span<T0> _column0;
        private Span<T1> _column1;
        private Span<T2> _column2;
        private Span<T3> _column3;
        private Span<T4> _column4;
        private Span<T5> _column5;
        private Span<T6> _column6;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
            _column0 = default;
            _column1 = default;
            _column2 = default;
            _column3 = default;
            _column4 = default;
            _column5 = default;
            _column6 = default;
        }

        public static void Build(TinyWorld world)
        {
            world.Component<T0>();
            world.Component<T1>();
            world.Component<T2>();
            world.Component<T3>();
            world.Component<T4>();
            world.Component<T5>();
            world.Component<T6>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5, T6> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5, T6>(iterator);

        [UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5, T6> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        /// <summary>
        /// Deconstruct into component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ref<T0> c0, out Ref<T1> c1, out Ref<T2> c2, out Ref<T3> c3, out Ref<T4> c4, out Ref<T5> c5, out Ref<T6> c6)
        {
            c0 = new Ref<T0>(ref _column0[_index]);
            c1 = new Ref<T1>(ref _column1[_index]);
            c2 = new Ref<T2>(ref _column2[_index]);
            c3 = new Ref<T3>(ref _column3[_index]);
            c4 = new Ref<T4>(ref _column4[_index]);
            c5 = new Ref<T5>(ref _column5[_index]);
            c6 = new Ref<T6>(ref _column6[_index]);
        }

        /// <summary>
        /// Deconstruct into component spans (chunk access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void DeconstructSpans(out Span<T0> c0, out Span<T1> c1, out Span<T2> c2, out Span<T3> c3, out Span<T4> c4, out Span<T5> c5, out Span<T6> c6)
        {
            c0 = _column0;
            c1 = _column1;
            c2 = _column2;
            c3 = _column3;
            c4 = _column4;
            c5 = _column5;
            c6 = _column6;
        }

        /// <summary>
        /// Deconstruct into entities and component spans
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out ReadOnlySpan<Entity> entities, out Span<T0> c0, out Span<T1> c1, out Span<T2> c2, out Span<T3> c3, out Span<T4> c4, out Span<T5> c5, out Span<T6> c6)
        {
            entities = _entities;
            c0 = _column0;
            c1 = _column1;
            c2 = _column2;
            c3 = _column3;
            c4 = _column4;
            c5 = _column5;
            c6 = _column6;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.MoveNext())
                    return false;

                _index = 0;
                _count = _iterator.Count;
                _entities = _iterator.Entities;
                _column0 = _iterator.Column<T0>();
                _column1 = _iterator.Column<T1>();
                _column2 = _iterator.Column<T2>();
                _column3 = _iterator.Column<T3>();
                _column4 = _iterator.Column<T4>();
                _column5 = _iterator.Column<T5>();
                _column6 = _iterator.Column<T6>();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6> GetEnumerator() => this;

        /// <summary>
        /// Get entity at current index
        /// </summary>
        public readonly Entity Entity => _entities[_index];

        /// <summary>
        /// Get component reference at current index
        /// </summary>
        public readonly ref T0 Item0 => ref _column0[_index];
        public readonly ref T1 Item1 => ref _column1[_index];
        public readonly ref T2 Item2 => ref _column2[_index];
        public readonly ref T3 Item3 => ref _column3[_index];
        public readonly ref T4 Item4 => ref _column4[_index];
        public readonly ref T5 Item5 => ref _column5[_index];
        public readonly ref T6 Item6 => ref _column6[_index];
    }

    /// <summary>
    /// Query data for 8 components
    /// </summary>
    [SkipLocalsInit]
    public ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7>>
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct
    {
        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<Entity> _entities;
        private Span<T0> _column0;
        private Span<T1> _column1;
        private Span<T2> _column2;
        private Span<T3> _column3;
        private Span<T4> _column4;
        private Span<T5> _column5;
        private Span<T6> _column6;
        private Span<T7> _column7;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
            _column0 = default;
            _column1 = default;
            _column2 = default;
            _column3 = default;
            _column4 = default;
            _column5 = default;
            _column6 = default;
            _column7 = default;
        }

        public static void Build(TinyWorld world)
        {
            world.Component<T0>();
            world.Component<T1>();
            world.Component<T2>();
            world.Component<T3>();
            world.Component<T4>();
            world.Component<T5>();
            world.Component<T6>();
            world.Component<T7>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5, T6, T7> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5, T6, T7>(iterator);

        [UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5, T6, T7> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        /// <summary>
        /// Deconstruct into component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ref<T0> c0, out Ref<T1> c1, out Ref<T2> c2, out Ref<T3> c3, out Ref<T4> c4, out Ref<T5> c5, out Ref<T6> c6, out Ref<T7> c7)
        {
            c0 = new Ref<T0>(ref _column0[_index]);
            c1 = new Ref<T1>(ref _column1[_index]);
            c2 = new Ref<T2>(ref _column2[_index]);
            c3 = new Ref<T3>(ref _column3[_index]);
            c4 = new Ref<T4>(ref _column4[_index]);
            c5 = new Ref<T5>(ref _column5[_index]);
            c6 = new Ref<T6>(ref _column6[_index]);
            c7 = new Ref<T7>(ref _column7[_index]);
        }

        /// <summary>
        /// Deconstruct into component spans (chunk access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void DeconstructSpans(out Span<T0> c0, out Span<T1> c1, out Span<T2> c2, out Span<T3> c3, out Span<T4> c4, out Span<T5> c5, out Span<T6> c6, out Span<T7> c7)
        {
            c0 = _column0;
            c1 = _column1;
            c2 = _column2;
            c3 = _column3;
            c4 = _column4;
            c5 = _column5;
            c6 = _column6;
            c7 = _column7;
        }

        /// <summary>
        /// Deconstruct into entities and component spans
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out ReadOnlySpan<Entity> entities, out Span<T0> c0, out Span<T1> c1, out Span<T2> c2, out Span<T3> c3, out Span<T4> c4, out Span<T5> c5, out Span<T6> c6, out Span<T7> c7)
        {
            entities = _entities;
            c0 = _column0;
            c1 = _column1;
            c2 = _column2;
            c3 = _column3;
            c4 = _column4;
            c5 = _column5;
            c6 = _column6;
            c7 = _column7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.MoveNext())
                    return false;

                _index = 0;
                _count = _iterator.Count;
                _entities = _iterator.Entities;
                _column0 = _iterator.Column<T0>();
                _column1 = _iterator.Column<T1>();
                _column2 = _iterator.Column<T2>();
                _column3 = _iterator.Column<T3>();
                _column4 = _iterator.Column<T4>();
                _column5 = _iterator.Column<T5>();
                _column6 = _iterator.Column<T6>();
                _column7 = _iterator.Column<T7>();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6, T7> GetEnumerator() => this;

        /// <summary>
        /// Get entity at current index
        /// </summary>
        public readonly Entity Entity => _entities[_index];

        /// <summary>
        /// Get component reference at current index
        /// </summary>
        public readonly ref T0 Item0 => ref _column0[_index];
        public readonly ref T1 Item1 => ref _column1[_index];
        public readonly ref T2 Item2 => ref _column2[_index];
        public readonly ref T3 Item3 => ref _column3[_index];
        public readonly ref T4 Item4 => ref _column4[_index];
        public readonly ref T5 Item5 => ref _column5[_index];
        public readonly ref T6 Item6 => ref _column6[_index];
        public readonly ref T7 Item7 => ref _column7[_index];
    }

}
