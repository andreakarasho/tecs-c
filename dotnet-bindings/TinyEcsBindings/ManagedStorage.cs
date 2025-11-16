using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace TinyEcsBindings;

/// <summary>
/// Pluggable storage provider for managed C# components
/// </summary>
public static unsafe class ManagedStorage
{
    private const string DllName = "tinyecs";

    // ============================================================================
    // Storage Provider Structure (must match C layout)
    // ============================================================================

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void* AllocChunkDelegate(void* userData, int componentSize, int capacity);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FreeChunkDelegate(void* userData, void* chunkData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void* GetPtrDelegate(void* userData, void* chunkData, int index, int size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetDataDelegate(void* userData, void* chunkData, int index, void* data, int size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CopyDataDelegate(void* userData, void* srcChunk, int srcIdx, void* dstChunk, int dstIdx, int size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SwapDataDelegate(void* userData, void* chunkData, int idxA, int idxB, int size);

    [StructLayout(LayoutKind.Sequential)]
    public struct StorageProvider
    {
        public IntPtr alloc_chunk;    // Function pointer
        public IntPtr free_chunk;     // Function pointer
        public IntPtr get_ptr;        // Function pointer
        public IntPtr set_data;       // Function pointer
        public IntPtr copy_data;      // Function pointer
        public IntPtr swap_data;      // Function pointer
        public IntPtr user_data;      // void*
        public IntPtr name;           // const char*
    }

    // ============================================================================
    // P/Invoke Declarations
    // ============================================================================

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern TinyEcs.ComponentId tecs_register_component_ex(
        TinyEcs.World world,
        byte* name,
        int size,
        StorageProvider* storageProvider);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern StorageProvider* tecs_get_default_storage_provider();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void* tecs_iter_chunk_data(TinyEcs.QueryIter* iter, int columnIndex);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern StorageProvider* tecs_iter_storage_provider(TinyEcs.QueryIter* iter, int index);

    // ============================================================================
    // Managed Command Buffer
    // ============================================================================

    /// <summary>
    /// Command buffer for managed components - keeps objects alive during deferred command execution
    /// </summary>
    public sealed class ManagedCommandBuffer<T> where T : notnull
    {
        private readonly List<T?> _pendingValues = new();

        public int Store(T? value)
        {
            int index = _pendingValues.Count;
            _pendingValues.Add(value);
            return index;
        }

        public T? Get(int index) => _pendingValues[index];

        public void Clear() => _pendingValues.Clear();
    }

    // Global registry of command buffers per component type
    private static readonly Dictionary<TinyEcs.ComponentId, object> _commandBuffers = new();

    /// <summary>
    /// Get or create a command buffer for a specific component type
    /// </summary>
    public static ManagedCommandBuffer<T> GetOrCreateCommandBuffer<T>(TinyEcs.ComponentId componentId) where T : notnull
    {
        lock (_commandBuffers)
        {
            if (!_commandBuffers.TryGetValue(componentId, out var buffer))
            {
                buffer = new ManagedCommandBuffer<T>();
                _commandBuffers[componentId] = buffer;
            }
            return (ManagedCommandBuffer<T>)buffer;
        }
    }

    /// <summary>
    /// Clear all command buffers - call after tbevy_commands_apply
    /// </summary>
    public static void ClearCommandBuffers()
    {
        lock (_commandBuffers)
        {
            foreach (var kvp in _commandBuffers)
            {
                if (kvp.Value is ManagedCommandBuffer<object> buffer)
                {
                    buffer.Clear();
                }
            }
        }
    }

    // ============================================================================
    // Managed Component Storage
    // ============================================================================

    /// <summary>
    /// Storage for managed (reference type) components.
    /// Uses GCHandles to prevent GC from moving objects.
    /// A pinned IntPtr array holds the GCHandle values for C interop.
    /// </summary>
    public sealed class ManagedComponentStorage<T> : IDisposable where T : notnull
    {
        private readonly T?[] _objects;          // Actual objects stored here
        private bool _disposed;

        public ManagedComponentStorage(int capacity)
        {
            // Allocate array to hold the actual objects
            _objects = new T?[capacity];
        }

        public void Set(int index, ref T? value)
        {
            // Simply store in the array
            _objects[index] = value;
        }

        public ref T? Get(int index)
        {
            return ref _objects[index];
        }

        public void Copy(int srcIdx, int dstIdx)
        {
            // Copy the reference
            _objects[dstIdx] = _objects[srcIdx];
        }

        public void Swap(int idxA, int idxB)
        {
            // Swap the object references
            (_objects[idxA], _objects[idxB]) = (_objects[idxB], _objects[idxA]);
        }

        public Span<T?> AsSpan(int start, int count)
        {
            // Return a span directly over the managed array
            return new Span<T?>(_objects, start, count);
        }

        public T?[] GetArray() => _objects;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        ~ManagedComponentStorage()
        {
            Dispose();
        }
    }

    // ============================================================================
    // Storage Provider Factory
    // ============================================================================

    /// <summary>
    /// Creates a storage provider for managed components
    /// </summary>
    public sealed class ManagedStorageProvider<T> : IDisposable where T : notnull
    {
        private readonly List<ManagedComponentStorage<T>> _storages = new();
        private readonly Dictionary<IntPtr, ManagedComponentStorage<T>> _chunkToStorage = new();
        private readonly GCHandle _storagesHandle;
        private readonly StorageProvider _provider;
        private readonly GCHandle _providerHandle;

        private readonly AllocChunkDelegate _allocDelegate;
        private readonly FreeChunkDelegate _freeDelegate;
        private readonly GetPtrDelegate _getPtrDelegate;
        private readonly SetDataDelegate _setDataDelegate;
        private readonly CopyDataDelegate _copyDataDelegate;
        private readonly SwapDataDelegate _swapDataDelegate;

        private bool _disposed;
        private TinyEcs.ComponentId _componentId; // Store component ID for command buffer access

        internal void SetComponentId(TinyEcs.ComponentId componentId)
        {
            _componentId = componentId;
        }

        public ManagedStorageProvider()
        {
            // Keep delegates alive to prevent GC collection
            _allocDelegate = AllocChunk;
            _freeDelegate = FreeChunk;
            _getPtrDelegate = GetPtr;
            _setDataDelegate = SetData;
            _copyDataDelegate = CopyData;
            _swapDataDelegate = SwapData;

            _storagesHandle = GCHandle.Alloc(_storages);

            _provider = new StorageProvider
            {
                alloc_chunk = Marshal.GetFunctionPointerForDelegate(_allocDelegate),
                free_chunk = Marshal.GetFunctionPointerForDelegate(_freeDelegate),
                get_ptr = Marshal.GetFunctionPointerForDelegate(_getPtrDelegate),
                set_data = Marshal.GetFunctionPointerForDelegate(_setDataDelegate),
                copy_data = Marshal.GetFunctionPointerForDelegate(_copyDataDelegate),
                swap_data = Marshal.GetFunctionPointerForDelegate(_swapDataDelegate),
                user_data = GCHandle.ToIntPtr(_storagesHandle),
                name = Marshal.StringToHGlobalAnsi($"managed<{typeof(T).ToString()}>")
            };

            _providerHandle = GCHandle.Alloc(_provider, GCHandleType.Pinned);
        }

        public StorageProvider* GetProviderPointer()
        {
            return (StorageProvider*)_providerHandle.AddrOfPinnedObject();
        }

        private void* AllocChunk(void* userData, int componentSize, int capacity)
        {
            var storage = new ManagedComponentStorage<T>(capacity);
            _storages.Add(storage);
            var handle = GCHandle.Alloc(storage);
            var handlePtr = GCHandle.ToIntPtr(handle);

            // Store mapping from chunkData to storage (both local and global)
            _chunkToStorage[handlePtr] = storage;

            return (void*)handlePtr;
        }

        private void FreeChunk(void* userData, void* chunkData)
        {
            if (chunkData == null) return;

            var chunkPtr = (IntPtr)chunkData;
            if (_chunkToStorage.TryGetValue(chunkPtr, out var storage))
            {
                _chunkToStorage.Remove(chunkPtr);
                _storages.Remove(storage);
                storage.Dispose();
            }

            var handle = GCHandle.FromIntPtr(chunkPtr);
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }

        private void* GetPtr(void* userData, void* chunkData, int index, int size)
        {
            var storageHandle = GCHandle.FromIntPtr((IntPtr)chunkData);
            var storage = (ManagedComponentStorage<T>)storageHandle.Target!;
            ref var obj = ref storage.Get(index);
            return Unsafe.AsPointer(ref obj!);
        }

        private void SetData(void* userData, void* chunkData, int index, void* data, int size)
        {
            var storageHandle = GCHandle.FromIntPtr((IntPtr)chunkData);
            var storage = (ManagedComponentStorage<T>)storageHandle.Target!;

            if (data != null)
            {
                // Read the IntPtr value from data
                IntPtr dataValue = *(IntPtr*)data;

                // Check if this is a command buffer index (encoded as negative IntPtr)
                if (dataValue.ToInt64() < 0)
                {
                    // Decode buffer index: -(index + 1)
                    int bufferIndex = -(int)dataValue.ToInt64() - 1;
                    var commandBuffer = GetOrCreateCommandBuffer<T>(_componentId);
                    T? obj = commandBuffer.Get(bufferIndex);
                    storage.Set(index, ref obj);
                }
                else
                {
                    // Direct pointer to object reference (immediate mode)
                    ref var obj = ref Unsafe.AsRef<T?>(data);
                    storage.Set(index, ref obj);
                }
            }
            else
            {
                T? nullValue = default;
                storage.Set(index, ref nullValue);
            }
        }

        private void CopyData(void* userData, void* srcChunk, int srcIdx, void* dstChunk, int dstIdx, int size)
        {
            var srcHandle = GCHandle.FromIntPtr((IntPtr)srcChunk);
            var srcStorage = (ManagedComponentStorage<T>)srcHandle.Target!;

            var dstHandle = GCHandle.FromIntPtr((IntPtr)dstChunk);
            var dstStorage = (ManagedComponentStorage<T>)dstHandle.Target!;

            var srcValue = srcStorage.Get(srcIdx);
            dstStorage.Set(dstIdx, ref srcValue);
        }

        private void SwapData(void* userData, void* chunkData, int idxA, int idxB, int size)
        {
            var handle = GCHandle.FromIntPtr((IntPtr)chunkData);
            var storage = (ManagedComponentStorage<T>)handle.Target!;
            storage.Swap(idxA, idxB);
        }

        /// <summary>
        /// Add a managed component to an entity.
        /// The object reference is stored directly in the pinned array - no boxing or GCHandles per component!
        /// </summary>
        public void AddComponent(TinyEcs.World world, TinyEcs.Entity entity, TinyEcs.ComponentId componentId, T? value)
        {
            // We need to pass a pointer to the object reference
            // Create a stack variable to hold the reference and get its address
            void* ptr = Unsafe.AsPointer(ref value);
            TinyEcs.tecs_set(world, entity, componentId, ptr, IntPtr.Size);
        }

        /// <summary>
        /// Get a managed component from an entity.
        /// Uses the pointer ID mapping created by GetPtr.
        /// </summary>
        public ref T? GetComponent(TinyEcs.World world, TinyEcs.Entity entity, TinyEcs.ComponentId componentId)
        {
            // Call tecs_get which will invoke our GetPtr callback
            var ptr = TinyEcs.tecs_get(world, entity, componentId);
            return ref Unsafe.AsRef<T?>(ptr);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var storage in _storages)
                {
                    storage.Dispose();
                }
                _storages.Clear();

                if (_storagesHandle.IsAllocated)
                    _storagesHandle.Free();

                if (_providerHandle.IsAllocated)
                    _providerHandle.Free();

                if (_provider.name != IntPtr.Zero)
                    Marshal.FreeHGlobal(_provider.name);

                _disposed = true;
            }
        }

        ~ManagedStorageProvider()
        {
            Dispose();
        }
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    /// <summary>
    /// Register a managed component type with custom storage
    /// </summary>
    public static TinyEcs.ComponentId RegisterManagedComponent<T>(
        TinyEcs.World world,
        string name,
        out ManagedStorageProvider<T> storageProvider) where T : notnull
    {
        storageProvider = new ManagedStorageProvider<T>();

        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* namePtr = nameBytes)
        {
            var componentId = tecs_register_component_ex(
                world,
                namePtr,
                IntPtr.Size,  // Size of reference/pointer
                storageProvider.GetProviderPointer()
            );

            // Store component ID in the provider for command buffer access
            storageProvider.SetComponentId(componentId);

            return componentId;
        }
    }

    /// <summary>
    /// Get component at specific index in iteration (works with any storage)
    /// </summary>
    public static T* GetAt<T>(TinyEcs.QueryIter* iter, int columnIndex, int rowIndex) where T : unmanaged
    {
        var column = TinyEcs.tecs_iter_column(iter, columnIndex);
        return column != null ? &((T*)column)[rowIndex] : null;
    }

    /// <summary>
    /// Get a managed component value at a specific row in the iteration
    /// Note: Returns by value since we're accessing managed array
    /// </summary>
    public static ref T? GetManagedAt<T>(TinyEcs.QueryIter* iter, int columnIndex, int rowIndex) where T : notnull
    {
        // Get the chunk's storage data pointer
        var chunkData = (IntPtr)tecs_iter_chunk_data(iter, columnIndex);
        if (chunkData == IntPtr.Zero)
            return ref Unsafe.NullRef<T?>();

        var handle = GCHandle.FromIntPtr(chunkData);
        var storage = (ManagedComponentStorage<T>)handle.Target!;
        return ref storage.Get(rowIndex);
    }

    /// <summary>
    /// Get a Span over managed components for iteration.
    /// This returns a span directly over the managed object array (GC-safe).
    /// Use this for high-performance iteration when you need to access many components.
    /// </summary>
    public static Span<T> GetManagedSpan<T>(TinyEcs.QueryIter* iter, int columnIndex) where T : notnull
    {
        var count = TinyEcs.tecs_iter_count(iter);
        if (count == 0)
            return Span<T>.Empty;

        // Get the chunk's storage data pointer directly
        var chunkData = (IntPtr)tecs_iter_chunk_data(iter, columnIndex);
        if (chunkData == IntPtr.Zero)
            return Span<T>.Empty;

        var handle = GCHandle.FromIntPtr(chunkData);
        var storage = (ManagedComponentStorage<T>)handle.Target!;
        return storage.AsSpan(0, count)!;
    }

    /// <summary>
    /// Iterate over entities with mixed unmanaged and managed components using Span for unmanaged.
    /// More efficient than ForEachMixed when you need to process all components.
    /// </summary>
    public static void ForEachSpan<T1, T2>(
        TinyEcs.QueryIter* iter,
        Action<Span<T1>, Span<T2?>> action)
        where T1 : unmanaged
        where T2 : notnull
    {
        while (TinyEcs.tecs_iter_next(iter))
        {
            var count = TinyEcs.tecs_iter_count(iter);
            if (count == 0) continue;

            // Get span for unmanaged component
            var c1Ptr = TinyEcs.tecs_iter_column(iter, 0);
            Span<T1> span1;
            if (c1Ptr != null)
            {
                span1 = new Span<T1>(c1Ptr, count);
            }
            else
            {
                // Pluggable storage - cannot use span, fall back to per-entity
                for (int i = 0; i < count; i++)
                {
                    // Would need to use GetAt - not ideal for span API
                    // Better to use ForEachMixed in this case
                }
                continue;
            }

            // Get span for managed component (span of object references)
            var span2 = GetManagedSpan<T2>(iter, 1);

            action(span1, span2);
        }
    }

    // ============================================================================
    // Iteration Delegates for Managed Components
    // ============================================================================

    /// <summary>
    /// Delegate for iterating with 1 unmanaged and 1 managed component
    /// </summary>
    public delegate void MixedAction<T1, T2>(int index, ref T1 c1, T2? c2)
        where T1 : unmanaged
        where T2 : notnull;

    /// <summary>
    /// Delegate for iterating with 2 unmanaged and 1 managed component
    /// </summary>
    public delegate void MixedAction<T1, T2, T3>(int index, ref T1 c1, ref T2 c2, T3? c3)
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : notnull;

    /// <summary>
    /// Delegate for iterating with 1 managed component
    /// </summary>
    public delegate void ManagedAction<T1>(int index, T1? c1)
        where T1 : notnull;

    /// <summary>
    /// Delegate for iterating with 2 managed components
    /// </summary>
    public delegate void ManagedAction<T1, T2>(int index, T1? c1, T2? c2)
        where T1 : notnull
        where T2 : notnull;

    // ============================================================================
    // Iteration Methods for Managed Components
    // ============================================================================

    /// <summary>
    /// Iterate over entities with mixed managed and unmanaged components
    /// </summary>
    public static void ForEachMixed<T1, T2>(
        TinyEcs.QueryIter* iter,
        MixedAction<T1, T2> action)
        where T1 : unmanaged
        where T2 : notnull
    {
        while (TinyEcs.tecs_iter_next(iter))
        {
            var count = TinyEcs.tecs_iter_count(iter);

            // Check if column 0 uses native storage
            var c1Ptr = TinyEcs.tecs_iter_column(iter, 0);
            if (c1Ptr != null)
            {
                // Native storage - can use pointer directly
                var c1 = (T1*)c1Ptr;
                for (int i = 0; i < count; i++)
                {
                    var c2 = GetManagedAt<T2>(iter, 1, i);
                    action(i, ref c1[i], c2);
                }
            }
            else
            {
                // Pluggable storage - use GetAt
                for (int i = 0; i < count; i++)
                {
                    ref var c1 = ref *GetAt<T1>(iter, 0, i);
                    var c2 = GetManagedAt<T2>(iter, 1, i);
                    action(i, ref c1, c2);
                }
            }
        }
    }

    /// <summary>
    /// Iterate over entities with 2 unmanaged and 1 managed component
    /// </summary>
    public static void ForEachMixed<T1, T2, T3>(
        TinyEcs.QueryIter* iter,
        MixedAction<T1, T2, T3> action)
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : notnull
    {
        while (TinyEcs.tecs_iter_next(iter))
        {
            var count = TinyEcs.tecs_iter_count(iter);
            var c1 = TinyEcs.IterColumn<T1>(iter, 0);
            var c2 = TinyEcs.IterColumn<T2>(iter, 1);

            for (int i = 0; i < count; i++)
            {
                var c3 = GetManagedAt<T3>(iter, 2, i);
                action(i, ref c1[i], ref c2[i], c3);
            }
        }
    }

    /// <summary>
    /// Iterate over entities with only managed components
    /// </summary>
    public static void ForEachManaged<T1>(
        TinyEcs.QueryIter* iter,
        ManagedAction<T1> action)
        where T1 : notnull
    {
        while (TinyEcs.tecs_iter_next(iter))
        {
            var count = TinyEcs.tecs_iter_count(iter);
            for (int i = 0; i < count; i++)
            {
                var c1 = GetManagedAt<T1>(iter, 0, i);
                action(i, c1);
            }
        }
    }

    /// <summary>
    /// Iterate over entities with 2 managed components
    /// </summary>
    public static void ForEachManaged<T1, T2>(
        TinyEcs.QueryIter* iter,
        ManagedAction<T1, T2> action)
        where T1 : notnull
        where T2 : notnull
    {
        while (TinyEcs.tecs_iter_next(iter))
        {
            var count = TinyEcs.tecs_iter_count(iter);
            for (int i = 0; i < count; i++)
            {
                var c1 = GetManagedAt<T1>(iter, 0, i);
                var c2 = GetManagedAt<T2>(iter, 1, i);
                action(i, c1, c2);
            }
        }
    }
}
