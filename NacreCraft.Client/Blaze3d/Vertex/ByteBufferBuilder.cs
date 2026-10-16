using System.Runtime.InteropServices;

namespace NacreCraft.Client.Blaze3d.Vertex;

//ByteBufferBuilder a growable native byte arena, maps to vanilla ByteBufferBuilder
//Reserve hands out a byte range inside it; Build wraps everything written since the previous build into a Result
//A Result stores an offset rather than a pointer, so it stays valid across the realloc that growth performs
//Once every Result is released the whole block is handed out again; otherwise the arena only grows
public sealed unsafe class ByteBufferBuilder : IDisposable
{
    private const int DefaultMaxCapacity = int.MaxValue;

    private readonly int _maxCapacity;
    private nint _pointer;
    private int _capacity;
    private int _writeOffset;
    private int _resultStart;
    private int _openResults;
    private bool _disposed;

    public ByteBufferBuilder(int initialCapacity, int maxCapacity = DefaultMaxCapacity)
    {
        if (initialCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        if (maxCapacity < initialCapacity) throw new ArgumentOutOfRangeException(nameof(maxCapacity));
        _capacity = initialCapacity;
        _maxCapacity = maxCapacity;
        _pointer = (nint)NativeMemory.Alloc((nuint)initialCapacity);
    }

    //Pointer the base address of the native block
    public nint Pointer => _pointer;

    //Capacity bytes currently allocated
    public int Capacity => _capacity;

    //BytesWritten how much of the block is in use
    public int BytesWritten => _writeOffset;

    //Reserve reserves size bytes and returns a pointer to the reserved range
    public nint Reserve(int size)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ByteBufferBuilder));
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
        EnsureCapacity(size);
        var pointer = _pointer + _writeOffset;
        _writeOffset += size;
        return pointer;
    }

    //Build returns a view over the bytes written since the previous build, or null when nothing was written
    public Result? Build()
    {
        if (_writeOffset == _resultStart) return null;
        var result = new Result(this, _resultStart, _writeOffset - _resultStart);
        _resultStart = _writeOffset;
        _openResults++;
        return result;
    }

    //Clear resets the write cursor, only valid while no Result is outstanding
    public void Clear()
    {
        if (_openResults > 0)
            throw new InvalidOperationException("Cannot clear while a Result is still open");
        _writeOffset = 0;
        _resultStart = 0;
    }

    internal void ReleaseResult()
    {
        _openResults--;
        //Every view is gone, so the whole block can be handed out again
        if (_openResults == 0)
        {
            _writeOffset = 0;
            _resultStart = 0;
        }
    }

    private void EnsureCapacity(int size)
    {
        if (_writeOffset + size <= _capacity) return;
        var required = _writeOffset + size;
        if (required > _maxCapacity)
            throw new InvalidOperationException($"Buffer would grow past the {_maxCapacity} byte limit");
        var capacity = _capacity;
        while (capacity < required)
            capacity = (int)Math.Min((long)capacity * 2, _maxCapacity);
        _pointer = (nint)NativeMemory.Realloc((void*)_pointer, (nuint)capacity);
        _capacity = capacity;
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_pointer != 0)
        {
            NativeMemory.Free((void*)_pointer);
            _pointer = 0;
        }
        _capacity = 0;
        _disposed = true;
    }

    //Result a view over a byte range of the arena, maps to vanilla ByteBufferBuilder.Result
    //Holding one keeps the arena from recycling those bytes
    public sealed unsafe class Result : IDisposable
    {
        private readonly ByteBufferBuilder _owner;
        private readonly int _offset;
        private bool _closed;

        internal Result(ByteBufferBuilder owner, int offset, int size)
        {
            _owner = owner;
            _offset = offset;
            Size = size;
        }

        //Size bytes covered by this view
        public int Size { get; }

        //AsSpan the bytes, recomputed from the arena's current base so growth does not invalidate the view
        public Span<byte> AsSpan()
        {
            if (_closed) throw new ObjectDisposedException(nameof(Result));
            return new Span<byte>((void*)(_owner._pointer + _offset), Size);
        }

        public void Dispose()
        {
            if (_closed) return;
            _closed = true;
            _owner.ReleaseResult();
        }
    }
}
