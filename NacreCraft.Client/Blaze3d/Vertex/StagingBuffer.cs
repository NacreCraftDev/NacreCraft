using NacreCraft.Client.Blaze3d.Systems;

namespace NacreCraft.Client.Blaze3d.Vertex;

//StagingBuffer a CPU side batch of bytes waiting to be uploaded, maps to vanilla StagingBuffer
//Vanilla copies through a mapped GPU buffer; here the batch is kept in managed memory and handed to
//CommandEncoder.WriteToBuffer, which is a direct host visible write, so no staging GPU buffer is needed
//Appends never grow the batch: a full batch makes TryAppend return null so the caller uploads and retries
public sealed class StagingBuffer : IDisposable
{
    private readonly string _name;
    private readonly byte[] _buffer;
    private int _writeOffset;
    //_openHandles views handed out by TryAppend that have not been released yet
    private int _openHandles;
    private bool _disposed;

    private StagingBuffer(string name, int bufferSize)
    {
        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
        _name = name;
        _buffer = new byte[bufferSize];
    }

    //Create builds a staging buffer of the given size, maps to vanilla StagingBuffer.create
    public static StagingBuffer Create(string name, int bufferSize) => new(name, bufferSize);

    //Name for debug output
    public string Name => _name;

    //Capacity bytes the batch can hold before it has to be uploaded
    public int Capacity => _buffer.Length;

    //BytesUsed how much of the batch is filled
    public int BytesUsed => _writeOffset;

    //TryAppend copies data into the batch and returns a handle, or null when the data does not fit
    public BufferHandle? TryAppend(ReadOnlySpan<byte> data)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(StagingBuffer));
        if (data.Length == 0) return null;
        if (_writeOffset + data.Length > _buffer.Length) return null;
        var offset = _writeOffset;
        data.CopyTo(_buffer.AsSpan(offset));
        _writeOffset += data.Length;
        _openHandles++;
        return new BufferHandle(this, offset, data.Length);
    }

    //StartUploading opens a batch, maps to vanilla startUploading
    //Disposing the uploader ends the batch and recycles the area once every handle has been released
    public Uploader StartUploading(CommandEncoder encoder) => new(this, encoder);

    private ReadOnlySpan<byte> SpanOf(BufferHandle handle)
    {
        handle.CheckValidFor(this);
        return new ReadOnlySpan<byte>(_buffer, handle.Offset, handle.Size);
    }

    private void ReleaseHandle()
    {
        _openHandles--;
        TryRecycle();
    }

    private void CompleteBatch()
    {
        if (_writeOffset == 0) return;
        //Still referenced by an outstanding handle, the caller must upload those first
        if (_openHandles > 0) return;
        _writeOffset = 0;
    }

    private void TryRecycle()
    {
        if (_openHandles == 0 && _writeOffset > 0)
            _writeOffset = 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }

    //BufferHandle a view over one appended range, maps to vanilla StagingBuffer.BufferHandle
    //Holding it keeps the range from being recycled
    public sealed class BufferHandle : IDisposable
    {
        private readonly StagingBuffer _owner;
        private bool _closed;

        internal BufferHandle(StagingBuffer owner, int offset, int size)
        {
            _owner = owner;
            Offset = offset;
            Size = size;
        }

        //Offset byte offset of the range inside the batch
        public int Offset { get; }

        //Size bytes of the range
        public int Size { get; }

        internal void CheckValidFor(StagingBuffer staging)
        {
            if (_closed) throw new InvalidOperationException("BufferHandle has already been closed");
            if (!ReferenceEquals(staging, _owner))
                throw new ArgumentException("BufferHandle does not belong to this StagingBuffer", nameof(staging));
        }

        public void Dispose()
        {
            if (_closed) return;
            _closed = true;
            _owner.ReleaseHandle();
        }
    }

    //Uploader one upload batch, maps to vanilla StagingBuffer.Uploader
    public sealed class Uploader : IDisposable
    {
        private readonly StagingBuffer _staging;
        private readonly CommandEncoder _encoder;
        private bool _closed;

        internal Uploader(StagingBuffer staging, CommandEncoder encoder)
        {
            _staging = staging;
            _encoder = encoder;
        }

        //CopyTo copies one staged range into the destination buffer at the given byte offset
        public void CopyTo(BufferHandle source, Buffers.GpuBuffer destination, long destinationOffset)
        {
            if (_closed) throw new InvalidOperationException("Uploader has already been closed");
            if (destinationOffset < 0 || destinationOffset + source.Size > destination.Size)
                throw new ArgumentOutOfRangeException(nameof(destinationOffset));
            var target = destination.Slice(destinationOffset, source.Size);
            _encoder.WriteToBuffer(target, _staging.SpanOf(source));
        }

        public void Dispose()
        {
            if (_closed) return;
            _closed = true;
            _staging.CompleteBatch();
        }
    }
}
