namespace NacreCraft.Client.Blaze3d.Vertex;

//TlsfAllocator a variable size allocator over one fixed size heap, maps to vanilla TlsfAllocator
//Vanilla runs the two level bitmap TLSF algorithm; this implementation uses best fit over a doubly linked
//block list with neighbour merging, which keeps the same semantics and only differs in how fragmented it leaves the heap
public sealed class TlsfAllocator
{
    //Heap one fixed size block of memory the allocator carves up, maps to vanilla TlsfAllocator.Heap
    public class Heap
    {
        public Heap(long size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
        }

        //Size total bytes the heap covers
        public long Size { get; }
    }

    //Block one run of heap bytes, either free or handed out through an Allocation
    internal sealed class Block
    {
        public long Offset;
        public long Size;
        public bool Free;
        public Block? Previous;
        public Block? Next;
    }

    private readonly Heap _heap;
    private Block? _head;

    public TlsfAllocator(Heap heap)
    {
        _heap = heap;
        _head = new Block { Offset = 0, Size = heap.Size, Free = true };
    }

    //Allocate carves out size bytes aligned to align, or returns null when no free run fits
    public Allocation? Allocate(long size, int align)
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        if (align <= 0) throw new ArgumentOutOfRangeException(nameof(align));

        //Best fit: the smallest free run that still fits keeps large runs usable for large requests
        Block? best = null;
        for (var block = _head; block is not null; block = block.Next)
        {
            if (!block.Free) continue;
            var blockPadding = Align(block.Offset, align) - block.Offset;
            if (blockPadding + size > block.Size) continue;
            if (best is null || block.Size < best.Size) best = block;
        }
        if (best is null) return null;

        var alignedOffset = Align(best.Offset, align);
        //The alignment padding becomes its own free block so it can serve later small requests
        var padding = alignedOffset - best.Offset;
        if (padding > 0)
        {
            var front = new Block
            {
                Offset = best.Offset,
                Size = padding,
                Free = true,
                Previous = best.Previous,
                Next = best
            };
            if (best.Previous is not null) best.Previous.Next = front;
            else _head = front;
            best.Previous = front;
            best.Offset = alignedOffset;
            best.Size -= padding;
        }

        //Split off the tail so the remainder stays available
        var tail = best.Size - size;
        if (tail > 0)
        {
            var remainder = new Block
            {
                Offset = alignedOffset + size,
                Size = tail,
                Free = true,
                Previous = best,
                Next = best.Next
            };
            if (best.Next is not null) best.Next.Previous = remainder;
            best.Next = remainder;
            best.Size = size;
        }

        best.Free = false;
        return new Allocation(this, _heap, best);
    }

    //Free returns an allocation to the heap, maps to vanilla free
    public void Free(Allocation allocation)
    {
        if (allocation is null) throw new ArgumentNullException(nameof(allocation));
        if (allocation.Heap != _heap)
            throw new ArgumentException("Allocation does not belong to this allocator", nameof(allocation));
        allocation.Release();
    }

    //IsCompletelyFree whether the whole heap is back to one free run, maps to vanilla isCompletelyFree
    public bool IsCompletelyFree => _head is { Free: true, Next: null } && _head.Offset == 0 && _head.Size == _heap.Size;

    //ReleaseBlock marks a block free and merges it with adjacent free neighbours
    private void ReleaseBlock(Block block)
    {
        block.Free = true;
        if (block.Next is { Free: true } next)
        {
            block.Size += next.Size;
            block.Next = next.Next;
            if (next.Next is not null) next.Next.Previous = block;
        }
        if (block.Previous is { Free: true } previous)
        {
            previous.Size += block.Size;
            previous.Next = block.Next;
            if (block.Next is not null) block.Next.Previous = previous;
        }
    }

    private static long Align(long value, long alignment)
        => alignment <= 1 ? value : (value + alignment - 1) / alignment * alignment;

    //Allocation one handed out run of heap bytes, maps to vanilla TlsfAllocator.Allocation
    public sealed class Allocation
    {
        private readonly TlsfAllocator _owner;
        private readonly Block _block;
        private bool _freed;

        internal Allocation(TlsfAllocator owner, Heap heap, Block block)
        {
            _owner = owner;
            _block = block;
            Heap = heap;
        }

        //Size bytes of the allocation
        public long Size => _block.Size;

        //Heap the heap the allocation came from
        public Heap Heap { get; }

        //OffsetFromHeap byte offset inside the heap, the upload destination
        public long OffsetFromHeap => _block.Offset;

        //IsFreed whether it has already been returned, freeing twice is a no-op
        public bool IsFreed => _freed;

        public void Free() => Release();

        internal void Release()
        {
            if (_freed) return;
            _freed = true;
            _owner.ReleaseBlock(_block);
        }
    }
}
