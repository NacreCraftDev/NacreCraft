using System.Buffers;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Logging;
using NacreCraft.Util;

namespace NacreCraft.Client.Render;

//DynamicUniformStorage a per-frame bump allocator over a mappable ring buffer, maps to vanilla net.minecraft.client.renderer.DynamicUniformStorage
//Every write hands out one aligned block and the render pass binds that slice by offset, so thousands of draws can each
//carry their own transform without a UBO per draw; the whole storage resets when the frame ends
public sealed class DynamicUniformStorage<T> : IDisposable where T : class, DynamicUniformStorage<T>.IDynamicUniform
{
    private readonly List<MappableRingBuffer> _oldBuffers = new();
    private readonly int _blockSize;
    private readonly string _label;
    private MappableRingBuffer _ringBuffer;
    private int _nextBlock;
    private int _capacity;
    private T? _lastUniform;
    private bool _disposed;

    public DynamicUniformStorage(string label, int uboSize, int initialCapacity)
    {
        var device = RenderSystem.GetDevice();
        _blockSize = Mth.RoundToward(uboSize, device.GetDeviceInfo().Limits.MinUniformOffsetAlignment);
        _capacity = Mth.SmallestEncompassingPowerOfTwo(initialCapacity);
        _nextBlock = 0;
        _label = label;
        _ringBuffer = new MappableRingBuffer($"{label} x{_blockSize}", GpuBuffer.UsageMapWrite | GpuBuffer.UsageUniform, _blockSize * _capacity);
    }

    //BlockSize the aligned size of one uniform block
    public int BlockSize => _blockSize;

    //EndFrame resets the bump pointer and rotates the ring buffer
    public void EndFrame()
    {
        _nextBlock = 0;
        _lastUniform = null;
        _ringBuffer.Rotate();
        foreach (var oldBuffer in _oldBuffers)
            oldBuffer.Dispose();
        _oldBuffers.Clear();
    }

    //WriteUniform appends one uniform block and returns its slice
    public GpuBufferSlice WriteUniform(T uniform)
    {
        if (_lastUniform is not null && _lastUniform.Equals(uniform))
            return _ringBuffer.CurrentBuffer().Slice((long)(_nextBlock - 1) * _blockSize, _blockSize);

        if (_nextBlock >= _capacity)
        {
            var newCapacity = _capacity * 2;
            Log.Info($"Resizing {_label}, capacity limit of {_capacity} reached during a single frame. New capacity will be {newCapacity}.");
            ResizeBuffers(newCapacity);
        }

        var offset = (long)_nextBlock * _blockSize;
        var slice = _ringBuffer.CurrentBuffer().Slice(offset, _blockSize);
        using (var view = slice.Map(false, true))
        {
            var scratch = ArrayPool<byte>.Shared.Rent(_blockSize);
            try
            {
                scratch.AsSpan(0, _blockSize).Clear();
                uniform.Write(scratch);
                scratch.AsSpan(0, _blockSize).CopyTo(view.Data);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(scratch);
            }
        }

        _nextBlock++;
        _lastUniform = uniform;
        return slice;
    }

    //WriteUniforms appends a batch of uniform blocks in one mapped range, maps to vanilla writeUniforms
    public GpuBufferSlice[] WriteUniforms(T[] uniforms)
    {
        if (uniforms.Length == 0) return Array.Empty<GpuBufferSlice>();

        if (_nextBlock + uniforms.Length > _capacity)
        {
            var newCapacity = Mth.SmallestEncompassingPowerOfTwo(Math.Max(_capacity + 1, uniforms.Length));
            Log.Info($"Resizing {_label}, capacity limit of {_capacity} reached during a single frame. New capacity will be {newCapacity}.");
            ResizeBuffers(newCapacity);
        }

        var firstOffset = (long)_nextBlock * _blockSize;
        var totalLength = (long)uniforms.Length * _blockSize;
        var result = new GpuBufferSlice[uniforms.Length];
        var firstSlice = _ringBuffer.CurrentBuffer().Slice(firstOffset, totalLength);
        using (var view = firstSlice.Map(false, true))
        {
            var scratch = ArrayPool<byte>.Shared.Rent((int)totalLength);
            var blockBuffer = ArrayPool<byte>.Shared.Rent(_blockSize);
            try
            {
                for (var i = 0; i < uniforms.Length; i++)
                {
                    var block = i * _blockSize;
                    blockBuffer.AsSpan(0, _blockSize).Clear();
                    uniforms[i].Write(blockBuffer);
                    blockBuffer.AsSpan(0, _blockSize).CopyTo(scratch.AsSpan(block));
                    result[i] = _ringBuffer.CurrentBuffer().Slice(firstOffset + block, _blockSize);
                }

                scratch.AsSpan(0, (int)totalLength).CopyTo(view.Data);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(blockBuffer);
                ArrayPool<byte>.Shared.Return(scratch);
            }
        }

        _nextBlock += uniforms.Length;
        _lastUniform = uniforms[^1];
        return result;
    }

    private void ResizeBuffers(int newCapacity)
    {
        _capacity = newCapacity;
        _nextBlock = 0;
        _lastUniform = null;
        _oldBuffers.Add(_ringBuffer);
        _ringBuffer = new MappableRingBuffer($"{_label} x{_blockSize}", GpuBuffer.UsageMapWrite, _blockSize * _capacity);
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var oldBuffer in _oldBuffers) oldBuffer.Dispose();
        _oldBuffers.Clear();
        _ringBuffer.Dispose();
        _disposed = true;
    }

    //IDynamicUniform writes one uniform block into a scratch buffer
    public interface IDynamicUniform
    {
        void Write(byte[] buffer);
    }
}
