namespace NacreCraft.Client.Blaze3d.Resource;

//CrossFrameResourcePool keeps released resources alive for a few frames and hands them back when a matching
//descriptor asks again, maps to vanilla com.mojang.blaze3d.resource.CrossFrameResourcePool
//Reusing a render target across frames is free where reallocating one each frame would stall the GPU
public sealed class CrossFrameResourcePool : IGraphicsResourceAllocator, IDisposable
{
    private readonly int _framesToKeepResource;
    //_pool holds the most recently released resource first, matching the vanilla addFirst and iteration order
    private readonly LinkedList<ResourceEntry> _pool = new();

    public CrossFrameResourcePool(int framesToKeepResource) => _framesToKeepResource = framesToKeepResource;

    //EntryCount how many resources are currently parked in the pool, for diagnostics
    public int EntryCount => _pool.Count;

    //EndFrame ages every parked resource once and frees the ones whose lifetime ran out
    public void EndFrame()
    {
        var node = _pool.First;
        while (node is not null)
        {
            var next = node.Next;
            var entry = node.Value;
            //Vanilla decrements first and frees only when the counter was already zero
            if (entry.FramesToLive-- == 0)
            {
                entry.Free();
                _pool.Remove(node);
            }
            node = next;
        }
    }

    public T Acquire<T>(IResourceDescriptor<T> descriptor)
    {
        var resource = AcquireWithoutPreparing(descriptor);
        descriptor.Prepare(resource);
        return resource;
    }

    //AcquireWithoutPreparing takes a matching parked resource or allocates a fresh one
    private T AcquireWithoutPreparing<T>(IResourceDescriptor<T> descriptor)
    {
        var node = _pool.First;
        while (node is not null)
        {
            var next = node.Next;
            if (node.Value is ResourceEntry<T> entry && descriptor.CanUsePhysicalResource(entry.Descriptor))
            {
                _pool.Remove(node);
                return entry.Value;
            }
            node = next;
        }
        return descriptor.Allocate();
    }

    //Release parks a resource, it stays until its lifetime runs out
    public void Release<T>(IResourceDescriptor<T> descriptor, T resource)
        => _pool.AddFirst(new ResourceEntry<T>(descriptor, resource, _framesToKeepResource));

    //Clear frees every parked resource right away
    public void Clear()
    {
        foreach (var entry in _pool) entry.Free();
        _pool.Clear();
    }

    public void Dispose() => Clear();

    //ResourceEntry one parked resource plus how many more frames it may sit unused
    private abstract class ResourceEntry
    {
        public IResourceDescriptor Descriptor { get; }
        public int FramesToLive;

        protected ResourceEntry(IResourceDescriptor descriptor, int framesToLive)
        {
            Descriptor = descriptor;
            FramesToLive = framesToLive;
        }

        public abstract void Free();
    }

    //ResourceEntry<T> keeps the typed descriptor so Free can be called without a cast at the call site
    private sealed class ResourceEntry<T> : ResourceEntry
    {
        private readonly IResourceDescriptor<T> _descriptor;

        public T Value { get; }

        public ResourceEntry(IResourceDescriptor<T> descriptor, T value, int framesToLive)
            : base(descriptor, framesToLive)
        {
            _descriptor = descriptor;
            Value = value;
        }

        public override void Free() => _descriptor.Free(Value);
    }
}
