using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Systems;

namespace NacreCraft.Client.Blaze3d.Vertex;

//UberGpuBuffer<T> packs many small allocations into a few large GPU buffers, maps to vanilla UberGpuBuffer
//A key is a caller defined handle, typically one section mesh; each key owns exactly one allocation
//Data goes into the staging buffer first and only reaches the GPU when UploadStagedAllocations runs, which lets
//many small writes share one upload pass
public sealed class UberGpuBuffer<T> : IDisposable where T : notnull
{
    //UploadCallback notified once a key's bytes have landed on the GPU, maps to vanilla UploadCallback
    public interface IUploadCallback
    {
        void BufferHasBeenUploaded(T key);
    }

    private sealed class HeapEntry
    {
        public required TlsfAllocator.Heap Heap { get; init; }
        public required TlsfAllocator Allocator { get; init; }
        public required GpuBuffer Buffer { get; init; }
    }

    private sealed record StagedEntry(StagingBuffer.BufferHandle Handle, IUploadCallback? Callback);

    private readonly string _name;
    private readonly int _usage;
    private readonly long _heapSize;
    private readonly int _alignSize;
    private readonly StagingBuffer _stagingBuffer;
    private readonly List<HeapEntry> _heaps = new();
    private readonly Dictionary<T, StagedEntry> _staged = new();
    private readonly Dictionary<T, TlsfAllocator.Allocation> _allocations = new();
    private bool _disposed;

    public UberGpuBuffer(string name, int usage, long heapSize, int alignSize, StagingBuffer stagingBuffer)
    {
        _name = name;
        _usage = usage | GpuBuffer.UsageCopyDst | GpuBuffer.UsageCopySrc;
        _heapSize = heapSize;
        _alignSize = alignSize;
        _stagingBuffer = stagingBuffer;
    }

    //Name for debug output
    public string Name => _name;

    //HeapCount number of GPU buffers the uber buffer currently spans
    public int HeapCount => _heaps.Count;

    //AddAllocation stages data for a key, replacing anything the key staged earlier
    //Returns false when the staging batch cannot take the data, the caller uploads and retries
    public bool AddAllocation(T key, IUploadCallback? callback, ReadOnlySpan<byte> data)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UberGpuBuffer<T>));
        var handle = _stagingBuffer.TryAppend(data);
        if (handle is null) return false;
        if (_staged.TryGetValue(key, out var previous)) previous.Handle.Dispose();
        _staged[key] = new StagedEntry(handle, callback);
        return true;
    }

    //UploadStagedAllocations moves everything staged onto the GPU, maps to vanilla uploadStagedAllocations
    //Returns false when at least one key could not be placed, those keys stay staged for the next pass
    public bool UploadStagedAllocations(GpuDevice device, StagingBuffer.Uploader uploader)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UberGpuBuffer<T>));
        if (_staged.Count == 0) return true;

        var complete = true;
        var pending = new List<T>();
        foreach (var (key, entry) in _staged)
        {
            //The key's previous range is dead as soon as new bytes are staged for it
            if (_allocations.Remove(key, out var previous)) previous.Release();

            var allocation = FindOrCreateAllocation(device, entry.Handle.Size);
            if (allocation is null)
            {
                //No heap could take it; keep the bytes staged so the caller can retry
                complete = false;
                pending.Add(key);
                continue;
            }

            uploader.CopyTo(entry.Handle, GetGpuBuffer(allocation), allocation.OffsetFromHeap);
            _allocations[key] = allocation;
            entry.Callback?.BufferHasBeenUploaded(key);
            entry.Handle.Dispose();
        }

        foreach (var key in pending) _staged.Remove(key);
        RecycleEmptyHeaps();
        return complete;
    }

    //GetAllocation the range currently owned by a key, null when it has none
    public TlsfAllocator.Allocation? GetAllocation(T key)
        => _allocations.TryGetValue(key, out var allocation) ? allocation : null;

    //GetGpuBuffer the GPU buffer an allocation lives in
    public GpuBuffer GetGpuBuffer(TlsfAllocator.Allocation allocation)
    {
        foreach (var heap in _heaps)
            if (ReferenceEquals(heap.Heap, allocation.Heap)) return heap.Buffer;
        throw new ArgumentException("Allocation does not belong to this uber buffer", nameof(allocation));
    }

    //RemoveAllocation drops a key, freeing its range and discarding anything it staged
    public void RemoveAllocation(T key)
    {
        if (_staged.Remove(key, out var staged)) staged.Handle.Dispose();
        if (_allocations.Remove(key, out var allocation)) allocation.Release();
    }

    //FindOrCreateAllocation looks for a heap with room, opening a new one when none fits
    private TlsfAllocator.Allocation? FindOrCreateAllocation(GpuDevice device, long size)
    {
        if (size > _heapSize)
            throw new InvalidOperationException($"Allocation of {size} bytes exceeds the heap size of {_heapSize}");
        foreach (var heap in _heaps)
        {
            var allocation = heap.Allocator.Allocate(size, _alignSize);
            if (allocation is not null) return allocation;
        }
        var buffer = device.CreateBuffer($"{_name} heap {_heaps.Count}", _usage, _heapSize);
        var tlsfHeap = new TlsfAllocator.Heap(_heapSize);
        var entry = new HeapEntry { Heap = tlsfHeap, Allocator = new TlsfAllocator(tlsfHeap), Buffer = buffer };
        _heaps.Add(entry);
        return entry.Allocator.Allocate(size, _alignSize);
    }

    //RecycleEmptyHeaps closes the first heap that has all of its ranges back, one per pass like vanilla
    private void RecycleEmptyHeaps()
    {
        for (var i = 0; i < _heaps.Count; i++)
        {
            if (!_heaps[i].Allocator.IsCompletelyFree) continue;
            _heaps[i].Buffer.Dispose();
            _heaps.RemoveAt(i);
            return;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var entry in _staged.Values) entry.Handle.Dispose();
        _staged.Clear();
        foreach (var allocation in _allocations.Values) allocation.Release();
        _allocations.Clear();
        foreach (var heap in _heaps) heap.Buffer.Dispose();
        _heaps.Clear();
        _disposed = true;
    }
}
