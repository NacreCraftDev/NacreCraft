using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Systems;

namespace NacreCraft.Client.Render;

//MappableRingBuffer a three-deep ring of host-mappable GPU buffers, maps to vanilla net.minecraft.client.renderer.MappableRingBuffer
//The render thread writes one buffer per frame while the previous frames are still in flight; a fence per slot stops the
//writer from reusing a buffer the GPU has not finished reading
public sealed class MappableRingBuffer : IDisposable
{
    private const int BufferCount = 3;

    private readonly GpuBuffer[] _buffers = new GpuBuffer[BufferCount];
    private readonly GpuFence?[] _fences = new GpuFence?[BufferCount];
    private readonly int _size;
    private int _current;
    private bool _disposed;

    public MappableRingBuffer(string label, int usage, int size)
    {
        if ((usage & (GpuBuffer.UsageMapRead | GpuBuffer.UsageMapWrite)) == 0)
            throw new ArgumentException("MappableRingBuffer requires at least one of UsageMapRead or UsageMapWrite", nameof(usage));

        var device = RenderSystem.GetDevice();
        for (var i = 0; i < BufferCount; i++)
        {
            _buffers[i] = device.CreateBuffer($"{label} #{i}", usage, size);
            _fences[i] = null;
        }

        _size = size;
    }

    //Size the byte size of each slot
    public int Size => _size;

    //CurrentBuffer returns this frame's buffer, waiting for the GPU to finish with it first
    public GpuBuffer CurrentBuffer()
    {
        var fence = _fences[_current];
        if (fence is not null)
        {
            fence.AwaitCompletion(long.MaxValue);
            fence.Dispose();
            _fences[_current] = null;
        }

        return _buffers[_current];
    }

    //Rotate advances to the next slot and records a fence on the slot being retired
    public void Rotate()
    {
        _fences[_current]?.Dispose();
        _fences[_current] = RenderSystem.GetDevice().CreateCommandEncoder().CreateFence();
        _current = (_current + 1) % BufferCount;
    }

    public void Dispose()
    {
        if (_disposed) return;
        for (var i = 0; i < BufferCount; i++)
        {
            _buffers[i].Dispose();
            _fences[i]?.Dispose();
        }

        _disposed = true;
    }
}
