using System.Runtime.InteropServices;

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
    public static extern void* tecs_iter_get_at(TinyEcs.QueryIter* iter, int columnIndex, int rowIndex);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern StorageProvider* tecs_iter_storage_provider(TinyEcs.QueryIter* iter, int index);

    // ============================================================================
    // Managed Component Storage
    // ============================================================================

    /// <summary>
    /// Storage for managed (reference type) components using pinned C# arrays
    /// </summary>
    public sealed class ManagedComponentStorage<T> : IDisposable where T : class
    {
        private readonly T?[] _components;
        private readonly GCHandle _arrayHandle;
        private readonly IntPtr* _arrayPtr;
        private bool _disposed;

        public ManagedComponentStorage(int capacity)
        {
            _components = new T?[capacity];
            _arrayHandle = GCHandle.Alloc(_components, GCHandleType.Pinned);
            _arrayPtr = (IntPtr*)_arrayHandle.AddrOfPinnedObject();
        }

        public IntPtr* GetPointer(int index)
        {
            return _arrayPtr + index;
        }

        public void Set(int index, T? value)
        {
            _components[index] = value;
        }

        public T? Get(int index)
        {
            return _components[index];
        }

        public void Copy(int srcIdx, int dstIdx)
        {
            _components[dstIdx] = _components[srcIdx];
        }

        public void Swap(int idxA, int idxB)
        {
            (_components[idxA], _components[idxB]) = (_components[idxB], _components[idxA]);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_arrayHandle.IsAllocated)
                {
                    _arrayHandle.Free();
                }
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
    public sealed class ManagedStorageProvider<T> : IDisposable where T : class
    {
        private readonly List<ManagedComponentStorage<T>> storages = new();
        private readonly GCHandle storagesHandle;
        private readonly StorageProvider provider;
        private readonly GCHandle providerHandle;

        private readonly AllocChunkDelegate allocDelegate;
        private readonly FreeChunkDelegate freeDelegate;
        private readonly GetPtrDelegate getPtrDelegate;
        private readonly SetDataDelegate setDataDelegate;
        private readonly CopyDataDelegate copyDataDelegate;
        private readonly SwapDataDelegate swapDataDelegate;

        private bool disposed;

        public ManagedStorageProvider()
        {
            // Keep delegates alive to prevent GC collection
            allocDelegate = AllocChunk;
            freeDelegate = FreeChunk;
            getPtrDelegate = GetPtr;
            setDataDelegate = SetData;
            copyDataDelegate = CopyData;
            swapDataDelegate = SwapData;

            storagesHandle = GCHandle.Alloc(storages);

            provider = new StorageProvider
            {
                alloc_chunk = Marshal.GetFunctionPointerForDelegate(allocDelegate),
                free_chunk = Marshal.GetFunctionPointerForDelegate(freeDelegate),
                get_ptr = Marshal.GetFunctionPointerForDelegate(getPtrDelegate),
                set_data = Marshal.GetFunctionPointerForDelegate(setDataDelegate),
                copy_data = Marshal.GetFunctionPointerForDelegate(copyDataDelegate),
                swap_data = Marshal.GetFunctionPointerForDelegate(swapDataDelegate),
                user_data = GCHandle.ToIntPtr(storagesHandle),
                name = Marshal.StringToHGlobalAnsi($"managed<{typeof(T).ToString()}>")
            };

            providerHandle = GCHandle.Alloc(provider, GCHandleType.Pinned);
        }

        public StorageProvider* GetProviderPointer()
        {
            return (StorageProvider*)providerHandle.AddrOfPinnedObject();
        }

        private void* AllocChunk(void* userData, int componentSize, int capacity)
        {
            var storage = new ManagedComponentStorage<T>(capacity);
            storages.Add(storage);
            return (void*)GCHandle.ToIntPtr(GCHandle.Alloc(storage));
        }

        private void FreeChunk(void* userData, void* chunkData)
        {
            if (chunkData == null) return;

            var handle = GCHandle.FromIntPtr((IntPtr)chunkData);
            if (handle.IsAllocated)
            {
                var storage = (ManagedComponentStorage<T>)handle.Target!;
                storages.Remove(storage);
                storage.Dispose();
                handle.Free();
            }
        }

        private void* GetPtr(void* userData, void* chunkData, int index, int size)
        {
            var handle = GCHandle.FromIntPtr((IntPtr)chunkData);
            var storage = (ManagedComponentStorage<T>)handle.Target!;
            return storage.GetPointer(index);
        }

        private void SetData(void* userData, void* chunkData, int index, void* data, int size)
        {
            var handle = GCHandle.FromIntPtr((IntPtr)chunkData);
            var storage = (ManagedComponentStorage<T>)handle.Target!;

            // data points to a reference (IntPtr)
            var objPtr = *(IntPtr*)data;
            var obj = objPtr != IntPtr.Zero ? (T)GCHandle.FromIntPtr(objPtr).Target! : null;
            storage.Set(index, obj);
        }

        private void CopyData(void* userData, void* srcChunk, int srcIdx, void* dstChunk, int dstIdx, int size)
        {
            var srcHandle = GCHandle.FromIntPtr((IntPtr)srcChunk);
            var srcStorage = (ManagedComponentStorage<T>)srcHandle.Target!;

            var dstHandle = GCHandle.FromIntPtr((IntPtr)dstChunk);
            var dstStorage = (ManagedComponentStorage<T>)dstHandle.Target!;

            dstStorage.Set(dstIdx, srcStorage.Get(srcIdx));
        }

        private void SwapData(void* userData, void* chunkData, int idxA, int idxB, int size)
        {
            var handle = GCHandle.FromIntPtr((IntPtr)chunkData);
            var storage = (ManagedComponentStorage<T>)handle.Target!;
            storage.Swap(idxA, idxB);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                foreach (var storage in storages)
                {
                    storage.Dispose();
                }
                storages.Clear();

                if (storagesHandle.IsAllocated)
                    storagesHandle.Free();

                if (providerHandle.IsAllocated)
                    providerHandle.Free();

                if (provider.name != IntPtr.Zero)
                    Marshal.FreeHGlobal(provider.name);

                disposed = true;
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
        out ManagedStorageProvider<T> storageProvider) where T : class
    {
        storageProvider = new ManagedStorageProvider<T>();

        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* namePtr = nameBytes)
        {
            return tecs_register_component_ex(
                world,
                namePtr,
                IntPtr.Size,  // Size of reference/pointer
                storageProvider.GetProviderPointer()
            );
        }
    }

    /// <summary>
    /// Get component at specific index in iteration (works with any storage)
    /// </summary>
    public static T* GetAt<T>(TinyEcs.QueryIter* iter, int columnIndex, int rowIndex) where T : unmanaged
    {
        return (T*)tecs_iter_get_at(iter, columnIndex, rowIndex);
    }
}
