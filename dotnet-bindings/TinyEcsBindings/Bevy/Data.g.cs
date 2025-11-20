#pragma warning disable 1591
#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcsBindings.Bevy
{
    /// <summary>
    /// Helper struct to track component reference and stride
    /// </summary>
    internal ref struct ComponentRefInfo<T> where T : struct
    {
        internal Ptr<T> Ref;
        internal int Stride; // 0 if optional and not present, 1 if present

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentRefInfo(ref T value, int stride)
        {
            Ref = new Ptr<T>(ref value);
            Stride = stride;
        }
    }

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
        private ComponentRefInfo<T0> _refInfo0;
        private RefRO<Entity> _entityRef;
        private int _entityStride;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
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
        public readonly void Deconstruct(out Ptr<T0> c0)
        {
            c0 = _refInfo0.Ref;
        }

        /// <summary>
        /// Deconstruct into entity ref and component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out RefRO<Entity> entity, out Ptr<T0> c0)
        {
            entity = _entityRef;
            c0 = _refInfo0.Ref;
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
                var span0 = _iterator.Column<T0>();
                _refInfo0 = span0.IsEmpty
                    ? new ComponentRefInfo<T0>(ref Unsafe.NullRef<T0>(), 0)
                    : new ComponentRefInfo<T0>(ref span0[0], 1);
                _entityStride = _entities.IsEmpty ? 0 : 1;
                _entityRef = _entities.IsEmpty
                    ? new RefRO<Entity>(ref Unsafe.NullRef<Entity>())
                    : new RefRO<Entity>(ref Unsafe.AsRef(in _entities[0]));
            }
            else
            {
                _refInfo0.Ref._value = ref Unsafe.Add(ref _refInfo0.Ref._value, _refInfo0.Stride);
                _entityRef._value = ref Unsafe.Add(ref _entityRef._value, _entityStride);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0> GetEnumerator() => this;
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
        private ComponentRefInfo<T0> _refInfo0;
        private ComponentRefInfo<T1> _refInfo1;
        private RefRO<Entity> _entityRef;
        private int _entityStride;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
            builder.With<T1>();
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
        public readonly void Deconstruct(out Ptr<T0> c0, out Ptr<T1> c1)
        {
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
        }

        /// <summary>
        /// Deconstruct into entity ref and component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out RefRO<Entity> entity, out Ptr<T0> c0, out Ptr<T1> c1)
        {
            entity = _entityRef;
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
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
                var span0 = _iterator.Column<T0>();
                _refInfo0 = span0.IsEmpty
                    ? new ComponentRefInfo<T0>(ref Unsafe.NullRef<T0>(), 0)
                    : new ComponentRefInfo<T0>(ref span0[0], 1);
                var span1 = _iterator.Column<T1>();
                _refInfo1 = span1.IsEmpty
                    ? new ComponentRefInfo<T1>(ref Unsafe.NullRef<T1>(), 0)
                    : new ComponentRefInfo<T1>(ref span1[0], 1);
                _entityStride = _entities.IsEmpty ? 0 : 1;
                _entityRef = _entities.IsEmpty
                    ? new RefRO<Entity>(ref Unsafe.NullRef<Entity>())
                    : new RefRO<Entity>(ref Unsafe.AsRef(in _entities[0]));
            }
            else
            {
                _refInfo0.Ref._value = ref Unsafe.Add(ref _refInfo0.Ref._value, _refInfo0.Stride);
                _refInfo1.Ref._value = ref Unsafe.Add(ref _refInfo1.Ref._value, _refInfo1.Stride);
                _entityRef._value = ref Unsafe.Add(ref _entityRef._value, _entityStride);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1> GetEnumerator() => this;
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
        private ComponentRefInfo<T0> _refInfo0;
        private ComponentRefInfo<T1> _refInfo1;
        private ComponentRefInfo<T2> _refInfo2;
        private RefRO<Entity> _entityRef;
        private int _entityStride;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
            builder.With<T1>();
            builder.With<T2>();
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
        public readonly void Deconstruct(out Ptr<T0> c0, out Ptr<T1> c1, out Ptr<T2> c2)
        {
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
            c2 = _refInfo2.Ref;
        }

        /// <summary>
        /// Deconstruct into entity ref and component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out RefRO<Entity> entity, out Ptr<T0> c0, out Ptr<T1> c1, out Ptr<T2> c2)
        {
            entity = _entityRef;
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
            c2 = _refInfo2.Ref;
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
                var span0 = _iterator.Column<T0>();
                _refInfo0 = span0.IsEmpty
                    ? new ComponentRefInfo<T0>(ref Unsafe.NullRef<T0>(), 0)
                    : new ComponentRefInfo<T0>(ref span0[0], 1);
                var span1 = _iterator.Column<T1>();
                _refInfo1 = span1.IsEmpty
                    ? new ComponentRefInfo<T1>(ref Unsafe.NullRef<T1>(), 0)
                    : new ComponentRefInfo<T1>(ref span1[0], 1);
                var span2 = _iterator.Column<T2>();
                _refInfo2 = span2.IsEmpty
                    ? new ComponentRefInfo<T2>(ref Unsafe.NullRef<T2>(), 0)
                    : new ComponentRefInfo<T2>(ref span2[0], 1);
                _entityStride = _entities.IsEmpty ? 0 : 1;
                _entityRef = _entities.IsEmpty
                    ? new RefRO<Entity>(ref Unsafe.NullRef<Entity>())
                    : new RefRO<Entity>(ref Unsafe.AsRef(in _entities[0]));
            }
            else
            {
                _refInfo0.Ref._value = ref Unsafe.Add(ref _refInfo0.Ref._value, _refInfo0.Stride);
                _refInfo1.Ref._value = ref Unsafe.Add(ref _refInfo1.Ref._value, _refInfo1.Stride);
                _refInfo2.Ref._value = ref Unsafe.Add(ref _refInfo2.Ref._value, _refInfo2.Stride);
                _entityRef._value = ref Unsafe.Add(ref _entityRef._value, _entityStride);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2> GetEnumerator() => this;
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
        private ComponentRefInfo<T0> _refInfo0;
        private ComponentRefInfo<T1> _refInfo1;
        private ComponentRefInfo<T2> _refInfo2;
        private ComponentRefInfo<T3> _refInfo3;
        private RefRO<Entity> _entityRef;
        private int _entityStride;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
            builder.With<T1>();
            builder.With<T2>();
            builder.With<T3>();
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
        public readonly void Deconstruct(out Ptr<T0> c0, out Ptr<T1> c1, out Ptr<T2> c2, out Ptr<T3> c3)
        {
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
            c2 = _refInfo2.Ref;
            c3 = _refInfo3.Ref;
        }

        /// <summary>
        /// Deconstruct into entity ref and component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out RefRO<Entity> entity, out Ptr<T0> c0, out Ptr<T1> c1, out Ptr<T2> c2, out Ptr<T3> c3)
        {
            entity = _entityRef;
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
            c2 = _refInfo2.Ref;
            c3 = _refInfo3.Ref;
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
                var span0 = _iterator.Column<T0>();
                _refInfo0 = span0.IsEmpty
                    ? new ComponentRefInfo<T0>(ref Unsafe.NullRef<T0>(), 0)
                    : new ComponentRefInfo<T0>(ref span0[0], 1);
                var span1 = _iterator.Column<T1>();
                _refInfo1 = span1.IsEmpty
                    ? new ComponentRefInfo<T1>(ref Unsafe.NullRef<T1>(), 0)
                    : new ComponentRefInfo<T1>(ref span1[0], 1);
                var span2 = _iterator.Column<T2>();
                _refInfo2 = span2.IsEmpty
                    ? new ComponentRefInfo<T2>(ref Unsafe.NullRef<T2>(), 0)
                    : new ComponentRefInfo<T2>(ref span2[0], 1);
                var span3 = _iterator.Column<T3>();
                _refInfo3 = span3.IsEmpty
                    ? new ComponentRefInfo<T3>(ref Unsafe.NullRef<T3>(), 0)
                    : new ComponentRefInfo<T3>(ref span3[0], 1);
                _entityStride = _entities.IsEmpty ? 0 : 1;
                _entityRef = _entities.IsEmpty
                    ? new RefRO<Entity>(ref Unsafe.NullRef<Entity>())
                    : new RefRO<Entity>(ref Unsafe.AsRef(in _entities[0]));
            }
            else
            {
                _refInfo0.Ref._value = ref Unsafe.Add(ref _refInfo0.Ref._value, _refInfo0.Stride);
                _refInfo1.Ref._value = ref Unsafe.Add(ref _refInfo1.Ref._value, _refInfo1.Stride);
                _refInfo2.Ref._value = ref Unsafe.Add(ref _refInfo2.Ref._value, _refInfo2.Stride);
                _refInfo3.Ref._value = ref Unsafe.Add(ref _refInfo3.Ref._value, _refInfo3.Stride);
                _entityRef._value = ref Unsafe.Add(ref _entityRef._value, _entityStride);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3> GetEnumerator() => this;
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
        private ComponentRefInfo<T0> _refInfo0;
        private ComponentRefInfo<T1> _refInfo1;
        private ComponentRefInfo<T2> _refInfo2;
        private ComponentRefInfo<T3> _refInfo3;
        private ComponentRefInfo<T4> _refInfo4;
        private RefRO<Entity> _entityRef;
        private int _entityStride;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
            builder.With<T1>();
            builder.With<T2>();
            builder.With<T3>();
            builder.With<T4>();
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
        public readonly void Deconstruct(out Ptr<T0> c0, out Ptr<T1> c1, out Ptr<T2> c2, out Ptr<T3> c3, out Ptr<T4> c4)
        {
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
            c2 = _refInfo2.Ref;
            c3 = _refInfo3.Ref;
            c4 = _refInfo4.Ref;
        }

        /// <summary>
        /// Deconstruct into entity ref and component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out RefRO<Entity> entity, out Ptr<T0> c0, out Ptr<T1> c1, out Ptr<T2> c2, out Ptr<T3> c3, out Ptr<T4> c4)
        {
            entity = _entityRef;
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
            c2 = _refInfo2.Ref;
            c3 = _refInfo3.Ref;
            c4 = _refInfo4.Ref;
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
                var span0 = _iterator.Column<T0>();
                _refInfo0 = span0.IsEmpty
                    ? new ComponentRefInfo<T0>(ref Unsafe.NullRef<T0>(), 0)
                    : new ComponentRefInfo<T0>(ref span0[0], 1);
                var span1 = _iterator.Column<T1>();
                _refInfo1 = span1.IsEmpty
                    ? new ComponentRefInfo<T1>(ref Unsafe.NullRef<T1>(), 0)
                    : new ComponentRefInfo<T1>(ref span1[0], 1);
                var span2 = _iterator.Column<T2>();
                _refInfo2 = span2.IsEmpty
                    ? new ComponentRefInfo<T2>(ref Unsafe.NullRef<T2>(), 0)
                    : new ComponentRefInfo<T2>(ref span2[0], 1);
                var span3 = _iterator.Column<T3>();
                _refInfo3 = span3.IsEmpty
                    ? new ComponentRefInfo<T3>(ref Unsafe.NullRef<T3>(), 0)
                    : new ComponentRefInfo<T3>(ref span3[0], 1);
                var span4 = _iterator.Column<T4>();
                _refInfo4 = span4.IsEmpty
                    ? new ComponentRefInfo<T4>(ref Unsafe.NullRef<T4>(), 0)
                    : new ComponentRefInfo<T4>(ref span4[0], 1);
                _entityStride = _entities.IsEmpty ? 0 : 1;
                _entityRef = _entities.IsEmpty
                    ? new RefRO<Entity>(ref Unsafe.NullRef<Entity>())
                    : new RefRO<Entity>(ref Unsafe.AsRef(in _entities[0]));
            }
            else
            {
                _refInfo0.Ref._value = ref Unsafe.Add(ref _refInfo0.Ref._value, _refInfo0.Stride);
                _refInfo1.Ref._value = ref Unsafe.Add(ref _refInfo1.Ref._value, _refInfo1.Stride);
                _refInfo2.Ref._value = ref Unsafe.Add(ref _refInfo2.Ref._value, _refInfo2.Stride);
                _refInfo3.Ref._value = ref Unsafe.Add(ref _refInfo3.Ref._value, _refInfo3.Stride);
                _refInfo4.Ref._value = ref Unsafe.Add(ref _refInfo4.Ref._value, _refInfo4.Stride);
                _entityRef._value = ref Unsafe.Add(ref _entityRef._value, _entityStride);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4> GetEnumerator() => this;
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
        private ComponentRefInfo<T0> _refInfo0;
        private ComponentRefInfo<T1> _refInfo1;
        private ComponentRefInfo<T2> _refInfo2;
        private ComponentRefInfo<T3> _refInfo3;
        private ComponentRefInfo<T4> _refInfo4;
        private ComponentRefInfo<T5> _refInfo5;
        private RefRO<Entity> _entityRef;
        private int _entityStride;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
            builder.With<T1>();
            builder.With<T2>();
            builder.With<T3>();
            builder.With<T4>();
            builder.With<T5>();
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
        public readonly void Deconstruct(out Ptr<T0> c0, out Ptr<T1> c1, out Ptr<T2> c2, out Ptr<T3> c3, out Ptr<T4> c4, out Ptr<T5> c5)
        {
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
            c2 = _refInfo2.Ref;
            c3 = _refInfo3.Ref;
            c4 = _refInfo4.Ref;
            c5 = _refInfo5.Ref;
        }

        /// <summary>
        /// Deconstruct into entity ref and component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out RefRO<Entity> entity, out Ptr<T0> c0, out Ptr<T1> c1, out Ptr<T2> c2, out Ptr<T3> c3, out Ptr<T4> c4, out Ptr<T5> c5)
        {
            entity = _entityRef;
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
            c2 = _refInfo2.Ref;
            c3 = _refInfo3.Ref;
            c4 = _refInfo4.Ref;
            c5 = _refInfo5.Ref;
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
                var span0 = _iterator.Column<T0>();
                _refInfo0 = span0.IsEmpty
                    ? new ComponentRefInfo<T0>(ref Unsafe.NullRef<T0>(), 0)
                    : new ComponentRefInfo<T0>(ref span0[0], 1);
                var span1 = _iterator.Column<T1>();
                _refInfo1 = span1.IsEmpty
                    ? new ComponentRefInfo<T1>(ref Unsafe.NullRef<T1>(), 0)
                    : new ComponentRefInfo<T1>(ref span1[0], 1);
                var span2 = _iterator.Column<T2>();
                _refInfo2 = span2.IsEmpty
                    ? new ComponentRefInfo<T2>(ref Unsafe.NullRef<T2>(), 0)
                    : new ComponentRefInfo<T2>(ref span2[0], 1);
                var span3 = _iterator.Column<T3>();
                _refInfo3 = span3.IsEmpty
                    ? new ComponentRefInfo<T3>(ref Unsafe.NullRef<T3>(), 0)
                    : new ComponentRefInfo<T3>(ref span3[0], 1);
                var span4 = _iterator.Column<T4>();
                _refInfo4 = span4.IsEmpty
                    ? new ComponentRefInfo<T4>(ref Unsafe.NullRef<T4>(), 0)
                    : new ComponentRefInfo<T4>(ref span4[0], 1);
                var span5 = _iterator.Column<T5>();
                _refInfo5 = span5.IsEmpty
                    ? new ComponentRefInfo<T5>(ref Unsafe.NullRef<T5>(), 0)
                    : new ComponentRefInfo<T5>(ref span5[0], 1);
                _entityStride = _entities.IsEmpty ? 0 : 1;
                _entityRef = _entities.IsEmpty
                    ? new RefRO<Entity>(ref Unsafe.NullRef<Entity>())
                    : new RefRO<Entity>(ref Unsafe.AsRef(in _entities[0]));
            }
            else
            {
                _refInfo0.Ref._value = ref Unsafe.Add(ref _refInfo0.Ref._value, _refInfo0.Stride);
                _refInfo1.Ref._value = ref Unsafe.Add(ref _refInfo1.Ref._value, _refInfo1.Stride);
                _refInfo2.Ref._value = ref Unsafe.Add(ref _refInfo2.Ref._value, _refInfo2.Stride);
                _refInfo3.Ref._value = ref Unsafe.Add(ref _refInfo3.Ref._value, _refInfo3.Stride);
                _refInfo4.Ref._value = ref Unsafe.Add(ref _refInfo4.Ref._value, _refInfo4.Stride);
                _refInfo5.Ref._value = ref Unsafe.Add(ref _refInfo5.Ref._value, _refInfo5.Stride);
                _entityRef._value = ref Unsafe.Add(ref _entityRef._value, _entityStride);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5> GetEnumerator() => this;
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
        private ComponentRefInfo<T0> _refInfo0;
        private ComponentRefInfo<T1> _refInfo1;
        private ComponentRefInfo<T2> _refInfo2;
        private ComponentRefInfo<T3> _refInfo3;
        private ComponentRefInfo<T4> _refInfo4;
        private ComponentRefInfo<T5> _refInfo5;
        private ComponentRefInfo<T6> _refInfo6;
        private RefRO<Entity> _entityRef;
        private int _entityStride;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
            builder.With<T1>();
            builder.With<T2>();
            builder.With<T3>();
            builder.With<T4>();
            builder.With<T5>();
            builder.With<T6>();
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
        public readonly void Deconstruct(out Ptr<T0> c0, out Ptr<T1> c1, out Ptr<T2> c2, out Ptr<T3> c3, out Ptr<T4> c4, out Ptr<T5> c5, out Ptr<T6> c6)
        {
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
            c2 = _refInfo2.Ref;
            c3 = _refInfo3.Ref;
            c4 = _refInfo4.Ref;
            c5 = _refInfo5.Ref;
            c6 = _refInfo6.Ref;
        }

        /// <summary>
        /// Deconstruct into entity ref and component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out RefRO<Entity> entity, out Ptr<T0> c0, out Ptr<T1> c1, out Ptr<T2> c2, out Ptr<T3> c3, out Ptr<T4> c4, out Ptr<T5> c5, out Ptr<T6> c6)
        {
            entity = _entityRef;
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
            c2 = _refInfo2.Ref;
            c3 = _refInfo3.Ref;
            c4 = _refInfo4.Ref;
            c5 = _refInfo5.Ref;
            c6 = _refInfo6.Ref;
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
                var span0 = _iterator.Column<T0>();
                _refInfo0 = span0.IsEmpty
                    ? new ComponentRefInfo<T0>(ref Unsafe.NullRef<T0>(), 0)
                    : new ComponentRefInfo<T0>(ref span0[0], 1);
                var span1 = _iterator.Column<T1>();
                _refInfo1 = span1.IsEmpty
                    ? new ComponentRefInfo<T1>(ref Unsafe.NullRef<T1>(), 0)
                    : new ComponentRefInfo<T1>(ref span1[0], 1);
                var span2 = _iterator.Column<T2>();
                _refInfo2 = span2.IsEmpty
                    ? new ComponentRefInfo<T2>(ref Unsafe.NullRef<T2>(), 0)
                    : new ComponentRefInfo<T2>(ref span2[0], 1);
                var span3 = _iterator.Column<T3>();
                _refInfo3 = span3.IsEmpty
                    ? new ComponentRefInfo<T3>(ref Unsafe.NullRef<T3>(), 0)
                    : new ComponentRefInfo<T3>(ref span3[0], 1);
                var span4 = _iterator.Column<T4>();
                _refInfo4 = span4.IsEmpty
                    ? new ComponentRefInfo<T4>(ref Unsafe.NullRef<T4>(), 0)
                    : new ComponentRefInfo<T4>(ref span4[0], 1);
                var span5 = _iterator.Column<T5>();
                _refInfo5 = span5.IsEmpty
                    ? new ComponentRefInfo<T5>(ref Unsafe.NullRef<T5>(), 0)
                    : new ComponentRefInfo<T5>(ref span5[0], 1);
                var span6 = _iterator.Column<T6>();
                _refInfo6 = span6.IsEmpty
                    ? new ComponentRefInfo<T6>(ref Unsafe.NullRef<T6>(), 0)
                    : new ComponentRefInfo<T6>(ref span6[0], 1);
                _entityStride = _entities.IsEmpty ? 0 : 1;
                _entityRef = _entities.IsEmpty
                    ? new RefRO<Entity>(ref Unsafe.NullRef<Entity>())
                    : new RefRO<Entity>(ref Unsafe.AsRef(in _entities[0]));
            }
            else
            {
                _refInfo0.Ref._value = ref Unsafe.Add(ref _refInfo0.Ref._value, _refInfo0.Stride);
                _refInfo1.Ref._value = ref Unsafe.Add(ref _refInfo1.Ref._value, _refInfo1.Stride);
                _refInfo2.Ref._value = ref Unsafe.Add(ref _refInfo2.Ref._value, _refInfo2.Stride);
                _refInfo3.Ref._value = ref Unsafe.Add(ref _refInfo3.Ref._value, _refInfo3.Stride);
                _refInfo4.Ref._value = ref Unsafe.Add(ref _refInfo4.Ref._value, _refInfo4.Stride);
                _refInfo5.Ref._value = ref Unsafe.Add(ref _refInfo5.Ref._value, _refInfo5.Stride);
                _refInfo6.Ref._value = ref Unsafe.Add(ref _refInfo6.Ref._value, _refInfo6.Stride);
                _entityRef._value = ref Unsafe.Add(ref _entityRef._value, _entityStride);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6> GetEnumerator() => this;
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
        private ComponentRefInfo<T0> _refInfo0;
        private ComponentRefInfo<T1> _refInfo1;
        private ComponentRefInfo<T2> _refInfo2;
        private ComponentRefInfo<T3> _refInfo3;
        private ComponentRefInfo<T4> _refInfo4;
        private ComponentRefInfo<T5> _refInfo5;
        private ComponentRefInfo<T6> _refInfo6;
        private ComponentRefInfo<T7> _refInfo7;
        private RefRO<Entity> _entityRef;
        private int _entityStride;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator iterator)
        {
            _iterator = iterator;
            _index = -1;
            _count = -1;
            _entities = default;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
            builder.With<T1>();
            builder.With<T2>();
            builder.With<T3>();
            builder.With<T4>();
            builder.With<T5>();
            builder.With<T6>();
            builder.With<T7>();
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
        public readonly void Deconstruct(out Ptr<T0> c0, out Ptr<T1> c1, out Ptr<T2> c2, out Ptr<T3> c3, out Ptr<T4> c4, out Ptr<T5> c5, out Ptr<T6> c6, out Ptr<T7> c7)
        {
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
            c2 = _refInfo2.Ref;
            c3 = _refInfo3.Ref;
            c4 = _refInfo4.Ref;
            c5 = _refInfo5.Ref;
            c6 = _refInfo6.Ref;
            c7 = _refInfo7.Ref;
        }

        /// <summary>
        /// Deconstruct into entity ref and component refs (per-entity access)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out RefRO<Entity> entity, out Ptr<T0> c0, out Ptr<T1> c1, out Ptr<T2> c2, out Ptr<T3> c3, out Ptr<T4> c4, out Ptr<T5> c5, out Ptr<T6> c6, out Ptr<T7> c7)
        {
            entity = _entityRef;
            c0 = _refInfo0.Ref;
            c1 = _refInfo1.Ref;
            c2 = _refInfo2.Ref;
            c3 = _refInfo3.Ref;
            c4 = _refInfo4.Ref;
            c5 = _refInfo5.Ref;
            c6 = _refInfo6.Ref;
            c7 = _refInfo7.Ref;
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
                var span0 = _iterator.Column<T0>();
                _refInfo0 = span0.IsEmpty
                    ? new ComponentRefInfo<T0>(ref Unsafe.NullRef<T0>(), 0)
                    : new ComponentRefInfo<T0>(ref span0[0], 1);
                var span1 = _iterator.Column<T1>();
                _refInfo1 = span1.IsEmpty
                    ? new ComponentRefInfo<T1>(ref Unsafe.NullRef<T1>(), 0)
                    : new ComponentRefInfo<T1>(ref span1[0], 1);
                var span2 = _iterator.Column<T2>();
                _refInfo2 = span2.IsEmpty
                    ? new ComponentRefInfo<T2>(ref Unsafe.NullRef<T2>(), 0)
                    : new ComponentRefInfo<T2>(ref span2[0], 1);
                var span3 = _iterator.Column<T3>();
                _refInfo3 = span3.IsEmpty
                    ? new ComponentRefInfo<T3>(ref Unsafe.NullRef<T3>(), 0)
                    : new ComponentRefInfo<T3>(ref span3[0], 1);
                var span4 = _iterator.Column<T4>();
                _refInfo4 = span4.IsEmpty
                    ? new ComponentRefInfo<T4>(ref Unsafe.NullRef<T4>(), 0)
                    : new ComponentRefInfo<T4>(ref span4[0], 1);
                var span5 = _iterator.Column<T5>();
                _refInfo5 = span5.IsEmpty
                    ? new ComponentRefInfo<T5>(ref Unsafe.NullRef<T5>(), 0)
                    : new ComponentRefInfo<T5>(ref span5[0], 1);
                var span6 = _iterator.Column<T6>();
                _refInfo6 = span6.IsEmpty
                    ? new ComponentRefInfo<T6>(ref Unsafe.NullRef<T6>(), 0)
                    : new ComponentRefInfo<T6>(ref span6[0], 1);
                var span7 = _iterator.Column<T7>();
                _refInfo7 = span7.IsEmpty
                    ? new ComponentRefInfo<T7>(ref Unsafe.NullRef<T7>(), 0)
                    : new ComponentRefInfo<T7>(ref span7[0], 1);
                _entityStride = _entities.IsEmpty ? 0 : 1;
                _entityRef = _entities.IsEmpty
                    ? new RefRO<Entity>(ref Unsafe.NullRef<Entity>())
                    : new RefRO<Entity>(ref Unsafe.AsRef(in _entities[0]));
            }
            else
            {
                _refInfo0.Ref._value = ref Unsafe.Add(ref _refInfo0.Ref._value, _refInfo0.Stride);
                _refInfo1.Ref._value = ref Unsafe.Add(ref _refInfo1.Ref._value, _refInfo1.Stride);
                _refInfo2.Ref._value = ref Unsafe.Add(ref _refInfo2.Ref._value, _refInfo2.Stride);
                _refInfo3.Ref._value = ref Unsafe.Add(ref _refInfo3.Ref._value, _refInfo3.Stride);
                _refInfo4.Ref._value = ref Unsafe.Add(ref _refInfo4.Ref._value, _refInfo4.Stride);
                _refInfo5.Ref._value = ref Unsafe.Add(ref _refInfo5.Ref._value, _refInfo5.Stride);
                _refInfo6.Ref._value = ref Unsafe.Add(ref _refInfo6.Ref._value, _refInfo6.Stride);
                _refInfo7.Ref._value = ref Unsafe.Add(ref _refInfo7.Ref._value, _refInfo7.Stride);
                _entityRef._value = ref Unsafe.Add(ref _entityRef._value, _entityStride);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6, T7> GetEnumerator() => this;
    }

}
